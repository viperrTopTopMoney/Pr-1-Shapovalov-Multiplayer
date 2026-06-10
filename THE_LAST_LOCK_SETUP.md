# THE LAST LOCK: что уже настроено

Основная рабочая сцена: `Assets/Scenes/Main.unity`.

В сцене автоматически создан корень `THE_LAST_LOCK_GAMEPLAY`:

- `01_ENVIRONMENT_FLOODED_GROUNDS` содержит `Cabin1` и поверхность для NavMesh.
- `02_GAMEPLAY/Barricades_NETWORKED` содержит сетевую дверь и три сетевых окна.
- `HouseIntrusionZone_TRIGGER` включает десятисекундное условие поражения.
- `Markers` содержит две точки игроков, восемь точек зомби и цель внутри дома.
- `NavMeshSurface` уже запечён.

Также созданы временные сетевые префабы зомби:

- `Assets/Generated/Prefabs/Zombie_Normal_PLACEHOLDER.prefab`
- `Assets/Generated/Prefabs/Zombie_Runner_PLACEHOLDER.prefab`
- `Assets/Generated/Prefabs/Zombie_Boss_PLACEHOLDER.prefab`

Они позволяют тестировать все пять волн до импорта финальных моделей зомби.

## Быстрый тест

1. Открой `Main`.
2. Нажми Play.
3. Запусти Host.
4. Запусти второй экземпляр через Multiplayer Play Mode и подключи Client.
5. У Host появится кнопка `START GAME` в левом HUD.
6. Управление: WASD, Shift, Space, удержание E.

## Что нужно визуально поправить вручную

1. Открой `THE_LAST_LOCK_GAMEPLAY`.
2. Подвинь четыре объекта из `Barricades_NETWORKED` точно к двери и окнам Cabin1.
3. При необходимости измени размеры `HouseIntrusionZone_TRIGGER`, чтобы триггер покрывал только внутреннюю часть дома.
4. Расставь деревья и декор из Flooded Grounds вокруг готовой компактной игровой зоны.

Не удаляй компоненты `NetworkObject`, `DoorController`, `WindowController`,
`ZombieNetwork`, `ZombieView` и `NavMeshAgent`.

## Анимации

Survivalist содержит готовые Idle, Walk и Run. Они подключены к
`Assets/Generated/Animations/Player.controller`.

В Survivalist нет отдельных Death, Downed и Revive клипов. Контроллер уже
содержит нужные состояния и параметры, но временно использует Idle как
запасной клип. После импорта подходящих клипов достаточно заменить Motion
в состояниях `Downed` и `Death`; сетевой код уже переключает их.

## Замена временного зомби

Открой нужный `Zombie_*_PLACEHOLDER.prefab` и замени только дочерний объект
`Visual_REPLACE_WITH_ZOMBIE_ASSET` на модель зомби. Корневые сетевые и AI
компоненты оставь без изменений. Затем назначь Animator в `ZombieView`.
