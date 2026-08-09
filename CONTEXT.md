# Inventories

The e-commerce bounded context for SmartInventory: a category-generic multi-shop
catalogue and the stock, cart, order, delivery, discount, and costing concepts
built on it. First category is nursery plants; the model is category-generic from
day one.

## Language

### Tenancy

**Shop**:
A tenant of the SaaS platform — an isolated storefront with its own catalogue, stock,
orders, discounts, and profit view. Customers shop within one Shop at a time; there is
no cross-shop browsing or cross-shop cart. `ShopId` is the tenancy partition that scopes
downstream aggregates, enforced by **Tenant Isolation (RLS)** (below). A Shop has a **Shop
Status** lifecycle (Pending Verification → Active,
with Rejected / On Hold / Deactivated) managed by the **Platform Super-Admin**; only an
**Active** Shop with a valid **Subscription** has a public storefront and a full back office
(see Platform & subscription, [ADR-0013](docs/adr/0013-platform-admin-and-subscription.md)).
_Avoid_: Store, Tenant (Shop is the domain term; "tenant" describes its role), Merchant
(that is the owner, not the shop), Marketplace (rejected — storefronts are isolated)

**Tenant Isolation (RLS)**:
How one Shop's data is kept private from another's under the microservice split. **One shared
schema, a `shop_id` column on every shop-scoped table, and PostgreSQL Row-Level Security** — the
DB enforces isolation and **fails closed** (no `app.shop_id` set → zero rows), so a forgotten
filter leaks nothing. **Not** a schema or database per shop, and **not** an app-layer
`WHERE shop_id`. Platform-global tables (catalogue taxonomy, shop registry, Subscription Tiers,
Super-Admin) carry no `shop_id` and no policy. The **tenant context** (`app.shop_id`) is set per
transaction (`SET LOCAL`) from the token's `shop_id` claim on the HTTP path, and from the
**`shop_id` on the event envelope** on the async path; outbox/inbox/relay tables are RLS-exempt
plumbing. See [ADR-0019](docs/adr/0019-per-shop-tenancy-via-postgres-rls.md).
_Avoid_: Schema-per-tenant, Partition-per-shop (both rejected — see ADR-0019), Sharding

**Shop User**:
Any user with administrative access to a Shop's back office (the Owner or staff added by
the Owner). **Shop-scoped**: a Shop User belongs to exactly one Shop. Distinct from a
Customer, who buys from the storefront. **Identified by email and authenticated via Entra
External ID** (ADR-0006), same as the Owner. Lifecycle: the Owner (or a `USER.WRITE` holder)
**invites** a staff member by email and assigns one or more Groups; **revoke = deactivate the
membership** (reversible, audit preserved), not a hard delete. A staff Shop User is assigned
one or more Groups (effective permissions = their union); the Owner holds none (implicit
super-admin).
_Avoid_: Staff (a Shop User who is not the Owner), Admin, Employee

**Merchant** (a.k.a. **Owner**):
The Shop User who owns the Shop — an implicit **super-admin** holding every Policy, which
cannot be revoked. **Not an assignable Group** — permission checks short-circuit to allow for
the Owner. Exactly one per Shop, and a Shop User owns at most one Shop (1:1 for the MVP; a
single Shop already sells across multiple categories, so one owner rarely needs a second
Shop). The Owner cannot be revoked or demoted from within the Shop; only a **platform
suspension** of the Shop (SI-30) blocks their access. Owner *transfer* is out of MVP scope.
_Avoid_: Shop owner (acceptable synonym; "Merchant"/"Owner" is canonical), Seller, Vendor

