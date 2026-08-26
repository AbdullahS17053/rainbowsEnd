# ADR-0002: Pot Gold Is the Run Currency

Status: Accepted
Date: 2026-08-26

## Context

Rainbow's End currently has two gold stores. Pots harvest into their local
stored-gold fields, while the HUD and shop read `EconomyState.CurrentGold`.
The pot's auto-bank rate and military gold pickups feed the second store. This
creates visible disagreement—for example, a pot can show 80 while the HUD
shows 20—and makes the Fae Tree feel like an implicit deposit point despite the
client's request for no depositing there.

The game permits multiple live pots, temporary fractional harvesting, upgrades
that spend gold, pooled military gold pickups, and enemy pot drains. A solution
must preserve those loops while making the displayed and spendable value
predictable.

## Decision

Use **Pot Gold** as the single spendable run currency:

- Its value is the combined stored gold of all live Pots of Gold.
- The HUD and shop display the floor of the combined fractional total.
- Upgrade spending drains the oldest live pot first, then newer pots.
- Military gold pickups deposit into the oldest live pot; if no live pot can
  accept the gold, the pickup remains available rather than becoming hidden.
- Pot auto-banking and the unused bulk-transfer path are removed.
- `EconomyState` remains only for the existing harvest-multiplier service in
  this scope. It is no longer a wallet for the HUD, shop, or pickups.

## Alternatives considered

1. Keep `EconomyState` as the wallet and increase auto-bank speed.
   Rejected: the pot and HUD still represent different values between updates,
   and the physical pot remains misleading.
2. Mirror every pot change into `EconomyState`.
   Rejected: this keeps two mutable sources of truth and adds synchronization,
   rounding, destruction, drain, and pooled-pickup failure cases.
3. Deposit only at the Fae Tree.
   Rejected: it conflicts directly with the client request and reintroduces
   location-gated currency behavior.

## Consequences

- Positive: one value drives pot labels, HUD, affordability, spending, and
  pickup rewards.
- Positive: no currency is silently deposited or hidden at the Fae Tree.
- Positive: oldest-first spending is deterministic without new player UI.
- Negative: shop/pickup code must stop depending on the legacy wallet.
- Negative: a pickup may remain in the world if every live pot is unavailable
  or full; this is intentional because silently crediting invisible gold would
  violate the decision.

## Rollback

Revert the PotController/PotRegistry/shop/HUD/GoldPickup currency changes as
one unit and restore the auto-bank configuration. Do not retain only part of
the migration, because mixed sources would recreate the mismatch.
