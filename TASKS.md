# TASKS - Crossroads

מסמך מעקב משימות חי, נגזר מה-Roadmap באיפיון ([docs/spec.md](docs/spec.md) §17).
כל שלב ממופה ל-slug של `/phase` למדידת זמן נטו. סמן `[x]` כשמשימה הושלמה.

> שלב נוכחי: phase-5-menus (תפריטים / שמירה / תוכן + ויזואל ל-NewbornKing). להגדרה: `/phase phase-5-menus`.

## Roadmap

| שלב | /phase | סטטוס |
|---|---|---|
| Setup - איפיון + שלד + בריג' | setup | בוצע |
| 0a - Hello-bridge | phase-0a | בוצע |
| 0b - asmdef + לולאה ב-EditMode | phase-0b | בוצע |
| 1 - Engine Core | phase-1 | בוצע - נוכחי |
| 2 - Skin/UI פרוצדורלי | phase-2 | בוצע (נשאר: אנימציית קלף; prefabs - החלטה) |
| 3 - NewbornKing content + Theme | phase-3 | בוצע (חסר: מדידות M על ריצה אמיתית) |
| 3.5 - קלון Reigns שני (Lighthouse) | phase-3.5 | בוצע - נוכחי |
| 4 - Hook למסע | phase-4 | בוצע ברמת-מנוע - נוכחי (UI מסע/מלאי = LATER) |

## Todo

### Phase 1 - Engine Core (phase-1) - בוצע (ראה Done)

### Phase 2 - Skin/UI פרוצדורלי (phase-2)
- [ ] (החלטת-עיצוב פתוחה) prefabs ל-UI/Prefabs/ - המלצה: לוותר. ה-UI בונה את עצמו פרוצדורלית (find-or-create), אז prefab הוא כפילות שה-runtime לא משתמש בה. נשאר ב-Todo עד החלטת המשתמש

### Phase 3 - NewbornKing content + Theme (phase-3)
- [ ] מדידת M1/M3/M5 על ריצה אמיתית (קלף-ראשון -> game-over -> restart חי בתוך המשחק)

### Phase 3.5 - קלון Reigns שני (phase-3.5) - בוצע (ראה Done)

### Phase 4 - Hook למסע (phase-4) - בוצע (מנוע + UI מסע; ראה Done)
- [ ] [LATER] מסך-מלאי (§9.6) - נדחה: לתוכן RefugeeRoad הנוכחי אין פריטים; ייכנס כשיתווסף מודל-פריטים
- [ ] [LATER] קישוטי-מפה: קווי-מסלול בין צמתים (כרגע פריסת-עומק בלבד, בלי קווים)

## In progress

(אין משימה פעילה כרגע)

## Done

### Phase 5 - תפריטים / שמירה / תוכן + ויזואל (phase-5-menus)
- [x] תנאי-ניצחון data-driven (ספק 7.5): `maxTurns` ב-story.json + GameState.Turn; הגעה לאורך-הריצה = ניצחון ("היורש בא בבגרות"). תאימות-לאחור (0 = ללא מגבלה; שמירות ישנות נטענות Turn=0)
- [x] סיומים מסתעפים: `when:{flag:"x"}` - ב-MaxTurns נבחר סיום-flag ראשון שמצב-הדגל מתאים לו, אחרת fallback. גנרי (כל משחק יכול). +3 בדיקות (WinConditionTests)
- [x] MenuOverlay (גנרי, רב-כפתורים): תפריט-ראשי (Continue/New Game/Quit) + pause (Resume/Restart/Main Menu) + דיאלוג-אישור; בחירה גם במקשי 1-9 (נגישות, מקביל למפה). +2 בדיקות
- [x] PauseButton (גנרי, בונה-את-עצמו, אייקון ניטרלי-שפה) בפינה + SwipeInput.OnMenu (Esc פותח pause)
- [x] EndScreen: כפתור "Main Menu" אופציונלי בנוסף ל-Restart (תאימות-לאחור - Show(text,onRestart) נשמר)
- [x] זרימת-שמירה ב-bootstrap: Continue מופיע רק כשיש שמירה תקפה; New Game עם שמירה קיימת מבקש אישור; Main Menu שומר את השמירה (אפשר להמשיך). guard לקלט בזמן שתפריט פתוח. עודכן ב-3 ה-Reigns bootstraps (RefugeeRoad/מסע ללא שינוי)
- [x] ויזואל NewbornKing: רקע מנושא, רצועת-HUD עליונה, תווית-דובר על הקלף (מוסתרת ל-narrator), תוויות-בחירה קבועות בפינות שמתבהרות בהטיה; Theme.accent + פלטה מלכותית (נייבי+זהב) + tint-ים לדוברים
- [x] תוכן NewbornKing: הורחב ל-28 קלפים (chain-ים חדשים: betrothal/plague/chapel/bandits/famine/war/spy/tutor + flags beloved/pious/tyrant/betrothed) + 6 סיומי-flag
- [x] wire_game_scene הורחב: רקע, תוויות-קלף, MenuOverlay+PauseButton, showMenu לצילום; ResourceBarView עמיד ל-re-wire (cache עמיד לאובייקטים שנמחקו)
- [x] **ממצא M3 נסגר**: חציון משחק-זהיר 39 -> 14 (יעד 10-15). qa-report עודכן. screenshots: nk_gameplay / nk_mainmenu / nk_swipe_preview. **72 בדיקות ירוקות (69 EditMode + 3 PlayMode)**