**Policy**:
A single grantable capability from a **platform-defined fixed catalog**, named
`RESOURCE.ACTION` (e.g. `PRODUCT.WRITE`, `ORDER.READ`). Actions are **`READ` and `WRITE`
only** — no finer CRUD — and **`WRITE` implies `READ`** for that resource. The MVP catalog:
`PRODUCT.READ/WRITE` (catalogue incl. catalogue discounts), `STOCK.READ/WRITE` (movements +
`unitCost`), `ORDER.READ/WRITE` (transitions, manual discount, Order Costs, delivery
assignment, on-behalf orders), `REPORT.READ` (profit/costing), `SETTING.WRITE`
(delivery-fee default, Cost Types, Delivery Person roster), `USER.WRITE` (staff + roles).
Shops compose Groups from these; they cannot invent new Policies. (Renamed from *Permission*
in SI-29.) See [ADR-0012](docs/adr/0012-rbac-mechanics-policies-roles-enforcement.md).
_Avoid_: Permission (the superseded name), Scope, Grant, Capability, Privilege

**Group**:
A **shop-defined** named bundle of Policies (e.g. "Stock Clerk" = {`STOCK.WRITE`,
`ORDER.READ`}) that the Owner creates and assigns to Shop Users. Shop-scoped (carries
`shop_id`, RLS-isolated). A Shop User is assigned **one or more Groups**, and their effective
permissions are the **union** of those groups' Policies (the Owner holds none — implicit
super-admin). The **Group → Policy** and **User → Group** mappings are **data in the Accounts
DB**; Policies stay static strings in code. Renamed from **Role**, and reverses ADR-0012's
"exactly one Role per user / Groups deferred" (see the Groups amendment in
[ADR-0012](docs/adr/0012-rbac-mechanics-policies-roles-enforcement.md)).
_Avoid_: Role (the superseded name — folded into Group), Profile, Policy (a Policy is the
atomic capability a Group bundles)

**Customer**:
A person who buys from a single Shop. **Shop-scoped**: a Customer record belongs to one
Shop and is keyed by phone number *within that Shop*, so the same phone shopping at two
Shops yields two separate Customers. Identity is **app-managed** (not Entra): a phone
verified by OTP. A Customer is either a Guest or Registered — one record, upgraded in
place, never two identities for the same phone.
_Avoid_: Buyer, Client, User (User is the generic auth concept; Customer and Merchant are
the two kinds), Account

**Guest**:
An unregistered Customer. Has no login session, so must **OTP-verify the phone at every
checkout**, and enters delivery details per order (nothing saved as a reusable profile).
_Avoid_: Anonymous (the Customer is identified by a verified phone, just not registered)

**Registered Customer**:
A Customer who opted in to an ongoing account. Authenticates by **phone OTP at login**
(the session then persists), has a **saved delivery address**, and is **not asked for OTP
at checkout**. Registering **upgrades the same phone-keyed Guest record in place** — no
account merge or identity linking.
_Avoid_: Member, Account holder

### Catalogue

**Category**:
A platform-defined kind of product (e.g. Plant, Showpiece, Handcrafted) that owns a
fixed *Product Details schema* — the set of attributes its products share. The set of
categories and their Product Details schemas ship in code; shops do not define their own.
_Avoid_: Type, Kind, Department

