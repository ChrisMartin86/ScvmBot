# Cy_Borg Data Reference

This directory contains the game data tables for Cy_Borg character generation.

## Files

- **names.json** — Character handles/street names
- **classes.json** — Playable classes with abilities and starting gear
- **weapons.json** — Weapon table (d10 by default for classless, varies by class)
- **armor.json** — Armor table (d4 for classless, maps to tier 0–3)
- **gear.json** — Starting gear items
- **apps.json** — Nanograms / hacker apps (like scrolls)
- **descriptions.json** — Trait, Appearance, and Glitch tables

## Character Generation Rules

- **Abilities**: Roll 3d6 for each of STR, AGI, PRE, TGH. Apply the standard modifier table.
  - 3–4 → −3 | 5–6 → −2 | 7–8 → −1 | 9–12 → 0 | 13–14 → +1 | 15–16 → +2 | 17–18 → +3
- **HP**: Toughness modifier + hit die (varies by class, d6 default), minimum 1
- **Luck**: Roll luck die (d4 for most classes)
- **Credits**: 2d6 × 10 (classless), or class-specific formula
- **Class**: 50/50 chance of being classless; if classed, pick one of the six classes

## License

CY_BORG is © Stockholm Kartell. Used under the CY_BORG Third Party License.
