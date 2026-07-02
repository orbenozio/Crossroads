# Crossroads - agent playbook

This repo is the **Crossroads engine**, shipped as a git-distributed UPM package, plus the toolkit for
building games on top of it. Each game is its **own** Unity project repo that pulls this engine. This file
is the end-to-end playbook: how to build a new game, prepare a press kit, post/share, and release - and the
hard-won gotchas. (Writing-style rules - no em-dash, straight quotes, English code comments - come from the
global `~/.claude/CLAUDE.md` and still apply here.)

## The model

```
orbenozio/Crossroads        engine UPM package (unity-package/) + authoring toolkit   <- THIS repo
orbenozio/the-newborn-king  game project (the POC; its bridge tools are the template)
orbenozio/lighthouse        game project (Reigns-style)
orbenozio/refugee-road      game project (journey / map)
```

- **Engine** (`com.orbenozio.crossroads`) lives under `unity-package/`. Consumed via
  `https://github.com/orbenozio/Crossroads.git?path=/unity-package#vX.Y.Z`. Public repo.
- **One-directional dependency** (enforced by tests on both sides): a game depends on
  `Crossroads.Engine` + `Crossroads.UI`; the engine never references a game.
- A game = **Content (`story.json`, `resources.asset`, optional `map.json`) + a `Theme` + a thin
  `GameBootstrap` + a wired scene**. No engine code is copied into a game.
- Two formats share one `EventEngine`: **Reigns** (swipe a card, 4 meters, break = game over) and
  **journey** (same engine wrapped in a node map with a goal). Journey adds only `map.json`.

Current engine: **v0.6.1**. New games should pin `#v0.6.1` or later (>= v0.1.1 auto-imports TMP - see Gotchas).
The loading-bar marker is theme-driven too (`theme.loadingMarker`; null = a neutral procedural seal, not the
old crown - a game supplies its own marker art), and the loading fill/text follow the accent.
The theming is fully data-driven: a semantic-role palette (incl. `plaqueFill`/`plaqueEdge` so even the
procedural plaques repaint) + `MedallionStyle`/`MeterStyle` component styles + a gameplay backdrop on the
`Theme`; a pluggable `CardChoiceFeedback` hook (inject it via `GameShell.Config.choiceFeedback`, or a scene
component as fallback) replaces the choice-selection effect. The engine out-of-box default is **neutral** - the
original medieval look ships as the `Samples~/Medieval` skin `Theme`. Overrides are explicit (`OptionalFloat`/
`OptionalColor`; unset = inherit the engine default). See `unity-package/THEMING.md` for the decision rule
(token / style-struct / hook / core), the token model, and the compatibility policy.

---

## Build a new game (checklist)

Do this in a NEW folder under `C:\Dev\UnityProjects\<GameName>` - NOT inside this repo.

1. **Scaffold the project**
   - Copy `ProjectSettings/` from an existing game (or this repo) as a baseline. Set in `ProjectSettings.asset`:
     `productName: <Display Name>` and `applicationIdentifier.Android: com.crossroads.<gamekey>`.
     Portrait 540x960 is the house default.
   - `Packages/manifest.json`:
     ```json
     {
       "dependencies": {
         "com.orbenozio.crossroads": "https://github.com/orbenozio/Crossroads.git?path=/unity-package#v0.1.1",
         "com.orbenozio.unity-agent-bridge": "https://github.com/orbenozio/unity-agent-bridge.git?path=/unity-package#v0.2.0",
         "com.unity.inputsystem": "1.14.0",
         "com.unity.nuget.newtonsoft-json": "3.2.1",
         "com.unity.test-framework": "1.6.0",
         "com.unity.ugui": "2.0.0",
         "com.unity.modules.audio": "1.0.0", "com.unity.modules.imgui": "1.0.0",
         "com.unity.modules.jsonserialize": "1.0.0", "com.unity.modules.ui": "1.0.0",
         "com.unity.modules.uielements": "1.0.0", "com.unity.modules.unitywebrequest": "1.0.0"
       }
     }
     ```
   - `.gitignore`: ignore `[Ll]ibrary/`, `[Tt]emp/`, `[Bb]uild[s]/`, `[Ll]ogs/`, `[Uu]ser[Ss]ettings/`,
     `*.csproj`, `*.sln`, `itch/` (heavy build zips). Do NOT ignore `Assets/` content.

2. **Game code + content** under `Assets/Game/`
   - `Crossroads.Game.<GameName>.asmdef`: `"references": ["Crossroads.Engine", "Crossroads.UI"]`.
   - `GameBootstrap.cs` - the only game script. Serialized fields: `storyJson` (TextAsset), `resources`
     (ResourceSet), `theme` (Theme), `seed` (int), optional `mapJson` (journey), and the UI refs
     (`cardView`, `resourceBar`, `swipeInput`, `endScreen`, `messageOverlay`, `menuOverlay`,
     `pauseButton`, `audioDirector`, `loadingScreen`). Reigns uses `new Deck(story)`; journey uses
     `new MapGraph(story, map)` + `engine.EnterNode(id)`. Copy a game's bootstrap as the template.
   - `Content/story.json`, `Content/resources.asset`, `Content/theme.asset` (+ `Content/map.json` for journey).
   - `Scenes/Game.unity` - one scene with `GameBootstrap` wired to the content (by serialized refs) and UI.

