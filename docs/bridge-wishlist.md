# Unity Agent Bridge - wishlist

רשימה חיה של מועמדים ל-TOOL / built-in COMMAND / CUSTOM COMMAND שעלו תוך כדי פיתוח Crossroads.
עוברים עליה בבאצ'ים מסודרים; מועמדים עשויים להיכנס ל-package של unity-agent-bridge.
כל פריט עם העדות הקונקרטית (מה נכשל / כמה צעדים זה עלה).

## מומש בפרויקט (engine v0.4.0 - כלי theming חדשים)
- [x] **`set_theme_role`** - הגדרת צבע-role ב-Theme (base: background/card/text/accent/approaching/willBreak;
  optional overrides: ring/divider/choiceHint/choiceGlow/hudPlate/textMuted). עדות: פלטת ה-roles החדשה של
  v0.4.0 עוטפת צבעים ב-`OptionalColor`, ו-set_property הגנרי לא מגיע ל-wrapper. מומש כ-custom tool.
- [x] **`set_component_style`** - הגדרת מטריקה ב-structs `medallion`/`meter` (size/ringThickness/innerFraction/
  ringColor, iconSize/frameSize/iconTint). עדות: השדות הם `OptionalFloat`/`OptionalColor` מקוננים ב-struct;
  ה-set_property הגנרי לא מגיע אליהם. מומש כ-custom tool. (מועמד להכללה ב-package של הברידג' אם יופיע דפוס דומה.)
- [x] **`set_theme_art` + `gameplayArt`** - נוסף slot ל-backdrop של מסך-המשחק.

## נכנס כבר ל-v0.2.0
- `create_canvas` - היה: בניית Canvas ב-5 צעדים שמגדירה World Space כברירת-מחדל.
- `set_color` (hex) - היה: צבע דרך JSON עם escaping של PowerShell.
- `set_property` עם שמות ציבוריים - היה: רק שמות serialized (`m_Color`).
- `new_scene`.
- `build_reigns_frame` - מומש כ-custom command בפרויקט (UnityAgentBridge/Commands/).

## פתוח - מועמדים לבאצ' הבא

### Tools
- [ ] **`create_ui_image`** - קריאה אחת ל-create_gameobject + set_parent + add Image + set_rect + set_color(hex).
  עדות: כל מד-משאב = 5 קריאות; `build_reigns_frame` הוא 29 צעדים שרובם boilerplate כזה. יכווץ קלף/מד ל-1-2 קריאות.
- [ ] **`set_text` עם color/fontSize/align אופציונליים בקריאה אחת.**
  עדות: אחרי כל `set_text` עדיין צריך 2 `set_property` (צבע + `m_FontData.m_FontSize`).
- [ ] **alias ל-`fontSize`** ב-set_property ל-Text -> `m_FontData.m_FontSize` (מקונן; היוריסטיקת `m_`+Capitalize מפספסת אותו).
- [ ] **`create_scriptable_object`** - יצירת asset של ScriptableObject עם ערכי-שדות (למשל resources.asset).
  עדות: כדי ליצור resources.asset נדרש custom C# tool; אין דרך לברידג' ליצור+לאכלס SO.
- [ ] **`set_reference` / `wire`** - הזרקת reference לשדה serialized (אובייקט-סצנה לפי instanceId / asset לפי path).
  עדות: set_property מטפל בערכים (color/מספרים) אך לא ב-object references; חיווט CardView.bodyText->Label ו-GameBootstrap.storyJson->TextAsset חייב SerializedObject ב-custom tool (setup_template_scene).

### CLI / run_command ergonomics
- [ ] **override פרמטרים ל-`run_command` בלי JSON blob** (למשל `run_command name=X title="..."`).
  עדות: העברת `title` מותאם לפקודה דרך ה-CLI נתקלת ב-quoting של PowerShell (`--%` + `\"`).

### Tools (המשך)
- [ ] **`open_scene`** - פתיחת סצנה קיימת (EditorSceneManager.OpenScene). יש new_scene + save_scene אבל אין פתיחה.
  עדות: בלי זה אי-אפשר לחזור לסצנה נקייה לפני run_tests platform=PlayMode, וכניסה ל-Play Mode עם סצנה "מלוכלכת" מקפיצה דיאלוג מודאלי ("Save scene?") שחוסם את Unity וגורם ל-timeout. מומש כ-custom tool open_scene בפרויקט. שקול גם: run_tests/run_playmode שישמרו/ינקו את הסצנה הפעילה אוטומטית כדי למנוע את הדיאלוג.

- [ ] **דגל `saveScenePrompt: never|always|ask` לכלים שעלולים לפתוח את מודאל "Save Scene?"** (run_tests, run_playmode, וכל מעבר-מצב). ברירת-מחדל ל-agent-sessions: `never` (זרוק). הרציונל: ה-bridge רץ על ה-main thread, וברגע שהמודאל פתוח ה-thread חסום - אי אפשר "לענות" על הדיאלוג מבחוץ, אז הפתרון חייב להיות מניעה ולא תגובה.
  עדות (2026-06-29, חוזר): אחרי 12 staging-captures של מסך-הסיום (nbk_end קורא EditorUtility.SetDirty), `run_tests platform=EditMode` נתקע ב-"Unity did not respond within the call timeout" - המודאל "Save Scene?" היה פתוח וחסם את ה-bridge. נדרשה התערבות ידנית של המשתמש (Don't Save). עקיפה מקומית שיושמה: כלי התצוגה המקדימה (nbk_card/nbk_menu/nbk_end) מנקים את דגל ה-dirty (EditorSceneManager.ClearSceneDirtiness) בסוף ה-staging, כך שהמודאל לא מופיע כלל. הכלל הראוי בליבה: כלים שמשנים סצנה ב-edit-mode לצורך תצוגה בלבד לא צריכים להשאיר אותה dirty.
- [ ] **`scene_state`** - כלי קריאה שמחזיר `{ path, isDirty, isLoaded }` לסצנה הפעילה. עדות: כדי לדעת מתי בטוח להריץ run_tests / לזרוק / לשמור צריך לדעת אם הסצנה dirty; כרגע אין דרך לשאול, מגלים רק כשהמודאל כבר תקע את הברידג'.

### באגים (לא רק חוסרים)
- [ ] **`call_tool` לא מעביר את `args` לכלי** (v0.2.0, חמור). קריאה ל-`call_tool` עם `args:{path:...}` הגיעה לכלי עם פרמטרים ריקים (לוג: `invoked path='' labels=''`). אותו דבר ל-`list_scene` עם `maxDepth=2` (התעלם, השתמש ב-default 4), ו-`demo_swipe` רץ על defaults בלי קשר ל-args שנשלחו. תוצאה: אי-אפשר להפעיל custom tool פרמטרי דרך `call_tool`. עקיפה זמנית: custom tool פרמטרלי (defaults בלבד) שקורא לכלים הגנריים in-process ב-C#. צריך לבדוק אם זה ב-MCP server (Node) או בפרוטוקול הברידג'. כנראה משפיע גם על שאר הכלים הגנריים (set_property וכו') כשנקראים דרך call_tool ולא דרך forwarder ייעודי.