**Product Detail**:
A descriptive, informational attribute of a product, defined by its category's Product
Details schema and filled in per product (e.g. a plant's water need). Low-stakes and
essentially string-valued: it powers display, not transactions. Product Details never carry
stock or price and are not the primary thing customers search on. (Renamed from **Trait**.)
_Avoid_: Trait (the superseded name), Attribute, Property, Spec, Feature

**Product**:
A catalogue entry belonging to one category. Groups its Stock Units and holds its
Product Details values. A product carries no stock or price of its own — those live on its
Stock Units.
_Avoid_: Item, Listing, Article

**Variation Axis**:
A named dimension of choice a category declares (e.g. Size, Colour). A category may
declare **at most two** axes. Each axis offers an unlimited set of option values
(Size: S/M/L; Colour: red/blue/…).
_Avoid_: Option (an option is a single value on an axis), Dimension, Attribute

**Variation**:
The precise, structured choice a customer makes along a category's variation axes.
Unlike a Product Detail, a variation is searchable/filterable and resolves to a Stock Unit
that carries stock and price.
_Avoid_: Variant, SKU (SKU means the concrete Stock Unit, not the axis)

**Stock Unit**:
The smallest sellable, individually-counted thing — one concrete combination of
variation option values (e.g. "Monstera · Large · Green"). It is the SKU that cart,
orders, and discounts reference. Its *identity and price* live in the Product
(catalogue) aggregate; its *count* lives in the separate Stock aggregate, keyed by
`StockUnitId`. A product may activate only the combinations it actually sells (subset
of the full matrix); a category with no variation axes yields exactly one default
Stock Unit per product, so everything sold is a Stock Unit. A Stock Unit is **not
sellable until its price is set** (see Pricing step) — activation gives it structure,
not sellability.
_Avoid_: SKU (informal alias), Variant, Line item

**Pricing (step)**:
Setting a Stock Unit's selling price. Price authoring is **triggered by Lot receipt**, not a free
anytime edit: every Lot makes its Stock Units **eligible for pricing**, opening a per-Stock-Unit step
where the **previous price auto-fills** (blank if none) and the owner may **override** any line.
Confirming writes **Pricing History**. Price is a **sellability gate** — an unpriced Stock Unit is not
sellable regardless of on-hand. See the pricing amendment in
[ADR-0015](docs/adr/0015-catalog-owns-price-and-cost-sales-owns-quantity.md).
_Avoid_: Repricing (one occasion of the step), Price list (there is no separate list — price lives on
the Stock Unit)

**Stock**:
The separate aggregate that owns the mutable quantity of a Stock Unit and the movements
that change it. Referenced by `StockUnitId`. On-hand is derived — the sum of the Stock
Unit's Stock Movements, not a stored counter that is overwritten.
_Avoid_: Inventory (the whole context is "Inventories"; a single count is Stock)

**Stock Movement**:
A single signed change to a Stock Unit's on-hand quantity, carrying a reason (e.g.
manual restock +10, damaged −3, **confirm-deduction −N**, cancel-restock +N). Movements are an
**append-only ledger** and the source of truth; on-hand is **derived** from them, while a maintained
**`onHand` projection** (with **`heldQty`**) is updated in the same transaction for O(1) availability
([ADR-0016](docs/adr/0016-stock-quantity-ledger-projection-and-concurrency.md)). Every change — manual
or automatic — is a movement; this is the audit trail. A UI "set on-hand to N" is translated into the
equivalent delta movement. Movements are **quantity-only**: the acquisition **cost** of inbound stock
lives on the **Lot** in the Catalog service, not on the movement
([ADR-0015](docs/adr/0015-catalog-owns-price-and-cost-sales-owns-quantity.md); supersedes the earlier
`unitCost`-on-movement from SI-22/SI-27).
_Avoid_: Adjustment (that is one *reason* for a movement, not the concept), Transaction

**Lot**:
A **supply receipt** registered in the **Catalog** service: a batch of goods received together from a
supplier, identified by a **lot number** and carrying header metadata (date, supplier, …), containing
**multiple line items** each `{Stock Unit, quantity received, cost}`. Registering a Lot feeds each
Stock Unit's weighted-average **Base Cost** (in Catalog) and emits a per-Stock-Unit quantity-received
event that **Sales** applies idempotently to `onHand` (dedupe by lot number). A receipt document, not
a FIFO cost layer. See [ADR-0015](docs/adr/0015-catalog-owns-price-and-cost-sales-owns-quantity.md).
_Avoid_: Batch (acceptable informal synonym; "Lot" is canonical), FIFO lot-layer (this is a receipt,
not a cost-consumption layer), Stock Movement (the Lot is the source; the inventory increment it
triggers is the Movement)

**Hold**:
A reservation of a quantity of a Stock Unit against a specific order, created at order
**placement** after validating availability. A hold raises *`heldQty`*, reducing *available* but not
*on-hand*. It is released by **Confirm** (which converts it into a deduction Stock Movement,
`onHand −= q`) or by a **pre-confirm cancel** (which releases it with no movement); a **post-confirm
cancel** instead posts a restock Movement, since the stock was already deducted. Holds do not
auto-expire in the MVP. See the [ADR-0003](docs/adr/0003-stock-held-at-order-placement.md) amendment.
_Avoid_: Reservation (acceptable synonym; "Hold" is canonical), Lock, Allocation