### Setup
- [x] איפיון מאושר (docs/spec.md) - product/ux-designer/architect + שער ביקורת + פס-תיקון
- [x] שלד פרויקט: 4 asmdefs (Engine/UI/Game/Tests) + generator (tools/New-UnitySkeleton.ps1) + README; מתקמפל נקי
- [x] unity-agent-bridge מותקן (v0.2.0) + custom command build_reigns_frame

### Phase 0a - Hello-bridge (phase-0a)
- [x] GameObject + UI דרך הבריג' + screenshot (פריים Reigns placeholder)

### Phase 0b - asmdef + לולאה (phase-0b)
- [x] גרף asmdef חד-כיווני + EventEngine מינימלי; 9 בדיקות EditMode ירוקות (כולל J-ARCH-1)

### Phase 1 - Engine Core (phase-1) - מלא
- [x] EventEngine: Preview (pure) / Resolve / Advance / EnterNode + ניתוב next מפורש
- [x] Deck + IEventSource + appearWhen/flags; MapGraph ממומש בפועל (Phase 4)
- [x] StoryValidator (חוקי §14.1) + SaveSystem round-trip + J-ARCH-3 (כולל בדיקת ה-gap)
- [x] כיסוי M9: 3 בדיקות ולידציה + ResourceRules tests
- [x] בחירה משוקללת (weight) + RNG דטרמיניסטי: DeterministicRng (xorshift, seed+drawCount, fast-forward ב-ctor); Deck בוחר משוקלל מבין הקלפים התקפים, שומר drawCount חזרה ל-state. דטרמיניסטי (אותו seed -> אותו רצף), save/resume ממשיך רצף זהה (fast-forward, M5). +5 בדיקות. 51 EditMode + 3 PlayMode ירוקים

### Phase 2 - Skin/UI פרוצדורלי (phase-2)
- [x] פריים Reigns placeholder דרך הבריג' (Canvas + קלף + 4 מדים) נבנה ונשמר כסצנה Game.unity
- [x] GameBootstrap מחובר (custom tool setup_template_scene) + resources.asset; הקלף הראשון מהמנוע נרנדר ב-edit וב-playmode (0 errors); תוכן באנגלית
- [x] swipe interaction מלא: גרירה+הטיה (preview), commit/cancel, fallback מקלדת (חצים); מעבר קלפים דרך Resolve/Advance מאומת (intro_01 -> work_push); playmode נקי
- [x] ResourceBarView מבוסס-דאטה: מד לכל משאב (fill לפי ערך + מסמן-סכנה !/!! לא-תלוי-צבע); מתעדכן עם כל בחירה (Energy 6->4, Calm 6->5); playmode נקי
- [x] מסך סיום (EndScreen): overlay + טקסט-סיום (נבחר לפי המשאב שנשבר) + כפתור Restart (StartRun); מאומת מקצה-לקצה (4 commits -> energy=0 -> "You ran out of energy" + Restart); playmode נקי
- [x] חיבור SaveSystem ללולאה (J4/M5): EventEngine.Resume(GameState) טעון + GameBootstrap load-on-start / save-on-commit / delete-on-gameover+fresh; fallback לריצה טרייה כשהצומת השמור לא קיים, ו-backfill ל-start כשנוסף מד אחרי השמירה (תוכן השתנה); save-on-commit מוגן מ-Advance שמסיים (לא משחזר ריצה שהסתיימה). +6 בדיקות (Resume x5 + disk round-trip)
- [x] קשיחות edge: default-ending אגנוסטי באנגלית (היה עברית - RTL שבור) + warning ב-StoryValidator כשאין ending fallback; +2 בדיקות
- [x] תיקון AppliedDeltas: מחזיר את ה-delta בפועל אחרי clamp (לא נומינלי) + בדיקה

