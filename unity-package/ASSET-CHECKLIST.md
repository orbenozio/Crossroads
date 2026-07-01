# Crossroads - asset & sound checklist

Exactly what a game built on this engine needs to prepare. Every visual/audio slot below is **optional** -
if you leave it empty the engine draws a procedural placeholder (or stays silent) - so you can run the loop
with zero art and add assets later. The only hard requirements to *run* are the three data files in section 1.

Canvas reference resolution is **portrait 540 x 960**. **Every image's width and height must be multiples of
4** (block compression). Real sizes below are taken from the shipped NewbornKing game; round to the nearest
nice multiple of 4. Audio is any Unity-supported format (the NewbornKing game uses `.mp3`).

---

## 1. Data - REQUIRED to run

- [ ] **`story.json`** - the deck/nodes, choices, resource deltas, flags, endings (`maxTurns` optional).
- [ ] **`resources.asset`** - the meters (ScriptableObject: `Create > Crossroads > Resource Set`). Usually **4**.
- [ ] **`theme.asset`** - the Theme object (`Create > Crossroads > Theme`). Required even if every art/audio
      slot inside it is left null - the bootstrap references it.
- [ ] **`map.json`** - **journey games only** (`startNodeId`, `goalNodeId`, `edges`).

---

## 2. Theme art (sprites) - one each, optional

Set on the `theme.asset`. Fallback = what you get if left null.

| ✓ | Slot (`theme.*`) | Purpose | Recommended size | If null |
|---|---|---|---|---|
| ☐ | `keyArt` | Backdrop for menu / title screens | **940 x 1672** (portrait 9:16, full-bleed) | flat panel |
| ☐ | `logo` | Title wordmark on the menu (instead of text) | **1380 x 960** (transparent PNG) | menu title text |
| ☐ | `cardArt` | Card background art | **1024 x 1536** (portrait 2:3) | flat card color |
| ☐ | `meterFrame` | Decorative frame behind each meter icon | **1024 x 1024** (square, transparent) | none |
| ☐ | `speakerFrame` | Medallion frame around the speaker portrait | **1252 x 1252** (square, transparent) | plain procedural ring |
| ☐ | `buttonSprite` | 9-sliced button plate | **2172 x 724** (wide, set 9-slice borders) | flat colored buttons |
| ☐ | `menuIcon` | Pause / menu button icon | **1252 x 1252** (square, transparent) | built-in three bars |
| ☐ | `loadingArt` | Full-bleed poster for the loading screen | **940 x 1672** (portrait) | falls back to `keyArt` |
| ☐ | `gameplayArt` | Backdrop for the gameplay screen (behind the card) | **940 x 1672** (portrait) | falls back to `keyArt` |

> Beyond art: the `theme.asset` also carries a semantic-role color palette and per-widget component styles
> (`medallion`, `meter`) - all optional, all inherit a neutral engine default when unset. Those are colors and
> metrics, not assets to prepare, so they live in `THEMING.md` (the theming & extensibility contract), not here.

---

## 3. Per-content-key art - one per key in your content

These are keyed lists on the theme. **Count = how many distinct keys your `story.json` uses.**

- [ ] **Resource / meter icons** - one per meter `id` (typically 4). On `theme.resourceLabels[].icon`.
      Square, **1252 x 1252**, transparent. (Same list also overrides each meter's `label`.) Null = text-only HUD.
- [ ] **Speaker portraits** - one per distinct `speaker` id used across `story.json` nodes. On
      `theme.speakers[].icon` (+ optional `tint`). Square, **512 x 512**. Null = procedural portrait.
- [ ] **Ending backdrops** - one per distinct ending `image` key in `story.json`. On `theme.endingArt[].art`.
      Portrait full-bleed, **1024 x 1536**. Null = falls back to `keyArt`.

> To know exactly which keys to deliver: list every `speaker` value and every ending `image` value in your
> `story.json`; that is your portrait set and your ending-art set.

---

## 4. Sound - the 5 Theme audio slots, optional

Set on the `theme.asset`. Any Unity-supported clip (NewbornKing uses `.mp3`).

| ✓ | Slot (`theme.*`) | When it plays | Type |
|---|---|---|---|
| ☐ | `music` | Looping gameplay track | loop |
| ☐ | `musicMenu` | Looping title / main-menu track (falls back to `music` if null) | loop |
| ☐ | `swipeSfx` | A choice is committed (the card slides off) | one-shot |
| ☐ | `cardSfx` | A new card appears | one-shot |
| ☐ | `clickSfx` | A UI button is pressed | one-shot |

> Tip: import the import settings via the `set_audio_import` authoring tool (streaming for music, decompress
> for short SFX).

---

## 5. App build identity - for a shippable build

Not part of the Theme; set on the project once (authoring tools handle the wiring).

- [ ] **App / launcher icon** - one **square** source PNG, **1024 x 1024** recommended (`set_app_icon` fills
      the cross-platform default + every Android kind: adaptive foreground/background, round, legacy).
- [ ] **Splash poster** - one **portrait full-bleed** title image (same shape as `keyArt`/`loadingArt`,
      up to 2048 px) via `set_splash`. Hiding the Made-with-Unity logo needs a Pro/Plus license.
- [ ] **`productName`** + **`applicationIdentifier.Android = com.crossroads.<gamekey>`** in ProjectSettings.

---

## 6. Typography (optional)

- [ ] `theme.tmpFont` (TMP_FontAsset) - per-game UI font. Null = the engine's default font.
- [ ] `theme.font` (legacy `Font`) - only if a component needs the non-TMP font.
- [ ] `theme.rightToLeft = true` for Hebrew/RTL (the bootstrap switches the UI to RTL before building it).
      Generate a Hebrew TMP SDF with the `create_hebrew_font` authoring tool.

---

## Authoring tools (do the wiring for you)

`create_resource_set`, `create_theme`, `create_hebrew_font`, `set_theme_art`, `set_theme_audio`,
`set_theme_button`, `set_theme_font`, `set_resource_icons`, `set_speaker_icons`, `set_ending_art`,
`set_app_icon`, `set_splash`, `set_audio_import`, `wire_game_scene` (Reigns) / `wire_journey_scene` (map),
`wire_loading`. These set ScriptableObject fields and asset references that the generic bridge tools cannot.
See the engine playbook (`../CLAUDE.md`) for the full build/press/release flow.
