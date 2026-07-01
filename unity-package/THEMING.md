# Crossroads - theming & extensibility contract

The engine ships an opinionated default look, but **it must not lock a design decision a game might reasonably
want to change**. Every such decision is reachable from the consuming game as *data* (the `Theme`) or an
*overridable behavior* (a hook) - never buried in engine code. This document is the decision rule for where a
new knob goes, the token model it goes into, and the compatibility policy around changing it.

---

## The decision rule - where does a value belong?

When the engine hardcodes a visual/behavioral value and a game wants it different, classify it:

| Kind | Goes to | Test |
|---|---|---|
| **Token** (semantic, cross-component) | a field on `Theme` (palette role, typography, an asset, a label) | "Is this a color/font/asset/text a game rethemes globally?" e.g. `accent`, `ring`, `keyArt`. |
| **Component style** (a metric of one widget) | a `[Serializable]` struct on `Theme` (`MedallionStyle`, `MeterStyle`) | "Is this a pixel size / fraction specific to one widget?" e.g. medallion diameter, ring thickness, meter icon size. Not a flat field at the Theme root. |
| **Hook** (behavior/animation, not expressible as data) | an overridable component / interface (`ICardChoiceFeedback`) | "Is this *how something animates/behaves*, not *what color/size it is*?" e.g. the choice-selection effect. |
| **Core** (a format affordance) | stays fixed in engine code | "Would changing it break the genre's UX contract?" e.g. the card's swipe slide/tilt, swipe directionality. |

The rule exists so the choice is a table lookup, not a per-game argument. When in doubt between Token and
Component-style, prefer the struct if the value is a raw pixel metric of a single widget - that keeps the Theme
root from becoming a flat god-object as more games are onboarded.

---

## The "unset = engine default" convention

A game overrides only what it wants; everything else inherits the engine default. This must be **explicit**,
not a magic zero. Unity deserializes a field that is missing from an older serialized `theme.asset` to the
type default (`0` / transparent), which is indistinguishable from a legitimate zero - so the convention is:

- **Sprites / object refs:** `null` is the natural "unset" (a game never wants an intentional null asset). No
  wrapper.
- **Floats / Colors:** an explicit optional wrapper - `OptionalFloat { bool set; float value; }`,
  `OptionalColor { bool set; Color value; }`. `set == false` means "inherit"; a game that genuinely wants
  `0` / transparent sets `set = true` with that value. An old asset lacking the field deserializes to
  `set == false` -> inherits -> keeps today's look.

All engine default literals live in one place: **`ThemeDefaults`** (`Runtime/UI/ThemeDefaults.cs`). Accessors
on `Theme` resolve `optional.set ? optional.value : ThemeDefaults.X`, and any fallback chain (a role that
defers to another role) is expressed there too. No default literal is duplicated in a consumer (`CardView`,
`ResourceBarView`).

---

## The token model

### Palette (semantic roles - no overloaded token)

`accent` used to mean buttons *and* the medallion ring *and* the HUD divider *and* the speaker name. Roles are
now separate so rethemeing one does not drag the others:

| Role | Used by | Unset falls back to |
|---|---|---|
| `background` | screen backdrop, HUD-bg derivation | - (base) |
| `card` | card fill when no `cardArt` | - (base) |
| `text` | body / primary text | - (base) |
| `textMuted` | secondary text | `text` dimmed |
| `accent` | primary buttons / highlights | - (base) |
| `ring` | speaker medallion ring, procedural frames | `accent` |
| `divider` | HUD separator line | `accent` |
| `choiceHint` | resting choice-label color | `text` |
| `choiceGlow` | active choice glow-halo | `accent` |
| `hudPlate` | meter plate fill | derived-from-`background` (legacy formula) |
| `approaching` / `willBreak` | meter danger bands | - (base; also marked `!`/`!!`, not color-only) |

### Component styles (nested structs, optional fields)

- `MedallionStyle { OptionalFloat size; OptionalColor ringColor; OptionalFloat ringThickness; OptionalFloat innerFraction; }`
  - `innerFraction` = how far the portrait tucks under an ornate `speakerFrame` (calibrated per frame asset;
    was a hardcoded `0.78` tied to one PNG).
  - Layout that depends on `size` (medallion anchor, lift, and the body band below it) is derived from `size`
    in code, so enlarging the medallion moves the text with it instead of colliding.
- `MeterStyle { OptionalFloat iconSize; OptionalFloat frameSize; OptionalColor iconTint; }`

### Hooks

- `ICardChoiceFeedback` / `CardChoiceFeedback` - the choice-selection emphasis. Default =
  `DefaultCardChoiceFeedback` (reads `choiceHint`/`choiceGlow` tokens). A game supplies its own for a
  different *animation* (e.g. a warm light sweep). The card's slide/tilt stays Core.
- Future hooks should be **injected from `GameBootstrap`** (data/DI), not discovered via `GetComponent`, so all
  overrides share one wiring model. (The current `GetComponent` discovery is the interim mechanism.)

---

## Default identity - neutral engine, skins as samples

The engine's out-of-box default is **neutral** (clean, typographic, low thematic signature) so any game can
dress it. A strong art direction ships as a **skin** = a `Theme` asset under `Samples~/`. The first game's
medieval look (bronze, gold ring, stone plaques) is the `Medieval` skin sample, not the engine default. A game
adopts a skin by starting from that `theme.asset` and overriding from there.

**Medieval skin recipe** (the values that reproduce the original look on top of the neutral default - author as
a `Theme` asset via the bridge `create_theme` + `set_*` tools; do not hand-write the YAML):

| Token | Value |
|---|---|
| `accent` | `(0.62, 0.50, 0.28)` bronze |
| `ring` | `(0.62, 0.50, 0.28)` bronze |
| `divider` | `(0.85, 0.68, 0.28)` gold |
| `choiceHint` | `(1.0, 0.90, 0.62)` warm gold |
| `choiceGlow` | `(1.0, 0.82, 0.40)` bright gold |
| `meter.iconTint` | `(0.72, 0.64, 0.48)` antique bronze |

Note: the rest of the medieval signature still lives in procedural texture generators (`PanelShapes` bronze
plaque edge) rather than in color tokens, so a skin cannot yet fully repaint those. Tokenizing the
`PanelShapes` fill/edge colors is the tracked remaining step to make the neutral/skin split complete; until
then those plaques render bronze regardless of the skin.

---

## Compatibility & versioning policy

- **Additive-with-fallback Theme change** (new optional token / struct field, unset = prior look) -> **minor**
  bump. Safe: an old `theme.asset` inherits and looks identical.
- **Structural Theme change** (renaming/moving a serialized field, changing a field's type in a way that drops
  old data) -> **major** bump + a migration note in this repo's README + a migration test.
- Every restructuring must keep a **migration test** that loads an old-shaped `theme.asset` fixture and asserts
  the resolved look is unchanged. `ThemeDefaultsTests` covers a fresh empty instance; the migration test
  covers the real "old asset missing new fields" path the convention exists to protect.
- The three games pin independent engine tags; a game only inherits a look change when it re-pins. So neutral
  defaults are safe to introduce at a bump - a game keeps its look until it upgrades, and the `Medieval` skin
  is the one-step path back for a game that wants the old identity.
