# SandRunners — checkpoint супер-прохода, 2026-07-14

## Статус

Супер-проход завершён. Ядро реализовано и компилируется, EditMode-тесты проходят, живой F5-сценарий и стресс проверены, кластеризация подтверждена визуально, development build создан и запущен smoke-тестом. Unity оставлен вне Play Mode.

## Реализовано

- `SalvageDebrisNode` заменены на агрегированные `SalvageField` с единым payload, состоянием назначения, переносчиком и набором визуальных частей.
- Добавлены лимиты: 24 поля, 72 части, 6 маркеров, 4 ближайших salvage-огня, обновление 10 Гц.
- При достижении лимита новый payload объединяется с ближайшим полем без потери ресурсов.
- Добавлен пул визуальных фрагментов и culling дальних деталей.
- Damage presentation переведён с `renderer.material` на `MaterialPropertyBlock`.
- Исправлена Unity 6 runtime-ошибка: `MaterialPropertyBlock` теперь создаётся лениво, а не в инициализаторе `MonoBehaviour`.
- Цена Salvage Scarab: 55 песка / 65 золота / 4 ветра.
- Payload: техника 8/5/0, баржа 16/10/2, постройка 14/8/0, крепость 18/18/4.
- Доставка защищена от повторной выдачи. Несколько скарабеев не могут одновременно занять одно поле.
- Скарабей несёт одно поле, поддерживает ручной приказ, авто-сбор, возврат, потерю/занятие цели, время погрузки и статус груза.
- Legacy salvage OnGUI отключён; управление перенесено в `Strategic Canvas`.
- Добавлены Canvas-панель выбранного скарабея, payload/дистанция/погрузка, кнопки nearest/return/auto.
- Маркеры ограничены, затухают, получают off-screen стрелки и spatial clustering с `xN`.
- Status HUD уменьшен до 390x112, mission HUD до 390x106, стартовая подсказка скрывается через 12 секунд.
- Командная панель остаётся только в стратегическом режиме.
- Палитра переведена в тёмный металл, матовое золото, янтарь и нейтральный песок.
- Сгенерирован оригинальный atlas и разрезан Unity на четыре игровые текстуры: neutral sand, dark metal, scorch/cracks, salvage glyphs.
- Текстуры подключены к песку, salvage-металлу, scorch FX и salvage-глифам.
- Salvage-звуки используют существующие каналы: heavy impact, harvest start, resource delivery и UI confirm.

## Изменённые основные файлы

- `Assets/Scripts/SandRunners/SandRunnersDamagePresentation.cs`
- `Assets/Scripts/SandRunners/SandRunnersStrategicCanvas.cs`
- `Assets/Scripts/SandRunners/SandRunnersReleaseCandidate.cs`
- `Assets/Scripts/SandRunners/SandRunnersPrototype.cs`
- `Assets/Scripts/SandRunners/SandRunnersDuneWorld.cs`
- `Assets/Scripts/SandRunners/SandRunnersBattleEffects.cs`
- `Assets/Scripts/SandRunners/SandRunnersVerticalSliceMission.cs`
- `Assets/Scripts/SandRunners/SandRunnersAssemblyInfo.cs`
- `Assets/Tests/EditMode/SandRunnersSalvageTests.cs`
- `Assets/Tests/EditMode/SandRunners.EditModeTests.asmdef`
- `Assets/SR_SurfaceKit_Atlas.png`
- `Assets/Resources/SandRunners/Textures/`

## Проверено

- Чистая C#-компиляция затронутых файлов; новых compile errors не было.
- EditMode: 5/5 passed.
  - точная цена скарабея;
  - точные payload четырёх классов;
  - сохранение payload при merge и ограничения частей;
  - выдача payload ровно один раз;
  - защита от второго скарабея на занятой цели.
- F5: скарабей создан, выбран, получил ближайшую цель, панель и маркеры отображались, runtime exceptions после исправления MPB отсутствовали.
- Стресс: подтверждено `fields=24 pieces=72`.
- Однокадровые замеры Game View:
  - обычный F5-кадр: 83.15 FPS;
  - другой тяжёлый кадр: 43.39 FPS;
  - стресс 24/72: 63.63 FPS.
- Эти замеры не являются полноценной метрикой среднего FPS и 1% low Windows Player.

## Финальная проверка после паузы

1. Development build найден полностью собранным: `Builds/SandRunners_Development/SandRunners.exe` и каталог `SandRunners_Data` присутствуют.
2. Player запущен в скрытом headless-режиме и стабильно работал 10 секунд до контролируемой остановки.
3. Живой cluster stress создал 8 групп по 3 поля; UI показал маркеры `SALVAGE x3`.
4. Точный runtime audit: `fields=24 pieces=72 markers=6 lights=4`.
5. Stress Game View: 66.68 FPS в проверочном кадре, исключений не было.
6. Финальный EditMode: 5/5 passed.
7. За последнюю минуту финальной проверки: 0 errors, 0 exceptions, 0 warnings.
8. Editor оставлен с `IsPlaying=false`, `IsCompiling=false`, `IsUpdating=false`.

## Итог

Запланированный проход реализован и передан как проверочный development build. Перед релизной упаковкой всё ещё разумно провести длительный Windows Player capture для достоверных average FPS и 1% low на целевой машине; однокадровые Editor-замеры не заменяют такой capture.
