# Howitzer Gunner & TV-Guided Missile — Implementation Report

## 1. Howitzer Gunner (`SandRunnersHowitzerGunner.cs`)

### Activation
- **Q** — переключиться на портовые (левые) гаубицы (циклически: орудие 1 → 2 → выход)
- **E** — переключиться на старбордные (правые) гаубицы (циклически: орудие 1 → 2 → выход)
- Выход из режима наводчика: **Escape**

### Все поля (private, в `SandRunnersPrototype`)
```csharp
private int gunnerSide;                    // -1 = port, 1 = starboard, 0 = off
private int gunnerBarrelIndex;            // 0 = первое орудие, 1 = второе
private float gunnerYaw;                  // горизонтальный угол наведения
private float gunnerPitch;                // вертикальный угол наведения
private Transform gunnerActiveMuzzle;     // Transform дула активного орудия
private Transform gunnerBarrelRoot;       // корневой Transform ствола (родитель дула)
```

### Ссылки на GameObjects в `Codex_Pyramid` / `Three_Hundred_Millimeter_Side_Howitzers`
- **Port**: `Howitzer_Port_1`, `Howitzer_Port_2` (каждый содержит `Muzzle` и `BarrelRoot`)
- **Starboard**: `Howitzer_Starboard_1`, `Howitzer_Starboard_2`
- Поиск через `FindChildRecursive(battlePyramid, name)`

### Управление (в `UpdateHowitzerGunner(dt)`)
- **Мышь X** → `gunnerYaw` (чувствительность `gunnerMouseSensitivity = 0.12`)
- **Мышь Y** → `gunnerPitch` (не инвертирован: `+= mouseDelta.y`, диапазон -30° до +45°)
- **Левая кнопка мыши** → стрельба (с кулдауном 1.1с)
- **Escape** → выход
- **Q/E** (только при входе) → переключение орудия/стороны

### Визуал и камера
- **Камера**: FOV 50°, смещение `OrbitCamera(6.5m назад, 0.8m вверх, 0.5m вбок)` относительно `gunnerBarrelRoot`
- **Поворот ствола**: для port — `Quaternion.Euler(0, yaw, pitch)`; для starboard — `Quaternion.Euler(0, -yaw, -pitch)`
- **Перекрестие**: зелёный круг (сетка 8 точек + 2 линии), HUD с углами и статусом
- **Стрельба**:
  1. Найти `Muzzle` в активном орудии
  2. `AimDir = (muzzle.position - barrelRoot.position).normalized`
  3. Получить `MuzzleTip` (смещение muzzle.position вперёд по aimDir)
  4. Raycast на 1000м по aimDir
  5. Если хит — `CreateWeaponTracer` + выстрел от первого лица (`CreateGunnerShot`)
  6. Если хит по врагу — нанести урон (200)
  7. `PlaySandRunnerSound(ArtilleryFire, tip, 1.2)`

### Известные проблемы
1. **Камера уходит под текстуры пирамиды** — смещение `OrbitCamera` может помещать камеру внутрь геометрии пирамиды. Нужна проверка коллизий (camera collision) или динамическая коррекция позиции камеры.
2. **Ствол не привязан физически к дулу** — хотя `BarrelRoot` поворачивается, визуально ствол может не совпадать с трассером. Возможно, неправильный `BarrelRoot` или отсутствие промежуточной иерархии.
3. **Raycast не учитывает рельеф** — при стрельбе вниз луч уходит под землю. Нужен `Physics.Raycast` в цель.

---

## 2. TV-Guided Missile (`SandRunnersGuidedMissile.cs`)

### Activation
- **V** — запуск TV-управляемой крылатой ракеты (расходует cruise missile)

### Все поля (private, в `SandRunnersPrototype`)
```csharp
private bool guidedMissileActive;
private Transform guidedMissileTransform;
private Vector3 guidedMissileVelocity;
private float guidedMissileLife;
private float guidedYaw;
private float guidedPitch;
private enum GuidedPhase { Eject, Boost, Cruise }
private GuidedPhase guidedPhase;
private float guidedPhaseTimer;
private Vector3 guidedLaunchPos;
```

### Фазы полёта
1. **Eject** (0.7с)
   - Ракета выбрасывается вверх (22 м/с) из выбранного `missileLauncher`
   - Камера остаётся у пирамиды, смотрит вверх на ракету
   - Управления нет
   
2. **Boost** (1.1с)
   - Двигатель зажигается
   - Плавный переход от подъёма к горизонтальному полёту (скорость 22 → 42 м/с)
   - Камера начинает следовать за ракетой (нос ракеты)
   - Управления нет

3. **Cruise** (до 7с)
   - Мышь управляет направлением ракеты через delta (`GuidedMouseSensitivity = 0.15`)
   - guidedYaw += delta.x
   - guidedPitch = Clamp(guidedPitch + delta.y, -85°, +85°)
   - `Quaternion.Euler(-pitch, yaw, 0) * Vector3.forward` → target direction
   - `guidedMissileVelocity = Lerp(velocity, targetDir * speed, TurnRate * dt)`

### Камера (Cruise)
- Позиция: нос ракеты + 1.8м вперёд по скорости, lerp за камерой (dt * 10f)
- Вращение: `LookRotation(velocity.normalized, Vector3.up)`, lerp (dt * 8f)
- ИК-оверлей: весь экран, тёмно-зелёный `Color(0.06, 0.15, 0.06, 0.7)`, поверх GUI

### Поражение
- Попадание: дистанция до врага ≤ 5м
- Самонаведение: перебор `enemies` в цикле
- Взрыв: `CreateExplosion(pos, 14f, 145f, false)` — радиус 14, урон 145
- Таймер: `guidedMissileLife -= dt;` (макс 7с)
- Выход по Escape уничтожает ракету

### Известные проблемы
1. **Нет возврата камеры после взрыва** — после окончания guided режима камера остаётся в последней позиции nosePos, `FollowCamera` потом плавно возвращает к пирамиде, но может быть резкий прыжок.
2. **Hit detection примитивный** — проверка расстояния до врагов. Должен быть raycast или sphere cast по пути полёта.
3. **Нет обратной связи по наведению** — не видно куда именно прицел направлен (нужна линия прицеливания).
4. **Курсор** — во время guided режима курсор остаётся locked (как при обычной игре), но игрок не видит курсор. Нужно или показать, или оставить locked (сейчас locked — нормально для delta-управления).

---

## 3. Интеграция в `SandRunnersPrototype.cs`

- `UpdateGuidedMissile(dt)` вызывается после `UpdateMissiles(dt)` (строка 312)
- `DrawGuidedMissileOverlay()` вызывается в `OnGUI()` после `DrawGunnerCrosshair()`
- `LaunchGuidedMissile()` вызывается по **V** в `HandleWeaponInput`
- При `SetGameFlowState(!playing)` — `ExitGuidedMissile()` если active
- В guided mode блокируются `HandleCameraModeInput()` и `HandlePyramidMovement()`
