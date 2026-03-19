# MÖRK BORG Reference Data

This directory contains the JSON files that drive character generation and
vignette assembly for the MÖRK BORG game system.  Each file's schema is
described below.

---

## Alignment Requirement

Several files are **linked by key**.  `VignetteDataAlignmentTests` in
`tests/ProfessorBot.Games.MorkBorg.Tests/` automatically verifies that
the two sides stay in sync.  Add entries to **both** places when extending
a linked table; the test suite will tell you exactly what is missing.

| Vignette section | Source file | Source table |
|---|---|---|
| `ClassIntros` keys | `classes.json` | `name` field + reserved keys `Default`, `Classless` |
| `Traits` keys | `descriptions.json` | `Trait` array |
| `Bodies` keys | `descriptions.json` | `BrokenBody` array |
| `Habits` keys | `descriptions.json` | `BadHabit` array |
| `Items` keys | `weapons.json` | `name` field + reserved key `Default` |

---

## File Schemas

### `names.json`

A flat JSON array of strings used as the character name pool.

```json
["Aerg-Tval", "Agn", "Arvant", ...]
```

---

### `weapons.json`

Array of weapon objects.  The `name` value must have a matching key in
`vignettes.json → Items`.

```json
[
  {
    "name": "string",         // display name, used as vignette key
    "damage": "string",       // die expression, e.g. "d6"
    "isRanged": false,
    "twoHanded": false,
    "special": "string|null"  // optional rider text
  }
]
```

---

### `armor.json`

Array of armor objects keyed by `tier` (0 = no armor, 1 = light, 2 = medium,
3 = heavy).

```json
[
  {
    "name": "string",
    "tier": 0,
    "damageReduction": "string|null",  // e.g. "d4"; null for no armor
    "agilityPenalty": 0                // added to defense DR
  }
]
```

---

### `items.json`

Array of miscellaneous equipment objects used for starting item tables and
container rolls.

```json
[
  {
    "name": "string",
    "category": "string",         // e.g. "Container", "Supply", "Equipment"
    "description": "string|null",
    "silverValue": 0,             // null if not for sale
    "isConsumable": false
  }
]
```

---

### `spells.json`

Array of scroll objects.  `scrollType` must be `"Unclean"` or `"Sacred"`.
`scrollNumber` is the 1-based position on the d10 table for that type.

```json
[
  {
    "name": "string",
    "scrollType": "Unclean|Sacred",
    "scrollNumber": 1,
    "description": "string",
    "usageDR": 12
  }
]
```

---

### `classes.json`

Array of playable class definitions.  The `name` value must have a matching
key in `vignettes.json → ClassIntros`.

```json
[
  {
    "name": "string",
    "hitDie": "d8",
    "omenDie": "d2",
    "description": "string",
    "classAbility": "string",
    "startingWeapons": ["string"],        // weapon names from weapons.json
    "startingArmor": ["string"],          // armor names from armor.json
    "startingScrolls": ["string"],
    "startingSilver": "string|null",      // die expression, e.g. "d6*10"
    "strengthModifier": 0,
    "agilityModifier": 0,
    "presenceModifier": 0,
    "toughnessModifier": 0,
    "weaponRollDie": "string|null",
    "armorRollDie": "string|null",
    "startingEquipmentMode": "ordinary|classless|custom",
    "startingItems": ["string"],          // item names or generation tokens
    "canUseScrolls": true,
    "canWearHeavyArmor": true,
    "notes": "string|null"
  }
]
```

**`startingItems` generation tokens**

| Token | Effect |
|---|---|
| `random_sacred_scroll` | Rolls a random Sacred scroll |
| `random_unclean_scroll` | Rolls a random Unclean scroll |
| `random_any_scroll` | Rolls either type at random |

**`startingEquipmentMode` values**

| Value | Behaviour |
|---|---|
| `ordinary` | Waterskin + food + container + class `startingItems` |
| `classless` | Full classless flow (Table A + Table B) |
| `custom` | Class `startingItems` only — no base kit or random tables |

---

### `descriptions.json`

Lookup tables for randomly rolled character traits drawn at generation time.
Each entry in a table becomes a character `Description` prefixed with the
section name (e.g. `"Trait: Cowardly"`, `"Body: Decaying teeth."`).

All values in `Trait`, `BrokenBody`, and `BadHabit` must have matching keys
in `vignettes.json` (`Traits`, `Bodies`, and `Habits` respectively).

```json
{
  "Trait": ["string", ...],       // rolled once per character
  "BrokenBody": ["string", ...],  // rolled once per character
  "BadHabit": ["string", ...]     // rolled once per character
}
```

---

### `vignettes.json`

Drives the flavour paragraph generated for each character.  All keyed
dictionaries must stay aligned with the source tables described in the
**Alignment Requirement** section above.

```json
{
  "Templates": ["string", ...],
  "ClassIntros": {
    "Default": ["string", ...],     // fallback when class has no entry
    "Classless": ["string", ...],   // used for characters with no class
    "<ClassName>": ["string", ...]  // one entry per class in classes.json
  },
  "Traits": {
    "<TraitName>": ["string", ...]  // one entry per value in descriptions.json Trait
  },
  "Bodies": {
    "<BodyName>": ["string", ...]   // one entry per value in descriptions.json BrokenBody
  },
  "Habits": {
    "<HabitName>": ["string", ...]  // one entry per value in descriptions.json BadHabit
  },
  "Items": {
    "Default": ["string", ...],     // fallback when weapon has no specific entry
    "<WeaponName>": ["string", ...]  // one entry per weapon in weapons.json
  },
  "Closers": ["string", ...]
}
```

**Template placeholders**

| Placeholder | Resolved from |
|---|---|
| `{name}` | `character.Name` |
| `{classIntro}` | `ClassIntros[character.ClassName]` → `Default` fallback |
| `{trait}` | `Traits[character Trait description]` → random fallback |
| `{body}` | `Bodies[character Body description]` → random fallback |
| `{habit}` | `Habits[character Habit description]` → random fallback |
| `{item}` | `Items[character weapon name]` → `Default` fallback |
| `{closer}` | Random entry from `Closers` |