**Available**:
The quantity of a Stock Unit that can still be ordered: `onHand − heldQty`, where `heldQty` is the
sum of active Holds. Both are maintained projections of the movement ledger
([ADR-0016](docs/adr/0016-stock-quantity-ledger-projection-and-concurrency.md)). Placement validates
against *available* in one atomic conditional update; it is the hard oversell guard.
_Avoid_: Free stock, Sellable

**Warehouse**:
The location stock lives in. In the MVP every shop has **exactly one *virtual* warehouse** — an
**implicit singleton**: stock is keyed by `StockUnitId` alone, with **no `WarehouseId`** on the ledger,
projection, Holds, or availability. It is named only to mark the future axis; **multi-warehouse (physical
locations + allocation logic) is deferred** ([ADR-0021](docs/adr/0021-single-virtual-warehouse.md)). "The
shop's stock" *is* the virtual warehouse's stock.
_Avoid_: Location, Bin, Store (that is the storefront), Fulfilment centre (all imply the deferred
multi-warehouse model)

### Cart & checkout

**Cart**:
A customer's in-progress selection of Stock Units and quantities within a single Shop. It
is a **client-side, ephemeral concept — not a persisted aggregate**: it lives in the
browser (no server-side Cart, no cross-device sync), holds unvalidated `{StockUnitId,
quantity}` line items, and touches no stock. It only materialises server-side at the
moment of placement, where it is validated against *available* and turned into an Order
plus Holds. Because registration happens *after* order confirmation (ADR-0006), there is
no pre-order authenticated cart, so no "merge cart on login" concept exists. See ADR-0007.
_Avoid_: Basket, Bag, Cart aggregate (there is no Cart aggregate — the Order is the first
persisted thing)

**Order**:
The first persisted aggregate in the purchase flow — the atomic, committed result of a
successful checkout, scoped to one Shop and referencing the phone-keyed Customer. Placing
an Order validates `available ≥ requested` on **every** line (all-or-nothing; any shortfall
rejects the whole placement), creates a **Hold** per Stock Unit (ADR-0003), and records
`paymentMethod = COD` (the only value in the MVP). It **snapshots** the delivery address,
contact name, and per-line price at placement so later profile or catalogue edits never
mutate a placed Order. An Order may be **customer-placed** (self-serve storefront checkout,
entering at **Placed**) or **staff-placed** on a customer's behalf (a phone order created by
a Shop User, entering at **Confirmed**); the **`createdBy`** attribute records which — a Shop
User's id, or `self` for a customer order. The Order moves through the order lifecycle (see
below) and records every transition in its **Status History**.
_Avoid_: Sale, Purchase, Transaction, Checkout (checkout is the flow; the Order is its
result)

**Order Line Item**:
One line of an Order: a reference to a **Stock Unit**, a quantity, and the **price captured
at placement** (snapshotted, not a live catalogue reference). Distinct from a cart line —
a cart line is an unvalidated client-side wish, an Order Line Item is a committed, priced,
stock-held fact.
_Avoid_: Order item, Cart line (a cart line is the pre-placement, unpersisted counterpart)

**Placed**:
The initial state of a **customer-placed** Order the instant self-serve checkout succeeds:
submitted by the customer, stock held, no one from the shop has contacted them yet, unpaid
(COD). It is the entry point of the order lifecycle. (Staff-placed orders skip Placed and
enter at Confirmed.)
_Avoid_: New, Pending, Created

### Order lifecycle

The states an Order moves through after creation, and the transitions between them. Every
transition is a **deliberate human action** — there are no automatic or timed transitions
(consistent with the no-expiry Holds of [ADR-0003](docs/adr/0003-stock-held-at-order-placement.md)).
All forward transitions and shop-side cancellation require a Shop User with the
`manage_orders` permission (the Owner always qualifies). Happy path:
**Placed → Confirmed → Processing → Dispatched → Delivered → Settled**, with **Cancelled**,
**Failed**, and **Returned** as terminal exits. Note that **Delivered is not terminal** —
revenue is realised only at **Settled**. See
[ADR-0008](docs/adr/0008-order-lifecycle-and-failed-vs-cancelled.md).