### Phase 3 - NewbornKing content + Theme (phase-3)
- [x] story.json: 15 קלפים אנגלית + flags (ambitious/taxed/spurned_varic...) + appearWhen מותנה-משאב/flag; 4 מדים (sleep/sanity/money/baby); 6 endings (5 ספציפיים + fallback)
- [x] resources.asset (4 מדים, sleep breakOn Both והשאר Min) + theme.asset עם override 'Baby'->'Heir' (J8); נוצרו דרך custom tools create_resource_set/create_theme
- [x] קלון: asmdef Crossroads.Game.NewbornKing + GameBootstrap (העתקה מדויקת, רק namespace) - אפס קוד Engine/UI משלו
- [x] J-ARCH-2: בדיקה שלקלון יש רק GameBootstrap.cs ושה-Engine/UI לא מפנים לאף Game; +בדיקות ולידציית-תוכן NewbornKing (M9 על דאטה אמיתית)
- [x] סצנה Game.unity נבנתה דרך CLI (new_scene/create_canvas/...) + custom tool wire_game_scene (גנרי לפי טיפוס-bootstrap); נרנדר ב-edit (Heir override נראה במד) + playmode נקי. 35/35 בדיקות ירוקות

### Phase 3.5 - קלון Reigns שני: Lighthouse (phase-3.5)
- [x] משחק שני שלם (M6/M7): שומר-מגדלור לבד על סלע - 7 קלפים אנגלית + appearWhen מותנה-משאב; 4 מדים אחרים (oil/supplies/health/spirits); 5 endings + fallback
- [x] resources.asset + theme.asset עם override 'Spirits'->'Resolve' (J8 שני), נוצרו דרך ה-CLI (create_resource_set/create_theme - args עוברים ב-CLI)
- [x] asmdef Crossroads.Game.Lighthouse + GameBootstrap (העתקה מדויקת מ-_Template, רק namespace) - אפס קוד Engine/UI
- [x] J-ARCH-2 הוכלל: בודק שכל Crossroads.Game.* (Template/NewbornKing/Lighthouse) מכיל רק GameBootstrap.cs ותלוי ב-Engine+UI; +GamesContentTests גנרי (מגלה ומאמת תוכן כל המשחקים)
- [x] סצנה דרך CLI + wire_game_scene; נרנדר (Oil/Supplies/Health/**Resolve** - ה-override נראה) + playmode נקי. 40 EditMode + 2 PlayMode ירוקים

### Phase 4 - Hook למסע: ליבת-מנוע + G2 (phase-4)
- [x] MapGraph אמיתי (היה stub): IEventSource + IGoalAwareSource, ניווט מפורש לפי שכני-מפה, מצב-מיקום ב-bag האטום GameState.MapState; MapData + MapLoader (map.json overlay, ספק 14.3)
- [x] ניצחון-מסע (ספק 7.5.6): GameOverReason.ReachedGoal + Ending.ReachedGoal (when:{reachedGoal:true}, R6) + StoryLoader parse; ה-EventEngine בודק יעד אחרי כל כניסה-לצומת (no-op ב-Reigns - Deck אינו goal-aware)
- [x] **G2 מוכח**: אותו EventEngine מריץ מסע דרך MapGraph - הגעה ליעד=ניצחון, שבירת-משאב=הפסד (אותה בדיקה), save/resume של מסע דרך אותו SaveSystem (mapState round-trip + re-entry). 5 בדיקות
- [x] תוכן RefugeeRoad אמיתי: story.json (6 צמתים, מפה מסתעפת forest->river/town) + map.json + resources (fuel/food/health/hope) + theme override hope->Morale; בדיקת מסע-מלא מקצה-לקצה עד haven=ניצחון. 46 EditMode + 2 PlayMode ירוקים
- [x] אפס שינוי בחוזה Reigns: story.json לא השתנה, ה-Deck/לולאת-Reigns לא נגעו; המסע = overlay בלבד (מוכיח G2 כפי שתוכנן באיפיון §12.4/14.3)

### Phase 4 - UI מסע משוחק (phase-4)
- [x] MapView (§9.6): מסך-מפה פרוצדורלי - פריסת-עומק BFS, מיקום נוכחי מודגש, יעד מסומן (זהב), שכנים בני-הגעה לחיצים, צמתים אחרים disabled אך גלויים; אגנוסטי-תוכן (מקבל MapData + מצב)
- [x] RefugeeRoad GameBootstrap למסע + asmdef: מתזמן מפה<->קלף (Resolve -> מפה -> בחירת-צומת -> EnterNode -> קלף), ניצחון/הפסד דרך אותו EndScreen. אין לוגיקת-מנוע - רק תזמון מסכים
- [x] סצנת RefugeeRoad חיה דרך CLI + custom tool wire_journey_scene; screenshot של המפה (border->checkpoint->forest->river/town->haven) + playmode נקי
- [x] בדיקת PlayMode לזרימת-המסע (opening -> קלף -> commit -> מפה -> בחירת-צומת -> קלף הבא) - שני הפורמטים משוחקים על אותו EventEngine
- [x] הקשחת-מסע: MapValidator (M9 למפה) - מאמת ש-start/goal/קצוות מפנים לצמתים אמיתיים, שהיעד בר-הגעה, ושיש reachedGoal ending; מחובר לולידציית-הטעינה של ה-journey bootstrap. + טיפול במבוי-סתום (EnterNode לצומת בלי המשך שאינו היעד -> NoMoreEvents מבוקר, לא מפה תקועה). +5 בדיקות
- [x] נגישות-מקלדת למפה (§11.4): מקשי 1-9 בוחרים שכן בני-הגעה (מקביל ל-fallback החצים ב-swipe); הצמתים הלחיצים ממוספרים "[N]". SelectByIndex מאחד click+keyboard. +3 בדיקות. **59 בדיקות ירוקות (56 EditMode + 3 PlayMode)**

### תמיכת RTL/עברית (TMP) - §10.6
- [x] מיגרציה מ-legacy UnityEngine.UI.Text ל-TextMeshPro בכל רכיבי ה-UI (CardView/ResourceBarView/EndScreen/MessageOverlay/MapView)
- [x] TMP Essentials יובאו (custom tool import_tmp_essentials); פונט עברי דינמי HebrewUI SDF (custom tool create_hebrew_font מ-arial - **פונט-dev placeholder**, להחליף ב-Noto/Heebo לפני הפצה, NG7)
- [x] UIFonts (ספק-פונט מרכזי מ-Resources + דגל RightToLeft גלובלי); Theme.rightToLeft מניע RTL; כל ה-bootstrap-ים מעבירים theme.rightToLeft ל-UIFonts לפני בניית ה-UI
- [x] wire tools יוצרים TMP body + מנקים UI פרוצדורלי ישן (CleanOldUi) למיגרציה; create_theme עם rtl param; כל הסצנות חוברו-מחדש ל-TMP
- [x] הדגמה: story_he.json + theme_he (RTL, אנרגיה/רוגע) + סצנה Game_he - **עברית מתרנדרת RTL נכון** (screenshot); אנגלית עדיין LTR (screenshot NewbornKing). 59 EditMode + 3 PlayMode ירוקים, playmode נקי

### QA - פס קבלה פורמלי (docs/qa-report.md)
- [x] QaMetricsTests: M1 (60 זרעים - כל ריצה מסתיימת נקי, אין מבוי-סתום) + M3 (אורך-סשן משחק-זהיר)
- [x] דוח QA מלא ממפה M1-M10 לעדות: 9/10 עוברים (מבני/אוטומטי), M2 דורש בודקים אנושיים. M8 אומת ב-grep (אפס תוכן קשיח ב-Engine/UI)
- [x] **ממצא M3**: חציון משחק-זהיר של NewbornKing = 39 בחירות (יעד 10-15) - תוכן סלחני לשחקן-זהיר; ממצא-כיול-תוכן לא-חוסם, מתועד. אין באגי-קוד פתוחים

### תצוגת-delta על הקלף + זרימת-מקלדת בדידה (§10.2/10.3)
- [x] ViewMapper.FormatDeltas (deltas -> מחרוזת "תווית סימן-ערך" עם קדימות-Theme); +2 בדיקות
- [x] CardView.ShowPreviewDeltas: בזמן swipe מציג בפינת-הקלף (ימין/שמאל לפי הצד שאליו גוררים) אילו מדים הבחירה תשנה; נעלם ב-ResetDrag. מחובר ב-4 ה-bootstrap-ים (HandlePreview) - גם על הקלף וגם על המדים. screenshots לשני הכיוונים
- [x] SwipeInput זרימת-מקלדת בדידה (§10.2): חץ = preview (רואים מה משתנה), חץ-שני-לאותו-צד או Enter = commit, חץ-נגדי = החלפה, Escape = cancel (היה: חץ = commit מיידי)

### גימור ותיעוד
- [x] אנימציית כניסה לקלף (CardView): פופ ease-out (slide+scale) ב-play בלבד; edit/בדיקות מקבלים מצב סופי ממורכז. ה-exit מתבטא בגרירת-המשתמש לפני ה-commit
- [x] README מלא ומדויק: מה זה, ארכיטקטורת 3-שכבות, **מדריך שכפול משחק חדש (Reigns + מסע) צעד-אחר-צעד דרך ה-CLI**, מודל-דאטה, הרצה+בדיקות. הקאפסטון ל"מנוע לשכפול" (G4)

### Phase 2 - השלמות (phase-2)
- [x] תצוגת delta בזמן swipe (§10.3): ResourceBarView.ShowPreview/ClearPreview - כל מד מושפע מציג שינוי צפוי (Money +1 / Sanity -1); מחובר ב-HandlePreview/HandleCancel בשני ה-bootstrap-ים; +2 בדיקות + screenshot
- [x] בדיקות PlayMode אוטומטיות: GameLoopPlayTests (asmdef PlayMode נפרד) - בונה rig + מניע GameBootstrap האמיתי דרך SwipeInput.Commit; בודק מעבר-קלף + הגעה ל-game-over + gating של מסך-הפתיחה (לא מתחיל עד Start). 2 בדיקות ירוקות
- [x] מסך פתיחה + מסך שגיאת-דאטה (§9.5): MessageOverlay (כותרת+גוף+כפתור אופציונלי) - מסך-פתיחה עם Start שמעכב את הריצה, ומסך-שגיאה (בלי כפתור) במקום כשל-שקט בולידציה; מחובר בשני ה-bootstrap-ים (null-safe, תאימות-לאחור). +2 בדיקות EditMode + screenshot; 39 EditMode + 2 PlayMode ירוקים
- [x] layout אדפטיבי: מושג דרך anchors + CanvasScaler (ScaleWithScreenSize) - מדים מעוגנים top-stretch לרוחב מלא, קלף ממורכז ומתורגם לפי גודל-מסך. מאומת בצילום ב-960x540 (landscape) וב-540x960 (portrait); הקלף נשאר portrait (נורמת ז'אנר Reigns). relayout-לפי-אוריינטציה אמיתי = polish עתידי לא-הכרחי

### תשתית בריג'
- [x] custom tools: create_resource_set, create_theme (יצירת ScriptableObject דאטה-דריבן), wire_game_scene (חיווט bootstrap גנרי), setup_games_content, open_scene, demo_swipe (פרמטרי)
- [x] מעבר לעבודה דרך ה-CLI (`unity-agent-bridge <tool> key=value`) במקום MCP; תועדו באגים/חוסרים (call_tool args, open_scene, דיאלוג save-on-play) ב-docs/bridge-wishlist.md
