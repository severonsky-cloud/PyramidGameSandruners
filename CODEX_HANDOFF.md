# SAND RUNNERS — передача смены (от Claude, 2026-07-10)

Отчёт для следующего агента (Codex). Проект: Unity 6.5 (6000.5.2f1), сцена `SampleScene`,
весь код среза — `Assets/Scripts/SandRunners/*.cs`, партиалы класса `SandRunnersPrototype`.
Компиляция чистая, плеймод проверен, исключений нет. Редактор оставлен ВНЕ плеймода.

## Что было сломано и починено

1. **CS0619 в `SandRunnersVerticalSliceFactions.cs:281`** — твой `GetInstanceID()` запрещён
   в Unity 6.5 (обсолет как ошибка). Каст `(int)GetEntityId()` тоже запрещён.
   Рабочая замена: `GetHashCode()` (возвращает тот же instance id).
   **Правило: в этом проекте не использовать `GetInstanceID()` вообще.**

2. **Баржи жужжеров были беззубые** — урон был `22f * dt` внутри гейта `fireCooldown` (раз в 0.9с),
   т.е. ~0.4 урона за выстрел. Исправлено на плоские `22f` за выстрел.
   **Правило: урон либо `X * dt` каждый кадр, либо плоский `X` под кулдауном — не смешивать.**

## Что добавлено (глобальная прокачка среза)

### Cruise Missile Silo — новая постройка (просьба Венеции: «пусковая строится отдельно»)
- `StructureKind.CruiseMissileSilo` в `SandRunnersRTSCore.cs`: имя/цена (170/190/18)/время (20с)/
  здоровье (460)/футпринт/визуал (`BuildStructureVisual`, ветка Silo_*) — рампа с 2 направляющими.
- Кнопка «Missile Silo» в командной панели (`SandRunnersStrategicCanvas.cs`, ряд построек).
- `GetCruiseLaunchPoint(bool nuclear)` — крылатые (Z / кнопка Cruise) и TV-ракета теперь стартуют
  с ближайшего к пирамиде силоса; если силоса нет — фолбэк на пусковые пирамиды.
  Подключено в `FireMissile()` (SandRunnersPrototype.cs) и `LaunchGuidedMissile()` (SandRunnersGuidedMissile.cs).
- `UpdateCruiseMissileSilo()` — фабрикация: каждые 26с +1 крылатая (потолок 12);
  каждый 4-й цикл вместо неё +1 сан-кор (потолок 4). Счётчик циклов — `structure.ammo`.

### Аэродром — теперь настоящий (был только хилкой 6hp/3с)
`UpdateAerodrome()` в `SandRunnersRTSCore.cs`:
- Ремонт: 10hp/3с флайерам в 26м, с синим лучом (таймер — `structure.cargoTimer`).
- ПВО-перехват: враг в 85м → залп 2 дрона (`FireInterceptorDrone`, урон 55, взрыв 5м),
  боезапас 2, перезарядка 8с (`structure.ammo` / `structure.reloadTimer` / `structure.fireTimer`).
- Авиация спавнится с площадки аэродрома (`GetSquadSpawnPosition` в StrategicCanvas.cs).
- Авиапроизводство быстрее на +35% за аэродром, максимум учитываются 2 (`UpdateProductionQueue`).

### UI — редизайн «золото + синий, почти диегетический»
- Палитра-константы в `SandRunnersStrategicCanvas.cs`: `UiGold` (1, .76, .23), `UiBlue` (.25, .62, 1),
  `UiGlassDeep/UiGlassPanel` — тёмно-синее стекло. Используй их, не хардкодь новые цвета.
- Панели: `CreatePanel` → тонкая золотая рамка (`AddUiBorder`) + трим (`AddPanelTrim`):
  золотая верхняя кромка и верхние уголки, синие нижние уголки.
- Кнопки: тёмная сталь, золотая рамка, синее подчёркивание; hover — синий, pressed — золото.
- Шкалы HULL (золото → красный при <25%) и APEX (синяя) в статус-панели:
  `CreateUiBar` / `SetUiBarRatio`, обновление в `UpdateStrategicInterface`.
- IMGUI-меню (main/pause/victory/defeat): `DrawPanelRect` теперь синее стекло + двойная рамка
  + золотые уголки. TV-ракета: синий фильтр «Sapphire TV-Link», золотой прицел, рамка видоискателя
  (`DrawGuidedMissileOverlay`).
- Кольцо выделения юнита — синее (было зелёное).

## ЛОВУШКИ — прочти перед работой

1. **НЕ вешай uGUI `Outline` на сплошной `Image`.** Outline дублирует ВЕСЬ прямоугольник графики
   4 раза цветом обводки — под полупрозрачной панелью это заливает её горчичным. Именно поэтому
   старый UI выглядел оливковым. Рамки — только тонкими Image-полосками: `AddUiBorder` / `CreateUiStrip`.
2. **Вступительная кинематографика тратит боезапас**: `SandRunnersCinematicDirector.cs:428-440`
   сама стреляет 2 крылатыми + 1 сан-кором. Если счётчики ракет «не сходятся» — это оно, не баг.
3. **Меню — IMGUI** (`GUI.Button` в OnGUI), из кода нажимается только рефлексией
   `ReloadSceneForNewGame()`. Канвас-кнопки — uGUI, их можно `onClick.Invoke()`.
4. **`battlePyramid` — public**, почти все остальные поля private (рефлексия для тестов).
5. Пробел в пути проекта (`My project`) — MCP-плагин ругается ошибкой в консоли, это шум.

## Как проверять (Unity MCP, сервер http://localhost:26977)

```bash
npx unity-mcp-cli run-tool assets-refresh --input '{}'                    # компиляция
npx unity-mcp-cli run-tool editor-application-set-state --input '{"isPlaying":true}'
npx unity-mcp-cli run-tool console-get-logs --input '{"logTypeFilter":"Exception","lastMinutes":5}'
npx unity-mcp-cli run-tool screenshot-game-view --input '{"width":1600,"height":900}'
```
Новая игра из кода: рефлексией вызвать `ReloadSceneForNewGame` у `SandRunnersPrototype`.

## Что стоит сделать дальше (не начато)

- **TV-ракета в бою руками не облётана** (управление/камера/попадания после переноса старта на силос —
  логика не менялась, но облёт нужен).
- Баланс: 3 баржи сносят поселение за ~9с — возможно, стоит поднять хп поселений (сейчас 520–680).
- Силос/аэродром не строились «честно» через билдера в тесте (создавались напрямую) —
  прогнать полный цикл: кнопка → блюпринт → билдер → постройка.
- У сан-кора нет визуала на рампе силоса; при желании — отдельная тяжёлая направляющая.
- Ресурсные точки не отображаются на «полосе фронта» внизу — можно добавить пиктограммы.

Удачи. Не трогай `GetInstanceID` и не вешай Outline на панели — остальное держится крепко.
