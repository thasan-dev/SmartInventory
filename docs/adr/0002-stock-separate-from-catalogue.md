# Stock is a separate aggregate from the product catalogue

The **Product** aggregate owns catalogue structure: the product, its variation axes
(at most two per category), which Stock Units exist (a subset of the axis matrix), and
each Stock Unit's price. A **separate Stock aggregate**, keyed by `StockUnitId`, owns
the mutable quantity and its movements. Product references stock only by id; Stock
references the catalogue only by id.

Why: stock mutates on a different rhythm and by different actors than the catalogue —
dispatch, cancel-restock, and manual adjustments change quantity constantly, while
axes/prices change rarely. Holding quantity inside the Product aggregate would force
every stock change to load and lock the whole product (all SKUs, prices, axes) and
would bloat the catalogue aggregate. In this event-driven / CQRS backend, splitting
them keeps each aggregate's consistency boundary tight and gives the inventory work
(SI-22) its own home for adjustments and audit.

Consequence: a Stock Unit's *identity and price* live in the catalogue; its *count*
lives in Stock. There is no cross-aggregate transactional invariant between them — a
Stock Unit's stock is created/reconciled via events when the catalogue defines or
retires a combination.

Considered and rejected: Stock Unit as a child entity of Product holding its own
`quantityOnHand` (one aggregate owns everything) — simpler to write, but couples
high-frequency stock writes to low-frequency catalogue edits under one lock.