**Confirmed**:
The Order after a Shop User has **contacted the customer and they confirmed** they still want
it (the COD call-to-confirm guard against fake/abandoned orders). It is the point the **final
price is agreed**: the manual per-order discount may be applied here and **nowhere later** —
after Confirmed the price is locked. Staff-placed orders are created directly in this state.
Once Confirmed the customer can no longer self-cancel; cancellation is shop-mediated.
_Avoid_: Accepted, Verified, Approved

**Processing**:
The Order while a Shop User is **preparing it** (packing, arranging fulfilment) after the
price is locked but before dispatch. Per-order **costing** is attached here (SI-27). It is
distinct from Confirmed (price agreed, not yet worked) and gives the back office a
new-orders vs in-progress split.
_Avoid_: Preparing, InProgress, Packing

**Dispatched**:
The Order after it has left the shop for delivery. Entering Dispatched **no longer touches stock** —
the deduction happened at **Confirmed** ([ADR-0003](docs/adr/0003-stock-held-at-order-placement.md)/
[0008](docs/adr/0008-order-lifecycle-and-failed-vs-cancelled.md) amendments). From here the order can
only reach Delivered, Cancelled (post-confirm → restock Movement), or Failed. Under the microservice
decomposition this transition is driven by **Logistics** outcomes (ADR-0009 amendment).
_Avoid_: Shipped, OutForDelivery, Sent

**Delivered**:
Goods handed to the customer **and the full COD amount collected in cash** — the two are
inseparable (no separate paid state, no partial payment). **Delivered is NOT terminal**: it
opens a return window during which the collected revenue is **provisional**, not yet realised.
From here the Order reaches **Settled** (sale final) or **Returned** (defect return).
_Avoid_: Completed, Paid, Fulfilled, Closed (none are final — Settled is)

**Settled**:
The terminal success state: the return window has passed and the sale is final. **Revenue is
realised at Settled** (not at Delivered); Cancelled, Failed, and Returned orders yield no
revenue. The transition is a **manual** Shop User (`manage_orders`) action — there is no timed
auto-settle (keeps the no-automatic-transitions rule). The exact date anchor for date-range
profit is SI-27's concern.
_Avoid_: Completed, Closed, Finalised, Confirmed

**Cancelled**:
A terminal exit where the Order is stopped **before delivery** with the goods **still
sellable**. **Pre-confirm** (from Placed) it **releases the Hold** (no Movement); **post-confirm**
(Confirmed / Processing / Dispatched) it posts a **restock Stock Movement**, since stock was deducted
at Confirm ([ADR-0003](docs/adr/0003-stock-held-at-order-placement.md)/
[0008](docs/adr/0008-order-lifecycle-and-failed-vs-cancelled.md) amendments). No money is involved
(COD is uncollected until Delivered). Carries a mandatory reason. A customer may self-cancel only
while **Placed**; otherwise a Shop User (`ORDER.WRITE`) cancels.
_Avoid_: Voided, Aborted, Returned (a return is post-delivery with a refund — distinct)

**Returned**:
A terminal exit reached **only from Delivered**, within the return window, when the customer
returns the goods for a genuine defect. Because COD cash was collected at delivery, a Return
**refunds that cash out-of-band** (the shop hands it back; the system records the refund of
the collected total — there is no online refund). It is **shop-mediated** (a Shop User
inspects and records it, not customer self-serve) and **whole-order only** (partial returns
are out of MVP scope). Stock is disposed **manually** like Failed/Cancelled: resellable goods
→ a restock Movement, defective goods → write-off. Yields **no revenue**. Carries a mandatory
reason.
_Avoid_: Refund (that is the money effect, not the state), Cancelled (pre-delivery, no refund)

