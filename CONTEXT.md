# Rainbow's End Gameplay Context

Rainbow's End is a tactical shooter in which the player protects movable Pots of Gold that collect gold from the unicorn's rainbow trail.

## Pot of Gold Feedback

**Pot of Gold**:
A movable gold container that can harvest gold only while it overlaps an active Rainbow Trail.
_Avoid_: Planter, gold pot

**Harvesting**:
The active state in which a Pot of Gold accrues gold from an overlapping Rainbow Trail.
_Avoid_: Filling, earning

**Harvest Flair**:
The upward rainbow visual shown only while a Pot of Gold is harvesting.
_Avoid_: Harvest glow, rainbow explosion

**Pot Glow**:
The persistent magical visual shown while a Pot of Gold contains uncollected gold.
_Avoid_: Harvest flair, full-pot indicator

**Pot Gold**:
The single spendable gold balance for a run, equal to the combined gold currently held in all live Pots of Gold. The HUD and upgrades use this value; it is not banked at the Fae Tree.
_Avoid_: Banked gold, home-tree deposit

**Ammo Counter**:
The HUD counter for the active weapon's shots. It shows infinity for the default weapon and remaining shots for a temporary weapon pickup.
_Avoid_: Pickup ammo, reserve ammo

**Run Timer**:
The HUD's elapsed run-time counter, shown as minutes and seconds from the start of a run.
_Avoid_: Next-spawn countdown, wave timer

**Kill Counter**:
The HUD total of all defeated hostile units: travellers, police, and military.
_Avoid_: Traveller kills, player-only kills

**Successful Theft Escape**:
A traveller reaching the arena edge after stealing a positive amount of Pot Gold. Only this outcome increments the escape counter.
_Avoid_: Retreat, unsuccessful flee

**Run Status HUD**:
The compact, always-visible Synty-styled HUD block showing Pot Gold, ammo, elapsed run time, kills, and successful theft escapes.
_Avoid_: Separate score screens, temporary notifications

**Run Statistics**:
Pot Gold, elapsed time, kills, and successful theft escapes are per-run values. They reset at run start and remain visible after the outcome until the next run.
_Avoid_: Lifetime statistics, persistent score

**Pot Gold Spending Order**:
When an upgrade costs Pot Gold held across multiple pots, the game deducts from the oldest live pot first, then newer pots until the cost is covered.
_Avoid_: Player-selected pot, random deduction

**Displayed Pot Gold**:
The HUD and shop use the floor of the combined fractional Pot Gold held in all live pots. Fractional harvest progress is retained internally.
_Avoid_: Rounded gold, discarded fractions

**Gold Pickup Deposit**:
A collected military gold pickup adds its value to the oldest live Pot of Gold, making it part of Pot Gold rather than a separate bank balance.
_Avoid_: Legacy bank reward, hidden gold
