# Crossroads

מנוע החלטות נרטיבי מבוסס-דאטה ב-Unity 2D, מתוכנן לשכפול: מנוע אחד יציב, ומחליפים סיפור + Theme כדי לקבל משחק אחר. שני פורמטים שחולקים EventEngine מרכזי:

- **בנוסח Reigns** - קלף + דמות + טקסט, החלקה שמאל/ימין, 4 מדי-משאבים, המשחק נגמר כשמשאב נשבר.
- **מסע/מפת-אירועים רוגלייט** - אותו מנוע אירועים עטוף במפת-צמתים, יעד להגיע אליו; הגעה ליעד = ניצחון, שבירת-משאב = הפסד.

האפיון המלא: [docs/spec.md](docs/spec.md).

## מה כבר בנוי

ארבעה משחקים על אותו EventEngine, שני פורמטים, הכל מונע-דאטה:

| משחק | פורמט | תוכן |
|---|---|---|
| `_Template` | Reigns | שלד-תבנית להעתקה (energy/calm) |
| `NewbornKing` | Reigns | עוצר לתינוק-מלך, 15 קלפים, 4 מדים (sleep/sanity/money/baby, override Heir) |
| `Lighthouse` | Reigns | שומר-מגדלור, 7 קלפים, 4 מדים (oil/supplies/health/spirits, override Resolve) |
| `RefugeeRoad` | מסע | חציית-גבול, 6 צמתים + מפה מסתעפת, 4 מדים (fuel/food/health/hope) |

49 בדיקות (46 EditMode + 3 PlayMode) ירוקות.

## דרישות

- Unity 6000.3.7f1 (Unity 6.3 LTS). נעול ב-`ProjectSettings/ProjectVersion.txt`.
- החבילות (Input System, Newtonsoft Json, Test Framework, uGUI) נמשכות אוטומטית דרך `Packages/manifest.json` בפתיחה הראשונה.
- `unity-agent-bridge` - כלי CLI/MCP להנעת ה-Editor (אופציונלי; נדרש רק לבנייה פרוצדורלית של סצנות). ראה למטה.

## ארכיטקטורה - שלוש שכבות, תלות חד-כיוונית

הכלל הזהוב: המשחקים תלויים במנוע, אף פעם לא הפוך. הגבול נאכף קומפילטורית דרך asmdef ונבדק אוטומטית (J-ARCH-1/2).

```
   Games/* (Crossroads.Game.<Name>.asmdef - רק GameBootstrap.cs)
                 |  references
        +--------+--------+
        v                 v
  Crossroads.UI  --->  Crossroads.Engine
                 (אין reference חזרה)
```

- **Crossroads.Engine** - C# טהור, אגנוסטי למשחק ולפורמט: `EventEngine`, `GameState`, `Deck` (Reigns), `MapGraph` (מסע), `IEventSource`, `SaveSystem`, `StoryValidator`. כאן כל ה-state וכל הלוגיקה. אין reference ל-UI/Games.
- **Crossroads.UI** - הצגה בלבד: `CardView`, `ResourceBarView`, `SwipeInput`, `EndScreen`, `MessageOverlay`, `MapView`, `Theme`, `StoryLoader`, `MapLoader`, `ViewMapper`. מפנה ל-Engine בלבד; חסר-ידע על תוכן ספציפי.
- **Content** (פר-משחק, דאטה) - `story.json`, `resources.asset`, `theme.asset`, ולמסע גם `map.json`. שכפול = החלפת Content + Theme בלבד.

### למה המנוע מריץ שני פורמטים (G2)

ה-`EventEngine` תלוי בהפשטה אחת - `IEventSource` (מי בוחר את האירוע הבא):

- **Reigns**: `Deck` בוחר את הקלף התקף הבא לפי `appearWhen`/flags. הלולאה: `Resolve(side)` ואז `Advance()`.
- **מסע**: `MapGraph` מנווט לפי שכני-מפה (`map.json`) ויש יעד. הלולאה: `Resolve(side)`, חזרה למפה, ואז `EnterNode(nodeId)`. הגעה ליעד -> `GameOverReason.ReachedGoal` (ניצחון).

אותו `story.json`, אותו `CardView/ResourceBarView/SwipeInput/EndScreen`. ההבדל היחיד הוא ה-`IEventSource` ו-`map.json` כ-overlay - בלי לשנות את סכמת התוכן או את המנוע.

## איך מוסיפים משחק Reigns חדש (שכפול)

המטרה: משחק חדש = Content + Theme בלבד, אפס שינוי בקוד Engine/UI.

1. **העתק את תיקיית המשחק.** העתק `Assets/Games/_Template/` ל-`Assets/Games/<MyGame>/`. שנה ב-asmdef ובמרחב-השמות שב-`GameBootstrap.cs` את `Template` ל-`<MyGame>` (זה השינוי היחיד בקוד, והוא מבני בלבד).
2. **כתוב את התוכן** - `Content/story.json`: רשימת `nodes` (כל אחד 2 בחירות עם `effects`/`setFlags`/`appearWhen`) ו-`endings`. ראה דוגמה ב-NewbornKing/Lighthouse.
3. **צור את האסטים** (ScriptableObjects דורשים GUID מה-Editor) דרך ה-CLI:
   ```
   unity-agent-bridge create_resource_set path=Assets/Games/<MyGame>/Content/resources.asset defs="id:Name:min:max:start:breakOn:dangerBand|..."
   unity-agent-bridge create_theme path=Assets/Games/<MyGame>/Content/theme.asset labels="id=Label|..."
   ```
   לחלופין דרך התפריט: Create > Crossroads > Resource Set / Theme.