**Failed**:
A terminal exit where **delivery could not be completed** — customer unreachable, wrong
address, refused, or goods damaged in transit. Reachable only from Dispatched. Unlike
Cancelled, Failed **does not auto-restock**: the dispatch deduction stands and the stock is
treated as a **loss** by default. If the physical goods do come back sellable, a Shop User
posts a **manual restock Stock Movement** separately — it is never an automatic side effect
of the state. Carries a mandatory reason. See [ADR-0008](docs/adr/0008-order-lifecycle-and-failed-vs-cancelled.md).
_Avoid_: Compensation (misleading — no payout occurs), Returned (returns/refunds are out of
MVP scope), Lost, WriteOff

**Status History**:
The ordered audit trail on an Order: one record per transition, each holding
`fromState → toState`, a timestamp, the actor (`by` — a Shop User id, or `customer`/`self`,
or `system`), and a reason where relevant (mandatory for Cancelled and Failed). It is the
single source of "who did what when" and supplies the timestamps downstream tickets read
(Delivered-at for SI-27 profit, Dispatched-at for SI-25). The current state is the latest
record.
_Avoid_: Audit log, Event log, Transitions (this is the domain trail, not infra logging)

### Delivery & fulfilment

**Delivery**:
The fulfilment record of an Order — an entity **belonging to the Order (1:1)**, not a separate
aggregate, and an Order never splits into multiple Deliveries (whole-order dispatch, ADR-0003).
It holds fulfilment data only — the **Delivery Method**, an optionally-assigned **Delivery
Person**, and the **Delivery Attempt** log — and *references* the Order's already-snapshotted
delivery address rather than duplicating it. It carries **no money**: the customer-facing
delivery fee lives on the Order (see below), and the shop's internal delivery cost is SI-27's.
The Delivery has **no state machine of its own** — the Order's state is the single source of
truth; the Delivery merely records how fulfilment went. See
[ADR-0009](docs/adr/0009-delivery-inside-order-and-partner-seam.md).
_Avoid_: Shipment, Fulfilment, Consignment, Dispatch (dispatch is the transition, not the record)

