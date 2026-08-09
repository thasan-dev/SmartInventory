# Discount composition: two mechanisms, catalogue-specificity-wins, manual stacks; no coupons in MVP

The MVP has **two** discount mechanisms, and a single, fixed rule for how they compose the
order total. **Coupons are deferred** to post-MVP.

**Catalogue Discount** (line-level) — a price reduction set in the catalogue, at either the
**Product** level (a percentage, cascading to all its Stock Units) or a single **Stock Unit**
(percentage *or* a per-unit fixed amount). The **more specific level wins**: a Stock Unit's own
discount overrides the Product's for that SKU, and the two **never stack** — a single item is
never double-discounted from the catalogue. Optionally time-bounded. The effective line price
is floored at zero and **snapshotted at placement** ([ADR-0007](./0007-cart-is-client-side-order-is-first-aggregate.md)).

**Manual Discount** (order-level) — a percentage or fixed reduction a Shop User applies at
**Confirmed** (SI-24) on the catalogue-discounted subtotal. It **stacks on top of** catalogue
discounts.

**Composition (locked at Confirmed):**
1. line effective price = Stock Unit price − Catalogue Discount (specificity-wins), floored at 0
2. subtotal = Σ(line effective price × qty)
3. COD payable = subtotal − Manual Discount (floored at 0) + delivery fee (SI-25)

**No margin cap.** Discounts are floored at zero but may drive the order below cost; that is a
deliberate owner choice. SI-27 surfaces the resulting margin/loss; SI-26 does not forbid it.

**Why these choices:** catalogue discounts are *line* concerns (they change what a SKU sells
for) while the manual discount is an *order* concern (a per-deal concession), so they live at
different levels and composing them by stacking is natural and predictable. Catalogue
specificity-wins (rather than stacking Product + Stock Unit) prevents surprising
double-discounts and keeps "what is this item's price right now?" answerable at one level.
Percentage-only at the Product level avoids the ambiguity of a fixed amount spread across
differently-priced variations; a Stock Unit, having one price, can take either.

Considered and rejected: **coupons in the MVP** — dropped to cut scope (validity, per-customer
and total caps, and coupon-vs-catalogue interaction are non-trivial); they remain in the fog
and can graduate later. **Stacking Product + Stock Unit catalogue discounts** — rejected to
avoid double-discounting and ambiguous effective price. **A hard margin floor / below-cost
block** — rejected in favour of leaving the call to the owner and letting SI-27 report the
consequence. **Product/category-scoped coupons and per-variation coupon logic** — moot while
coupons are deferred.