3. **Tests** under `Assets/Game/Tests/` (asmdef `Crossroads.Game.<GameName>.Tests`, Editor platform,
   references Engine + UI + the game asm + TestRunner + nunit):
   - A content test (load `story.json`/`resources.asset` from `Assets/Game/Content/...`, run
     `StoryValidator.Validate`, assert meter count / start node / deck size). Journey games also load
     `map.json`, run `MapValidator.Validate`, and simulate the route to the goal.
   - A `GameArchitectureTests` asserting the game references Engine+UI and neither references back.
   - Copy from `the-newborn-king/Assets/Game/Tests/` as the template.

4. **Per-game build tools** under `Assets/Editor/` (loose scripts -> compile into `Assembly-CSharp-Editor`,
   which auto-references the bridge): `build_webgl.cs` + `build_android.cs`. Only the scene path
   (`Assets/Game/Scenes/Game.unity`), `productName`, app id, and apk name change per game. Copy from a game.

5. **Verify** (see next section), then `git init -b master`, `gh repo create orbenozio/<gamekey> --public
   --source=. --push`, and `git tag v0.1.0 && git push origin v0.1.0`.

---

## Content authoring

Source-of-truth is data. The engine validates it at load and in tests. Schemas:

**`story.json`** (nodes = the deck; choices carry resource deltas + flags; endings fire on a broken meter):
```json
{
  "schemaVersion": 1,
  "startNodeId": "intro_01",
  "maxTurns": 18,                         // optional reign length (a win condition)
  "nodes": [
    { "id": "intro_01", "speaker": "narrator", "body": "...", "weight": 1,
      "appearWhen": { "allOf": [ { "flag": "started_work", "is": true },
                                 { "resource": "energy", "op": ">=", "value": 1 } ] },
      "choices": [
        { "side": "left",  "label": "Rest",  "effects": [ { "resource": "energy", "delta": 2 } ] },
        { "side": "right", "label": "Work",  "effects": [ { "resource": "energy", "delta": -2 } ],
          "setFlags": { "started_work": true }, "next": "burnout_check" } ] }
  ],
  "endings": [
    { "when": { "resource": "energy", "edge": "min" }, "text": "You ran out of energy." },
    { "flag": "crowned", "text": "..." },         // branching ending keyed by a flag
    { "fallback": true, "text": "The story ends." } ]
}
```
- `appearWhen` gates a node on flags/resources (branching). `next` forces the following node (overrides the
  source). `setFlags` writes story state. Endings match a broken meter `edge` (`min`/`max`), a `flag`, or `fallback`.

**`resources.asset`** (ScriptableObject; create via `Create > Crossroads > Resource Set`, do NOT hand-write
YAML). Fields per meter: `id`, `displayName`, `min` (0), `max` (10), `start` (5), `breakOn`
(`Min`/`Max`/`Both`), `dangerBand`. The array order is the fixed meter order. Most games use 4 meters.

**`theme.asset`** (`Create > Crossroads > Theme`): Palette base roles (background/card/text/accent/approaching/
willBreak) + optional role overrides (textMuted, ring, divider, choiceHint, choiceGlow, hudPlate - unset falls
back to a base role so `accent` is not overloaded), Art sprites (keyArt, logo, cardArt, meterFrame,
speakerFrame, buttonSprite, menuIcon, loadingArt, gameplayArt), Audio (music, musicMenu, swipeSfx, cardSfx,
clickSfx), component styles (`medallion`: size/ringColor/ringThickness/innerFraction; `meter`: iconSize/
frameSize/iconTint), Typography (font, tmpFont, `rightToLeft` for Hebrew), Overrides (per-resource
labels/icons, per-speaker styles, per-ending backdrop art). All optional - `OptionalFloat`/`OptionalColor`
metrics inherit the engine default when unset (`set == false`), sprites fall back when null. The engine default
is a **neutral** look; the medieval identity ships as a skin `Theme` sample. See `unity-package/THEMING.md`.

**`map.json`** (journey only): `{ "startNodeId", "goalNodeId", "edges": { "node": ["next", ...] } }`.

**Exact asset & sound list:** see `unity-package/ASSET-CHECKLIST.md` - every art/audio slot a game needs,
its purpose, recommended size (from a shipped game), and the procedural fallback if skipped.

**Authoring toolkit** - bridge `[McpTool]`s under `Assets/UnityAgentBridge/CustomTools/Editor/` (built for the
NewbornKing POC, reused for every game). Generic: `create_theme`, `create_resource_set`, `create_hebrew_font`,
`wire_game_scene`, `wire_journey_scene`, `wire_loading`, `set_theme_art/audio/button/font`, `set_ending_art`,
`set_resource_icons`, `set_speaker_icons`, `set_splash`, `set_app_icon`, `set_art_quality`, `set_audio_import`,
`set_player_resolution`, `open_scene`, `make_white_transparent`, `import_tmp_essentials`, `resolve_packages`.
These author ScriptableObjects / wire scenes / set asset-reference fields - things the generic bridge tools
can't do. Copy/adapt them into a new game project's `Assets/Editor/` when authoring that game.

