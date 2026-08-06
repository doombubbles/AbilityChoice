# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

- Fixed for BTD6 v56

## [4.4.6] - 2026-06-24

- Fixed Apply After Challenge Rules setting making ability choices not fully apply in Legends gamemodes

## [4.4.5] - 2026-06-21

- New setting "Apply After Challenge Rules" - Default On
  - Makes Ability Choices calculate and apply their tower modification AFTER Challenge Rules change the cooldowns of towers rather than before
- A number of Red Ability choices that internally implement their effects in a similar way to Tech Bot mechanics now start on cooldown rather than off cooldown
  - Mainly effects cash generation abilities like IMF Loan, Supply Drop, etc
- Fixed console errors for Ice Monkey Paragon Red Ability Chioce

## [4.4.4] - 2026-06-07

- Fixed an Alchemist patch messing with the Dan D'Monke transform ability

## [4.4.3] - 2026-06-05

- Fixed an issue where it would take an extra click to cycle a few Ability Choices
- Fixed an issue for some users with Tower Ability Choices not always activating
- Switched to using `AbilityModel.isHidden` instead of the previous Harmony Patch system for hiding certain abilities

## [4.4.2] - 2026-06-03

- Updated for BTD6 v55

## [4.4.1] - 2026-05-11

- Fixed the Primordial Wrath range multipliers not being calculated correctly

## [4.4.0] - 2026-05-08

- Fixed Legend of the Night Red Ability Choice not correctly getting the extra +10 range over Dark Champion Red Ability Choice
- Added Red and Blue Ability Choice for Boomerang Paragon
  - Red: Periodically overcharges its own attack speed, with the bonus tapering off over 10 seconds.
  - Blue: Buffs the attack speed of all primary towers, including itself and other paragons, by 25% instead of 11%.
- Added Red and Blue Ability Choice for Ice Monkey Paragon
  - Red: Periodically freezes and slows all Bloons, even BADs and Bosses somewhat, and the first Boss skull each tier is nullified.
  - Blue: All Bloons are permanently slowed, even BADs and Bosses somewhat.
- Added Red and Blue Ability Choice for Druid Paragon
  - Red: Toggle to forego its own income generation to go into a 25% effectiveness Wrathful state.
  - Blue: Permanently foregoes all of its own income generation to always be in a 30% effectiveness Wrathful state.

## [4.3.2] - 2026-04-14

- Fixed Beast Handler Blue Ability Choice
- Adjusted Bomb Shooter Red Ability choice to account for the new shrapnel crosspath

## [4.3.1] - 2026-04-09

- Fixed for BTD6 v54

## [4.3.0] - 2026-02-12

- Fixed for v53
- Added Red Ability Choices for the reworked Druid Ability, they periodically vine crush multiple Bloons at once
- Added a Blue Choice for Adora's Level 7 Sacrifice
  - Level 7: Adora get more 10% Hero XP for each 3+/X/X Super Monkey (up to 50%). Each one nearby will buff the range and rate of fire of themselves and Adora by 5%, stacking up to 25%.
  - Level 20: ... Super Monkey buff is now 10% per stack, up to 50%.

## [4.2.1] - 2025-12-03

- Fixed for v52

## [4.2.0] - 2025-10-15

- Minor fixes for v51
- Added a Red Ability choice for the new Bomb Shooter Paragon for automatic ISAB missiles
- Fixed Beast Handler ability choices not applying to all crosspaths

## [4.1.1] - 2025-09-02

- Fixed for BTD6 v50.1

## [4.1.0] - 2025-08-30

- Added Ability Choices for Silas
  - Frostbite
    - Red: Periodically Silas can hit all Bloons types for a short duration, doing extra damage to Frozen Bloons based on remaining freeze time.
    - Blue: Every 4th attack can hit all Bloons types and does extra damage to Frozen Bloons based on remaining freeze time.
  - Frozen Cascade
    - Red: Occasionally creates shattering blasts of ice around Silas then other Ice Monkeys spreading out from Silas. Frozen Bloons take extra damage.
    - Blue: The Ice Fragments of nearby Ice Monkey can hit all Bloon types and do more damage, further increased against Frozen Bloons.
  - Frozen Burial
    - Red: Periodically freezes and damages all Bloons, dealing more damage to already frozen Bloons, then fills the track with mini Ice Walls.
    - Blue: Silas creates more Ice Walls, and Ice Walls now deal damage to Bloons.
- Churchill's Level 3 Red effect is now the Blue effect. The new Red effect is getting occasional bursts of Armor Piercing Shell usage
- Adora's Level 3 Red effect is now the Blue effect. The new Red effect is getting occasional bursts of Long Arm of the Light usage

## [4.0.2] - 2025-08-27

- Fixed for BTD6 v50, Ability Choices for Silas will be added in a subsequent mod update
- Adjusted Blue Mode Biohack to slightly reduce the attack speed of the affected tower at lower levels
    - This is still likely going to be stronger than the base version, just no longer by quite as much
