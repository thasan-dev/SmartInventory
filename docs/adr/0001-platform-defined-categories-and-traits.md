# Platform-defined categories and trait schemas

> **Terminology update:** what this ADR calls a **Trait** / **trait schema** is renamed to
> **Product Detail** / **Product Details schema** (the concept is unchanged). This ADR keeps the
> original wording as the historical record; `backend/CONTEXT.md` is the authoritative glossary.

Categories (Plant, Showpiece, Handcrafted, …) and the trait schema each one owns are
**defined in code by the platform**, not created by shops at runtime. We rejected a
shop-defined / EAV model where each shop invents its own categories and typed
attributes.

Why: the MVP has effectively one real category on day one (nursery plants), so runtime
flexibility buys little, while shop-defined schemas would force a dynamic
attribute-schema editor and EAV-style storage/validation. Platform-defined keeps traits
strongly-typed and the model simple. Adding categories means a code change — an accepted
cost for the MVP.

Traits are descriptive and low-stakes (essentially string-valued, informational, used
for display/filtering); they never carry stock or price. The transactional rigor lives
on variations instead (see [ADR-0002](./0002-stock-separate-from-catalogue.md)).

Shop-defined categories/traits remain a possible post-MVP effort; revisit this if a
customer needs it.
