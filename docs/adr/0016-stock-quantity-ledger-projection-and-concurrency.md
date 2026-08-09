# Stock quantity: movement ledger + onHand/heldQty projection, and hold/deduction concurrency

Refines how **Stock quantity** (owned by the **Sales** service — ADR-0014/0015) is stored and mutated
under concurrency. It does not change the Hold *policy* (ADR-0003) beyond that ADR's Confirm-deduction
amendment; it fixes the *representation* and the *concurrency control*.

## Ledger + projection — both stored

- **Movement ledger (source of truth)** — every quantity change is an immutable, append-only **Stock
  Movement** (signed qty, reason, references such as order id / lot number). This is the audit trail
  and reconciliation base. It is **quantity-only**; acquisition cost lives on the **Lot** in Catalog
  (ADR-0015).
- **Projection (maintained for O(1) reads)** — per Stock Unit, a maintained **`onHand`** and
  **`heldQty`**, updated **in the same transaction** as each movement/hold. `available = onHand −
  heldQty`. The projection is a **cache of the ledger, always reconcilable from it** — not an
  independently-overwritten counter (which is what ADR-0003 / SI-22 rejected).

Summing the whole ledger on every read is O(n) and would force locking the entire history under the
atomic placement check — rejected. The projection makes availability a single-row read/update.

## The one contended, can-fail operation: Hold at placement

Placement (customer-driven, concurrent) is the **only** availability-checking operation that can fail
and contends on the row. It is a single atomic conditional statement:

```sql
UPDATE stock SET heldQty = heldQty + :q
 WHERE stockUnitId = :id AND onHand - heldQty >= :q;   -- 1 row = held, 0 = insufficient
```

The `WHERE` clause is the oversell guard; the row lock is held for one statement (microseconds). The
Hold record/Movement is inserted in the same transaction (inserts don't contend the row). No slow work
(events, SMS, computation) inside the transaction.

## Everything else is owner-driven and low-contention — but still atomic

- **Deduction at Confirm** — a safe conversion of an already-reserved hold: `onHand −= q; heldQty −=
  q` + a deduction Movement. Cannot fail on availability; owner-triggered. (ADR-0003/0008 amendments.)
- **Lot receipts / manual restock / damage** — owner-driven, rare, but still executed as
  **single-statement atomic updates** (`onHand += n`, …), never app-side read-modify-write, so a rare
  increment cannot lose an update against a concurrent hold. Async movements (lot receipts) are
  consumed **partitioned by `StockUnitId`** so same-SKU events serialise (ordering + idempotency by
  lot number via the MassTransit inbox); different SKUs stay fully parallel.

"Rare" reduces *contention*, not the need for *atomicity* — low frequency never excuses a lost update.

## Concurrency choice: pessimistic short-lock, not optimistic-retry

The atomic conditional `UPDATE` (a one-statement pessimistic row lock) is preferred over optimistic
version-check-with-retry, because retries storm exactly under the hot-row pressure we care about.
Contention is **per-Stock-Unit** — different SKUs never block each other, so only a single
hyper-popular SKU is ever a hot spot.

## Extreme hot-SKU: a deferred seam

If one SKU ever melts under true flash-sale load (not the MVP), escalate via **bucketed / sharded
counters** (split a SKU's stock into N sub-counters, `available = Σ buckets`) or a **per-SKU
single-writer**. Not built now; kept behind the reservation interface (same YAGNI logic as ADR-0014).

## Considered and rejected

- **Pure mutable counter (`stock = stock + n`, no ledger)** — rejected: loses audit, idempotency,
  reconciliation.
- **Pure derived (SUM movements on every read, no projection)** — rejected: O(n) and forces locking
  history under the atomic placement check.
- **Optimistic concurrency as the default** — rejected for the hot path: retry storms under
  contention.
- **Building bucketing / per-SKU actor now** — deferred: no scaling driver.

## Relates to

Refines ADR-0003 (hold policy; deduction-at-Confirm) and SI-22 (movement-sum stock). Quantity lives in
Sales (ADR-0014); cost lives on Lots in Catalog (ADR-0015). Idempotency reuses the existing MassTransit
EF **inbox** (`InboxState`).