4. **בנה וחווט את הסצנה** דרך ה-CLI (Canvas + קלף נבנים פרוצדורלית; `wire_game_scene` מחווט את ה-references שאי-אפשר דרך set_property גנרי):
   ```
   unity-agent-bridge new_scene setup=default mode=single path=Assets/Games/<MyGame>/Scenes/Game.unity
   unity-agent-bridge create_canvas name=Canvas
   unity-agent-bridge create_gameobject name=Card ; unity-agent-bridge set_parent target=Card parent=Canvas
   unity-agent-bridge add_component target=Card componentType=Image ; unity-agent-bridge set_rect target=Card width=760 height=980 anchor=center
   unity-agent-bridge set_text target=Card text="..."
   unity-agent-bridge wire_game_scene bootstrapType=Crossroads.Game.<MyGame>.GameBootstrap storyPath=... resourcesPath=... themePath=... title="..." intro="..."
   unity-agent-bridge save_scene
   ```

`StoryValidator` רץ בזמן-טעינה: דאטה לא-תקין מציג מסך-שגיאה במקום כשל-שקט.

## איך מוסיפים מסע

כמו Reigns, אבל:

- ה-`GameBootstrap` הוא של פורמט-המסע (מתזמן מפה<->קלף; ראה `RefugeeRoad/GameBootstrap.cs`).
- מוסיפים `Content/map.json`: `{ startNodeId, goalNodeId, edges: { nodeId: [neighbor, ...] } }` - מפנה ל-`node.id` קיימים ב-story.json.
- מוסיפים ending ניצחון: `{ "when": { "reachedGoal": true }, "text": "..." }`.
- מחווטים דרך `wire_journey_scene` (מוסיף `MapView` + שדה `mapJson`).

## מודל הדאטה

- **story.json** (זהה לשני הפורמטים): `schemaVersion`, `startNodeId`, `nodes[]` (`id`, `body`, `speaker`, `appearWhen`, `choices[]` עם `side`/`label`/`effects[]`/`setFlags`/`next`), `endings[]` (`when:{resource,edge}` / `when:{reachedGoal}` / `fallback`).
- **resources.asset** (ScriptableObject): מערך `ResourceDef` - `id`, `displayName`, `min/max/start`, `breakOn` (Min/Max/Both), `dangerBand`. הסדר = סדר-המדים הקבוע.
- **theme.asset** (ScriptableObject): פלטה + overrides לתוויות-משאבים (Theme override > ResourceDef). החלפת Theme משנה מראה+תוויות בלי קוד.
- **map.json** (מסע בלבד): overlay מפה כמתואר למעלה.

## הרצה ובדיקות

- **הרצה**: פתח את סצנת המשחק (למשל `Assets/Games/NewbornKing/Scenes/Game.unity`) ולחץ Play. מסך-פתיחה -> Start -> swipe (גרירה/חצים) להחלטה.
- **בדיקות**: Window > General > Test Runner. ה-EditMode (46) רץ בלי playmode ובלי בריג'; ה-PlayMode (3) בודק את לולאות שני הפורמטים. דרך ה-CLI:
  ```
  unity-agent-bridge run_tests platform=EditMode
  unity-agent-bridge run_tests platform=PlayMode
  ```
  כיסוי עיקרי: ArchitectureTests (J-ARCH-1/2/3), EngineLoopTests, ResumeTests, SaveSystemTests, StoryValidatorTests, ViewMapperTests, JourneyTests (G2), ובדיקות-תוכן לכל משחק.

## העבודה דרך הבריג' (CLI)

`unity-agent-bridge <tool> key=value ...` מניע את ה-Editor (בנייה פרוצדורלית של סצנות, קומפילציה, בדיקות, screenshots). הכלים הפרוצדורליים (`create_canvas`, `set_rect`, `set_text`, ...) בונים את ה-UI; custom tools בפרויקט (`create_resource_set`, `create_theme`, `wire_game_scene`, `wire_journey_scene`, `open_scene`) ממלאים את מה שהכלים הגנריים לא יכולים (יצירת ScriptableObject, חיווט references). הם תחת `Assets/UnityAgentBridge/CustomTools/Editor/`.

## מבנה התיקיות

```
Assets/
  Engine/
    Runtime/  Crossroads.Engine.asmdef
    UI/       Crossroads.UI.asmdef
    Tests/    Crossroads.Engine.Tests.asmdef (EditMode) + Tests/PlayMode (PlayMode)
  Games/
    _Template/  NewbornKing/  Lighthouse/   (Reigns)
    RefugeeRoad/                            (מסע)
  UnityAgentBridge/CustomTools/Editor/      (custom bridge tools)
docs/
  spec.md            האפיון המלא (17 פרקים)
  bridge-wishlist.md מועמדים לשיפור הבריג'
tools/
  New-UnitySkeleton.ps1   גנרטור השלד
TASKS.md             מעקב משימות לפי שלבי-הבנייה
```

## הגנרטור (tools/New-UnitySkeleton.ps1)

השלד נוצר בעזרת גנרטור פרמטרי שמייצר את ה-boilerplate המבני (עץ-תיקיות, גרף asmdef, manifest, ProjectVersion) מהגדרה דקלרטיבית - הליבה לסקיל "בניית שלד פרויקט Unity":

```
pwsh ./tools/New-UnitySkeleton.ps1 -ProjectRoot 'C:\path\to\project'
```

ההגדרה (`$Packages` / `$Assemblies` / `$Folders` בראש הסקריפט) היא משטח-הפרמטרים: לפרויקט אחר משנים את רשימת ה-assemblies וגרף ה-references, וה-asmdefs נוצרים בהתאם.
