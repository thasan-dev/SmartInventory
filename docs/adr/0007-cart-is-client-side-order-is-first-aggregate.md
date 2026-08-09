# Cart is a client-side concept; the Order is the first persisted aggregate

The customer's cart is **not a server-side aggregate**. It lives in the browser as an
ephemeral, unvalidated list of `{StockUnitId, quantity}` line items scoped to a single
Shop. It touches no stock and is never persisted server-side. The cart only materialises
on the server at **placement**, where it is validated against `available` and converted,
atomically, into an **Order** plus a **Hold** per Stock Unit. The **Order is therefore the
first persisted thing** in the purchase flow.

Placement is **all-or-nothing**: the placement transaction re-checks `available ≥ requested`
for every line, and any shortfall rejects the whole order (per-line messaging back to the
customer) — no partial orders, no partial Holds. The Order **snapshots** delivery address,
contact name, and per-line price at placement, so later profile or catalogue edits never
mutate a placed order. A successful placement yields an Order in the single initial state
**Placed** (confirmed, stock held, undispatched, unpaid COD); everything after Placed is
the order lifecycle owned by SI-24.

**Why no Cart aggregate:** browsing is fully public and registration happens *after* order
confirmation ([ADR-0006](./0006-dual-identity-merchant-entra-customer-phone.md)), so there
is no authenticated pre-order state — nothing to persist a cart against and nothing to
merge on login. Stock is not held until placement
([ADR-0003](./0003-stock-held-at-order-placement.md)), so a persisted cart would reserve
nothing and would need its own staleness handling. A server-side Cart buys cross-device
persistence and abandoned-cart analytics; the COD MVP needs neither, so it is pure cost.

**Consequences:** no cross-device carts and no abandoned-cart analytics in the MVP (both
are post-MVP additions that would introduce a Cart aggregate). The authoritative stock gate
is the placement transaction alone — any earlier availability read (e.g. on a review
screen) is advisory UX only and deliberately left out of the spec. Storefront
"can I buy this" reads use `available`, not `on-hand`.

Considered and rejected: a **server-side Cart aggregate** (cross-device persistence,
abandoned-cart tracking) — rejected as unneeded cost for a COD MVP with public browsing and
post-confirmation registration; **holding stock at add-to-cart or review** — rejected by
ADR-0003 (would require expiry to release abandoned checkouts); **partial placement** of the
in-stock lines — rejected in favour of all-or-nothing to keep the Order/Hold set consistent
and avoid "we placed half your order" ambiguity.