**Delivery Person**:
A shop-scoped record of someone who carries orders — essentially `{name, phone, active}`. It
is deliberately **not a Shop User**: a courier needs no back-office access, Entra login, or
RBAC permission, so they stay out of the merchant identity machinery (ADR-0006). The Owner
maintains this small roster (its size is the shop's "count of delivery workers") and may
assign one to a Delivery. A third minimal actor type alongside Customer and Shop User.
_Avoid_: Rider, Courier, Driver (acceptable informal synonyms; "Delivery Person" is canonical),
Delivery User (it is not a User)

**Delivery Attempt**:
One entry in a Delivery's ordered attempt log: `{attemptedAt, deliveryPerson, outcome,
reason?}`. Interim failed attempts (unreachable / wrong address / rescheduled) **append to the
log and leave the Order in Dispatched** — a retry is not a state change. A successful attempt
drives the Order to **Delivered**; a give-up drives it to **Failed** (carrying the final
reason). Outcome reasons share the taxonomy of the Order's Failed reasons.
_Avoid_: Attempt (ambiguous), Try, Delivery Event

**Delivery Method**:
How an Order is fulfilled — the **extension point** for fulfilment. The MVP has exactly **one
method: shop-handled, status set manually** by staff (staff record attempts and mark the
outcome). A future **delivery partner** (courier) would be *another* Delivery Method whose
adapter translates the partner's API/webhooks into the same outcomes — but the **Order state
machine is identical regardless of method**, and no partner integration is built in the MVP.
_Avoid_: Delivery type, Carrier, Provider, Channel

**delivery fee** (on the Order):
A **flat, per-order charge to the customer** for delivery, held on the **Order** (not the
Delivery). It **defaults from a shop-level setting**, is **editable per order and locked at
Confirmed** (the same gate as the manual discount), and is added to the COD total:
`COD payable = Σ(line price × qty) − discounts + delivery fee`, realised as revenue at
**Settled**. There is **no zone/weight/distance computation** in the MVP. On a **Returned**
order the refund includes the fee (full-total refund). This is the customer-facing charge
only; the shop's internal delivery **cost** is a separate SI-27 concern.
_Avoid_: Shipping fee, Delivery cost (the *cost* is the shop's expense, SI-27; the *fee* is the
customer charge)

### Discounts

The MVP has **two** discount mechanisms — a catalogue discount and a manual order discount.
**Coupons are deferred** (post-MVP) despite being in SI-18's destination. See
[ADR-0010](docs/adr/0010-discount-composition-and-precedence.md).

**Catalogue Discount**:
A reduction on a Stock Unit's price, set in the catalogue. Settable at two levels: on the
**Product** (a **percentage**, cascading to all its Stock Units) or on a single **Stock Unit**
(**percentage or fixed**). A **fixed** discount is a **per-unit amount** off the price. The
**more specific level wins** — a Stock Unit's own discount overrides the Product's for that SKU;
the two **never stack**. Optionally time-bounded (`{startsAt, endsAt}`). Applied at the **order
line**; the discounted line price is **snapshotted at placement** (SI-23). The effective price
is **floored at zero** (never negative).
_Avoid_: Sale, Markdown, Offer, Promotion (those are marketing framings; this is the price
reduction), Coupon (a coupon is a redeemed code — deferred, and distinct)

**Pricing History**:
Catalog's **append-only domain audit trail** of changes to what a customer is charged — **base price and
catalogue discount** — recording `{stockUnitId, changeType, before → after, changedBy, changedAt, reason?}`.
Written **in the same transaction as the change** (never drifts), **shop-scoped** (inherits RLS,
[ADR-0019](docs/adr/0019-per-shop-tenancy-via-postgres-rls.md)), and logged at the **level the value is
set** (SKU entry for a Stock-Unit change, one product-level entry for a Product discount — not fanned out).
The **first instance of the domain audit-trail pattern** (a sibling of **Order Status History** and the
**Stock Movement** ledger; Accounts-side trails deferred). Distinct from the **order-line price snapshot**
(what *one customer paid*, ADR-0007) — this tracks how the *catalogue* price moved. See
[ADR-0020](docs/adr/0020-domain-audit-trails-and-pricing-history.md).
_Avoid_: Audit log (this is a domain trail, not infra logging), Price movement (price is *set*, not
accumulated like Stock Movements), Event log

**Manual Discount**:
An **order-level** reduction (**percentage or fixed**) a Shop User applies at **Confirmed** on
the catalogue-discounted subtotal — the owner's concession during the confirm call. It
**stacks on top of** catalogue discounts and is **floored at zero**. There is **no margin cap**:
the owner may discount below cost as a deliberate choice; SI-27 surfaces the margin/loss, SI-26
does not forbid it. Locked at Confirmed with the rest of the price.
_Avoid_: Adjustment, Override, Rebate

**Order total composition** (locked at Confirmed):
1. **Line effective price** = Stock Unit price − Catalogue Discount (specificity-wins), floored at 0.
2. **Subtotal** = Σ(line effective price × qty).
3. **COD payable** = Subtotal − Manual Discount (floored at 0) + delivery fee.

### Costing & profit

The MVP computes **per-order profit** as a **contribution margin** — revenue minus *directly
attributable* costs. **Organization-wide overheads (salaries, electricity, rent) are
deliberately excluded** because they cannot be fairly distributed per order. **Date-range
profit reporting is deferred** (post-MVP). See
[ADR-0011](docs/adr/0011-per-order-contribution-margin-costing.md).

**Base Cost** (a.k.a. COGS):
The **weighted-average acquisition cost** of a Stock Unit, maintained **in the Catalog service** from
the cost on each **Lot** line item. Each Lot updates the running average; there is no FIFO lot
tracking. An order line's cost of goods = Base Cost × qty, **snapshotted onto the order line at
dispatch** so a later Lot cannot change a shipped order's COGS
([ADR-0015](docs/adr/0015-catalog-owns-price-and-cost-sales-owns-quantity.md)).
_Avoid_: Purchase price (that is one input to a Lot line's cost), FIFO cost, Landed cost

**Cost Type**:
A **shop-defined** category of order cost (e.g. Delivery, Packaging, Marketing) that the shop
sets up once. A Cost Type may carry an **optional default amount** (this is where a "fixed"
delivery cost lives — it pre-fills but stays editable). Shops choose *not* to create types for
overheads. Distinct from the platform-fixed Permission catalog — cost types are the shop's own.
_Avoid_: Expense category (acceptable synonym), Account, Ledger code

**Order Cost**:
A line the owner adds to an Order at **Processing**: `{costType, amount}`. An Order carries a
**flexible list** of these — as many as needed — rather than hardcoded delivery/packaging
fields. These are the per-order *directly attributable* costs beyond Base Cost. A Delivery
Order Cost is the **shop's expense** and is distinct from the customer-facing **delivery fee**
(revenue, SI-25) — the two never net.
_Avoid_: Fee (a fee is charged to the customer; a cost is the shop's expense), Charge, Expense line

**Per-Order Profit**:
`profit = revenue − Base-Cost-of-goods − Σ(Order Cost amounts)`, where
`revenue = Σ(line effective price × qty) − manual discount + delivery fee`. **Final at
`Settled`** (provisional before, since revenue realises there). For **`Failed`/`Returned`**
the loss = Σ(Order Costs incurred) plus Base-Cost-of-goods **only if the units were written
off** (not if manually restocked); it **finalises when the stock disposition is recorded**.
**`Cancelled`** is ≈ breakeven (an Order Cost such as packaging counts only if already
incurred). Shop-scoped, shown on the order.
_Avoid_: Net profit (this excludes overhead — it is a contribution margin), Margin %, Markup

### Platform & subscription

**Platform Super-Admin**:
The SaaS operator's staff — a **fourth actor** beside Shop User/Merchant, Customer, and
Delivery Person, and the only one that is **not shop-scoped**: it operates above/across all
Shops (the deliberate exception to storefront isolation, ADR-0004). Authenticates via Entra
External ID on a **separate platform-admin surface** (own token, no `ShopId`), operator-
provisioned (not self-service), and sits **outside the shop Policy/RBAC system**. Verifies /
approves / rejects shops, holds & lifts, activates / deactivates, and manages Subscriptions
and Tiers. A platform-admin token can never satisfy a shop or storefront endpoint.
_Avoid_: Admin, Super Admin (the Owner is the *shop's* super-admin; this is the *platform's*),
Operator (acceptable informal synonym)

**Shop Status**:
The lifecycle state of a Shop, mostly Platform-Super-Admin-driven: **Pending Verification**
(self-registered, details completed, subscription requested; not public) → **Active**
(SA-approved + subscription valid; storefront live, back office full), with exits **Rejected**
(SA declined; Owner may re-request → Pending), **On Hold** (SA block; Owner cannot re-request;
only SA lifts → Pending), and **Deactivated** (an Active shop turned off by subscription lapse
or SA action — **storefront offline, back office blocked, in-flight orders frozen**; SA
reactivates → Active). Only **Active + valid subscription** grants full access — the outermost
enforcement gate (renamed from SI-29's "suspended"/shop-active gate). See
[ADR-0013](docs/adr/0013-platform-admin-and-subscription.md).
_Avoid_: Suspended (renamed to Deactivated), State

**Subscription**:
Ties a Shop (via its Owner, the subscriber) to a **Subscription Tier** and a **validity period**
(`validFrom`, `validUntil`). Set and extended **manually by the Platform Super-Admin**; **payment
is out-of-band** (no gateway — consistent with the COD / no-online-payment scope). **Lapse is
enforced live** at the status gate (`Active` AND currently valid) — no scheduler; renewal = SA
extends `validUntil`. Grants access for the period; **entitlement is resolved from the local
database** (the source of truth), not Entra or an external billing system.
_Avoid_: Plan (that is the Tier), Licence, Membership, Billing (no billing engine exists)

**Subscription Tier**:
A named tier mapping to a set of **feature flags** that determine which features a subscribed
Shop may use. The **MVP ships a single Tier** (all MVP features on), but the tier→feature-flag
mechanism is built so more tiers can be added without rework; a feature is gated by checking the
flags of the Shop's current Tier.
_Avoid_: Plan (acceptable synonym; Tier is canonical), Package, SKU (that is a Stock Unit)