- Fixed an issue with Red Mode Take Aim sometimes becoming non permanent when loading a save
- Improved the interval handling for Red Mode Super Monkey Fan Club, so there will be no alternation of which dart
  monkeys are affected, and ability cooldown reduction can increase the number of affected dart monkeys

## [4.0.1] - 2025-07-14

- Fixed some console errors that could come up for Take Aim and Overclock

## [4.0.0] - 2025-06-26

### Paragon Ability Choices

- Master Builder
    - Red: Can Overclock other Paragons (permanently, one at a time), Mega Sentries create small explosions periodically
      instead of on death.
    - Blue: Nearby Paragons are partially overclocked, Mega Sentries create small explosions periodically instead of on
      death.
- Crucible of Steel and Flame
    - Red: Occasionally launches a Meteor at the strongest Bloon, sometimes shoots jets of flame with increased projectile lifespan.
    - Blue: Constantly shoots homing fireballs, sometimes shoots jets of flame with increased projectile lifespan.
- Navarch of the Seas
    - Red:  Occasionally uses a mega hook to destroy the strongest non-Boss Bloon on screen.
    - Blue: Can destroy BAD Bloons if it uses 8 out of its 10 hooks at once.
- Nautic Siege Core
    - Red: Occasionally fires a devastating radioactive missile at the strongest Bloon.
    - Blue: Attacks leave behind radioactive fallout.
- Mega Massive Munitions Factory
    - Red: Gets a 10s rate boost once per round, and occasionally creates Spikeageddons across the track.
    - Blue: Gets a 10s rate boost once per round, and ejects a continuous stream of Spikeageddon mines along the track.
- Goliath Doomship
    - Red: Occasionally drops carpet bombs along a selected path.
    - Blue: Joined by 3 bomber planes that launch homing bombs at the strongest Bloon.
- Magus Perfectus
  - Red: Spends 1000 mana/s on flame thrower + wall of fire attacks, and 25% current mana on occasional Phoenix explosions that create zombie ZOMGs/BFBs.
  - Blue: Foregoes necromancy and mana to permanently use weaker versions of all attacks at once.

### Desperado Ability Choices

- Take Aim
    - Red: Modified Ability: Permanently boost one tower at a time.
    - Blue: All towers in range have camo detection and slightly increased attack range.
- Marked to Pop
    - Red: Periodically Mark Bloons and make extra Execute attacks against Marked Bloons.
    - Blue: Desperado's main attacks have a chance to Mark Bloons for Execution.

### Other

- Overclock Ability Choice changes
    - Blue choice now uses a real overclock mutator so it has its special interactions with Villages and Banana Farms

## [3.2.3] - 2025-06-19

- Fixed for BTD6 v49
- Fixed some bugs with Corvus Ability Choices
- Added internal accounting for Rogue Legends ability cooldown scaling when calculating power of ability choices

Desperado ability choices will be added in a subsequent update after I fix other mods for v49

## [3.2.2] - 2025-02-05

- Fixed for BTD6 v47
- Fixed a number of tooltip inconsistencies
- Fixes bugs with Tack Shooter and Druid ability choices

## [3.2.1] - 2024-12-10

- Fixed for BTD6 v46

## [3.2.0] - 2024-10-09

- Fixed for BTD6 v45
- Temporarily disabled the custom bloon overlay displays for MelonLoader 0.6.5 support
- Adjusted the Adora Sacrifice ability choice to just cost money instead of accruing debt
- Fixed some lag with Sabo Ninja Red Choice when it got large attack speed buffs
- Fixed The Anti-Bloon Ability Choices not applying properly for every crosspath
  - Also fixed interaction with Tech Terror and Anti Bloon Red ability choice crits / knockback
- Added Blue Ability Choice for Rosalia Level 3: Like Red Choice but firing fewer missiles with higher damage.
- Added Ability Choices for Corvus
  - Soul Harvest Red Choice - Corvus periodically harvests nearby Bloons in an instant
  - Spirit Walk Red Choice - Corvus gains increased range.
  - Dark Ritual Red Choice - Performs Dark Rituals that harvest a huge number of Bloons near Corvus.
  - Dark Ritual Blue Choice - Constantly performs a Dark Ritual that harvests Bloons near Corvus.
  - Continuous Spells (Spear, Aggression, Malevolence, Storm)
    - No longer have an upfront mana cost
    - When disabled due to not enough mana, will automatically reenable when enough mana is regained
    - Red Choice - Full power of effect, but increased recurring mana cost to compensate for no upfront cost
    - Blue Choice - Same recurring mana cost, but reduced power of effect to compensate for no upfront cost 
  - Instant Spells (Repel, Echo, Haste, Trample, Frostbound, Ember, Ancestral Might, Overload, Nourishment, Vision)
    - Now behave like Continuous Spells (also auto re-enable and have no upfront cost)
    - Red Choice - Full power of effect, increased recurring mana cost to compensate for 100% uptime potential
    - Blue Choice - Same recurring mana cost, but reduce power of effect to compensate for 100% uptime potential
  - Special Behaviors
    - Trample summons horses that follow the spirit rather than sending them and the spirit along the track
    - Overload no longer makes the spirit disappear, but makes less powerful explosions to compensate (red choice does medium explosions every 10 seconds, blue does small explosions every second)
    - Nourishment just has a Red choice for passively sacrificing 13 mana per second
    - Vision's blue choice does not strip camo but still gives sight
    - Soul Barrier functions normally, but it's one red choice will make it automatically be used if a leak happens