---

## Build & verify

- **Tests (authoritative):** run **batchmode**, never trust the bridge `run_tests` (it reports a single
  vacuous root node - useless counts):
  ```
  Unity.exe -batchmode -projectPath <proj> -runTests -testPlatform EditMode -testResults <out.xml> -logFile <log>
  ```
  Parse `<out.xml>` `test-run/@total,@passed,@failed`. A clean run also proves the engine resolved from git
  (check `Packages/packages-lock.json` -> `com.orbenozio.crossroads.source == "git"`).
- **Compile check (interactive):** with the editor open + bridge connected, `refresh_assets` then
  `compile_errors`. The bridge's `compile_errors` IS reliable; its `run_tests` is not.
- **Build a player:** `build_webgl` (folder with `index.html`, itch-ready Gzip + decompression fallback) or
  `build_android` (apk). Both block and also write `Builds/last_build.json` - poll that if the call times out.
- **TMP:** engine >= v0.1.1 auto-imports TMP Essential Resources on first open. Older pins need a manual
  `Window > TextMeshPro > Import TMP Essential Resources` (or the `import_tmp_essentials` tool).

---

## Press kit (per game, lives in the game's repo)

Prepare and commit under the game repo:
- `press/` (TRACKED): `cover.png`, `screenshot-1..N.png` (title, opening, a gameplay card, loading, ending),
  a short loop `*.gif`, and a `*.mp4` capture. These feed the itch.io page / README / posts.
- `itch/` (GITIGNORED): the WebGL build zip (regenerable, heavy - keep it out of git, near the game).
- `_screens/` (TRACKED): working dev screenshots if you want them versioned.

Rules & tools:
- **Every image's width and height must be multiples of 4** (better block compression) - crop generator
  output before import.
- **WebGL itch zip:** never `Compress-Archive` (backslash paths -> loader 404 / grey screen). Zip with
  `ZipArchive` using forward-slash entry names.
- **Video:** the `autocut` skill cleans a screen recording (cut silences, Hebrew captions, vertical clip).
- **Images:** the `generate-image` skill drives ChatGPT for art/cover generation.
- **APK install for QA:** `install-apk` (clean) / `reinstall-apk` (keep data).

---

## Posts & sharing

- **LinkedIn:** the `linkedin-post` skill turns work in the current project into an EN engagement post and
  opens the composer. Run it from the relevant game repo so it has the right context.
- **Voice:** the `humanizer` skill rewrites anything sent on the user's behalf (Slack, commits, PRs, posts)
  into his voice - it triggers automatically before sending/committing on his behalf.
- **Style (global):** plain hyphens only (no em/en-dash), straight quotes, never sign commits as AI, commit
  bodies as bullet points, code comments in English (chat/docs may be Hebrew; Hebrew docs read RTL, clean
  Markdown, no HTML wrappers).

---

## Release flow

- Each game versions and tags **independently** (`v0.1.0`, ...) and pins an engine version. That pin is what
  makes the three releases independent.
- **Engine change:** edit `unity-package/`, bump `package.json` version, commit, `git tag vX.Y.Z`, push tag +
  master, keep the repo public. Then in each game that wants it, re-pin `manifest.json` to the new tag,
  re-resolve (open the editor once), and commit the updated `packages-lock.json` so manifest + lock agree.
- All four repos are public so Unity resolves the git URLs with zero auth.

---

## Gotchas (hard-won)

- **Unity won't re-resolve `manifest.json` live.** After editing a manifest externally, `AssetDatabase.Refresh`
  and even window focus do NOT reliably re-resolve packages. **Restart Unity** - a fresh launch always resolves.
- **bridge `run_tests` is unreliable** (single vacuous root node). Use batchmode `-runTests` + parse the XML.
- **Embedded/local package tests** only appear in the runner if the consuming project lists the package under
  `"testables"` in the manifest. (Game tests live in `Assets/`, so games don't need this; the engine dev
  project does.)
- **Any compile error blocks the whole domain reload**, so a newly added bridge tool won't load while other
  scripts are broken. Fix all errors first.
- **Move files WITH their `.meta`** to preserve GUIDs - scene/asset references survive. (When extracting a game
  from history: `git archive <commit> <path> | tar -x -C <dest> --strip-components=N`.)
- **TMP essentials** were missing in fresh consumer projects; fixed by the auto-importer in engine v0.1.1
  (`unity-package/Editor/TmpEssentialsAutoImport.cs`) and by committing the essentials into each game repo.
- **Per-game identity:** set `productName` + `applicationIdentifier.Android = com.crossroads.<gamekey>`; the
  build tools also set these at build time.

See `docs/spec.md` for the original engine spec, `docs/bridge-wishlist.md` for logged bridge gaps.
