# Single virtual warehouse; multi-warehouse deferred

Warehouse surfaced as a new concept, **"initially 1."** Decision: each shop has **exactly one *virtual*
warehouse** — a conceptual singleton that holds all of a shop's stock. Stock is **not** keyed by warehouse;
it stays keyed by `StockUnitId` ([ADR-0016](./0016-stock-quantity-ledger-projection-and-concurrency.md)
unchanged). Multi-warehouse is a **documented seam**, not built.

## What "virtual" means here

The warehouse is **implicit** — no `Warehouse` entity, no `WarehouseId` column on the movement ledger,
`onHand`/`heldQty` projection, Holds, or availability. "The shop's stock" *is* the single virtual
warehouse's stock. The concept is named so the model acknowledges the future axis, but it carries **no data
and no behavior** in the MVP.

## Why defer, and where the real cost is

Retrofitting a partition key later *sounds* like the risk, but the expensive part of multi-warehouse is
**not the key** — it is **allocation logic**: which warehouse fulfils an order line, splitting a line across
locations, per-warehouse atomic holds and oversell guards, and availability as sum-across-vs-per-location.
With "initially 1" there is **no driver** for any of that, and adding a `WarehouseId` now *without*
allocation would just be a constant column — low value. The coherent moment to introduce
`(StockUnitId, WarehouseId)` is **together with** the allocation logic, when a genuine second location
appears.

## Considered and rejected

- **Add `WarehouseId` to the stock key now** — rejected: a column pinned to one value, with the hard part
  (allocation) still deferred; buys little and complicates ADR-0016's ledger/projection for no MVP gain.
- **Full multi-warehouse (locations + allocation) now** — rejected: no MVP driver ("initially 1").

## Relates to

Keeps [ADR-0016](./0016-stock-quantity-ledger-projection-and-concurrency.md)'s `StockUnitId` keying intact;
the deferred multi-warehouse work would extend the stock key + add allocation, revisiting ADR-0016 and the
placement/Hold path (ADR-0003).
</content>