## [3.1.0] - 2024-08-25

- Added Red Ability Choices for Rosalia
- Added Red Ability Choices for Mermonkey
- Internally reworked Overclock/Ultraboost Red Ability Choices, they should work very robustly now
- Fixed a bug with Ezili's Moab Hex Blue Ability Choice

## [3.0.9] - 2024-08-01

- Fixes for BTD6 v44.0

## [3.0.8] - 2024-05-29

- Fixes for BTD6 v43.0

## [3.0.7] - 2024-04-21

- Fixed Elite Supply Drop for BTD6 42.2

## [3.0.6] - 2024-04-13

- Fixed a patch for BTD6 v42.1

## [3.0.5] - 2024-04-09

- Fixed potential crash with Pat Fusty Level 10 and MIB

## [3.0.4] - 2024-04-08

- Fixed for v42.0
- Reduced stun duration for Striker Jones Concussive Shell choices

## [3.0.3] - 2023-12-05

- Fixed for v40.0
- Will add Ability Choices for the new hero in a subsequent update

## [3.0.2] - 2023-10-16

- Fixed issue with Assassinate Moab ability choice not working without specific Monkey Knowledge
- Improved error handling for ability choices

## [3.0.1] - 2023-10-14

- Fixed for BTD6 v39
- Added an Ability Choice for the new Target Focus ability to make Mortar Monkeys constantly have increased accuracy

## [3.0.0] - 2023-09-27

- Added ability choices for all Hero abilities and applicable Geraldo Items
- These can be toggled by right clicking the corresponding icon in the Hero Screen upgrades list
- Fixed issue with changing Tower Ability choice settings after switching screens

## [2.2.2] - 2023-07-29

- Fixed Elite Supply Drop for 38.1

## [2.2.1] - 2023-06-07

- Fixed for BTD6 v37.0
- Fixed issue with Ninja Sabo blue effect

## [2.2.0] - 2023-04-10

- Added Beast Handler Ability Choices
- Fixed issue where Yellow settings could act like Red settings

## [2.1.3] - 2023-04-04

- Fixed for BTD6 v36.0

## [2.1.2] - 2023-02-20

- Recompiled for BTD6 v35.0

## [2.1.1] - 2022-12-27

- Updated for 0.6.0
- Custom Eletric Shock visuals won't work again until [this Il2CppInterop bug](https://github.com/BepInEx/Il2CppInterop/issues/66) is fixed :(

## [2.1.0] - 2022-10-23

- Fixed many Red Icon affects that've been broken by recent BTD6 internal changes
  - This is mainly the income generating and trasnforming related ones
- Fixed both Spike Factory Effects
- Also finished off giving Blue Icon effects to all middle paths that didn't already have them 
  - Added a Blue Icon choice to Transforming Tonic for whether he looks like the monster or not
  - Added a Blue Icon choice to Artillery Battery for permanently gaining the blast radius increase of the Bombardment ability
  - Turned the current Ice Monkey Red Icon effects into Blue Icon effects. The new Red Icon effects are about periodically freezing all Bloons on screen for a short duration.
  - Similarly, turned the current Glue Gunner Red Icon effects into Blue Icon effects. The new Red Icon effects are about periodically gluing all Bloons on screen for a short duration.
  - Added Blue Icon effects for Call to Arms and Homeland Defense for doubling the attack speed but not buffing pierce at all (gotta go fast)
  - Added Blue Icon effects for Turbo Charge and Perma Charge for increasing damage via a shock effect

## [2.0.2] - 2022-09-07

- Fixed for v32.3

## [2.0.1] - 2022-08-11

- Recompiled for v32.1 to fix ShowOkPopup method signature change

## [2.0.0] - 2022-08-08

- Revamped to Upgrade Screen choice
- Updated for Mod Helper 3.0
- Fixed for BTD6 v32.0

[unreleased]: https://github.com/doombubbles/ability-choice/compare/4.4.6...HEAD
[4.4.6]: https://github.com/doombubbles/ability-choice/compare/4.4.5...4.4.6
[4.4.5]: https://github.com/doombubbles/ability-choice/compare/a0e756a6a5dae11ab1b2366429d1467e471b2c14...4.4.5
