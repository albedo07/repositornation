# Reference mods: what we learned (reviewed 2026-10-02)

Decompiled with ilspycmd for study only. **Copy ideas and data, not code** (CreatureManager is GPL-3; the rest state no license).
Therzie Warfare, Therzie Monstrum and PathOfValheiman were received without DLLs (manifest/readme/localization only).

| Mod | What it is | What we take |
|---|---|---|
| WackyEpicMMOSystem | Levels, XP, attribute points, groups | XP-per-kill model, creature XP tables (saved in `epicmmo_xp_tables/`), death XP loss hook, group XP RPC pattern |
| CreatureManager (sighsorry, GPL-3) | Creature cloning, 32 combat modifiers, boss hunts, YAML reference writer | Approach for scanning `ZNetScene.m_prefabs` for `Character` prefabs and writing a reference file; modifier/elite ideas for Immortal Adversaries |
| GooCombatOverhaul | Souls-like combat: per-weapon roles, hyperarmor modes, lunge, hit-stop, counter/execution damage | Hyperarmor modes (damage/stagger/knockback taken), per-role attack tuning, combo finisher normalization |
| DualWield (source) | Off-hand weapon support | Reference for Mercenary Warfreak dual-wield |
| ValheimLegends (source + momos port) | 12-class system | Already used for aiming/raycast/projectile patterns (`DEV_REFERENCE_NOTES.txt`) |
| WackysDatabase | YML control of items/recipes/pieces/creatures | Pattern for data-driven configs with ServerSync |
| WackySpawners | Mob spawners with any piece look | Camps/spawners for Immortal Adversaries |
| WackyItemRequiresSkillLevel | Item equip/craft requirements by skill/level/keys | Later: gate gear by Immortal Heroes level |
| EpicLoot | Rarity, enchanting, bounties/treasure | Later: compatibility; bounty ideas for Immortal Adventures |
| PassivePowers | Boss powers become passive | Status-effect patterns |
| MyDirtyHoe | Grid/radius ruler | Authoritative in-game range calibration (Framework doc) |
| CombatOverhaul (leseryk), Forsaken | Older balance/power mods | Minor reference |
| Terraheim | Armor set bonuses + weapons | Later: gear identity ideas |
| PathOfValheiman (no DLL) | PoE-style passive tree | Design reference for the future attributes mod |

## EpicMMO XP model (as implemented)
- Per creature: `minExp`, `maxExp`, `level`, optional `biome`. XP on kill = `Random(min, max) + maxExp * 0.25 * stars`.
- Creatures missing from the tables give no XP. EpicMMO ships ~25 preset JSON packs (624 entries) instead of auto-generating; CreatureManager is the one that writes reference YAML by scanning prefabs.
- Curve: `300 * 1.05^n`. Death: lose 5-25% (configurable) on hard death.
- Group XP: an RPC sends XP to group members within range (Groups API).

## Our decisions (differ from EpicMMO)
- XP per star doubles: base x2^stars. Normal (Small) up to 7 stars, Elite (Big) up to 5. Big/Small from HP + damage.
- Unknown/modded creatures get auto-generated entries (by HP/damage) in a file we write.
- Curve 500 x1.25 per level through 15->16, then x1.05 to 80.
- Death: lose 5% of total XP, never below 10% of total XP or below the current level; no loss for 10 min after a death.
- Guild XP: guildmates active within 100 m each get 80% of kill XP (not quests).
