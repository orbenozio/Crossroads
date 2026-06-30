# Crossroads Engine

A data-driven narrative decision engine for Unity, with a reusable procedural UI skin.

Two formats share one `EventEngine`:
- **Reigns-style** - a card with a speaker + text, swipe left/right, four resource meters, game over when a meter breaks (hits 0 or max).
- **Journey / roguelike map** - the same event engine wrapped in a node map with a goal to reach (Oregon Trail / FTL / Slay the Spire style).

Ship a new game by swapping **Content** (`story.json`, `resources.asset`, optional `map.json`) and a **Theme** asset. No engine code changes.

## Layout

- `Runtime/Engine` - `Crossroads.Engine`, the game-agnostic C# core (event loop, resources, save/load, validators, RNG, map graph). Zero references to UI or any game.
- `Runtime/UI` - `Crossroads.UI`, the procedural skin (card, meters, screens, swipe input, theme, fonts/branding).
- `Tests` - engine unit + play-mode tests (run via the Unity Test Runner when the package is embedded or added to `testables`).
- `Samples~/Template` - a minimal game to clone (import via the Package Manager "Samples" tab).

## Install (consumer project)

Add to the consuming project's `Packages/manifest.json`:

```json
"com.orbenozio.crossroads": "https://github.com/orbenozio/Crossroads.git?path=/unity-package#v0.1.0"
```

Pin a tag (`#v0.1.0`) so each game upgrades the engine on its own schedule.

## Build a game on top

1. Import the **Template Game** sample (or copy a `Content/` + `Theme` set).
2. Edit `story.json` / `resources.asset` / `theme.asset`.
3. Wire a scene with a `GameBootstrap` that feeds the content into `EventEngine`.

See **[ASSET-CHECKLIST.md](ASSET-CHECKLIST.md)** for the exact list of art and sound a game needs to prepare
(every slot, its purpose, recommended size, and the procedural fallback if you skip it).
