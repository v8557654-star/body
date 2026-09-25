# SEA DRIFT - 3D Игра про плавание корабля

Простая, но красивая игра про плавание корабля по морю с реалистичными волнами и физикой. Сделано на Unity (C#) + есть веб-демо на Three.js.

## 🎮 Геймплей
- Корабль плавает по бесконечному океану
- Реалистичные волны Герстнера (как в Sea of Thieves)
- Физика плавучести по точкам (buoyancy points)
- Управление: WASD, мышь для камеры, пробел - тормоз
- Цель: собирать буи, выживать в штормах
- Динамическая погода: штиль -> шторм

## 🌊 Физика - как это работает?

### 1. Волны Герстнера
Формула волны:
```
x = x0 - dir.x * A * sin(f) * steepness
y = A * cos(f)
z = z0 - dir.y * A * sin(f) * steepness
f = k*(dot(dir, pos) - c*t)
```
Где:
- `k = 2π/λ` - волновое число
- `c = sqrt(g/k)` - фазовая скорость (дисперсия)
- `A` - амплитуда
- Используем 8 волн с разными направлениями/длинами для реалистичного океана

### 2. Плавучесть
Для каждой точки днища:
1. Спрашиваем у OceanManager высоту воды в точке
2. Считаем погружение: `depth = waterHeight - point.y`
3. Сила Архимеда: `F = buoyancyForce * depth * mass`
4. Применяем `AddForceAtPosition` - это дает крен и дифферент автоматически!
5. Добавляем drag от воды и толчок от скорости частиц воды

### 3. Управление
- Тяга двигателя: `F = forward * throttle * power * (1 - speed/maxSpeed)`
- Руль: `Torque = up * rudder * power * effectiveness(speed)`
- Боковой драг чтобы не скользил
- Стабилизация крена когда в воде

## 📁 Структура Unity проекта

```
Assets/
├── Scripts/
│   ├── Ocean/
│   │   ├── GerstnerWave.cs - структура волны
│   │   ├── OceanManager.cs - главный менеджер, считает волны
│   │   └── InfiniteOcean.cs - бесконечный океан из тайлов
│   ├── Boat/
│   │   ├── BoatPhysics.cs - физика плавучести
│   │   └── BoatController.cs - управление
│   ├── Camera/
│   │   └── FollowCamera.cs - камера за кораблем
│   └── UI/
│       └── GameManager.cs - буи, шторм, победа
├── Materials/
│   └── OceanShader.shader - шейдер океана с Gerstner в вершинах
├── Prefabs/
│   └── Boat.prefab - (создай из примитивов)
└── Scenes/
    └── Main.unity
```

## 🚀 Как запустить в Unity

1. Создай новый 3D проект Unity 2022.3 LTS
2. Скопируй папку Assets из этого репо
3. Создай сцену:
   - Пустой GameObject -> OceanManager (повесь OceanManager.cs)
   - Плоскость или InfiniteOcean для воды
   - Корабль: 
     - Создай Cube 4x1x8, добавь Rigidbody (mass 1000, drag 0.2, angularDrag 0.5)
     - Добавь BoatPhysics и BoatController
     - Создай 6 пустых объектов как точки плавучести по днищу (см. BoatPhysics.cs)
     - Добавь Mesh для красоты (можно из примитивов сделать)
   - Main Camera -> FollowCamera, таргет = корабль
   - Directional Light + Skybox
   - Несколько сфер с триггером и скриптом Buoy (буи)
   - GameManager
4. Материал океана: шейдер SeaDrift/Ocean, цвета Deep/Shallow
5. Play!

### Настройки Rigidbody корабля:
- Mass: 1000-2000
- Drag: 0.2
- Angular Drag: 0.8
- Interpolate: Interpolate
- Collision Detection: Continuous Dynamic

### Настройки OceanManager:
- 8 волн по дефолту уже красивые
- waveHeightMultiplier: 1 = штиль, 2.5 = шторм
- timeScale: 1

## 🎯 Идеи для развития

- [ ] Добавить острова (генерация через шум)
- [ ] Рыбалка
- [ ] Враги - акулы, пираты
- [ ] Торговля между портами
- [ ] Прокачка корабля
- [ ] Смена дня/ночи
- [ ] Подводные рифы и мели
- [ ] Multiplayer

## 🌐 Веб-демо

В папке `web-demo` - играбельный прототип на Three.js с той же физикой!
Запуск:
```bash
cd web-demo
npm install
npm run dev
```

Открывается на http://localhost:5173

Управление в веб-демо:
- W/S - вперед/назад
- A/D - руль
- Мышь - камера (зажми правую кнопку)
- Колесо - зум
- R - рестарт

## 📜 Лицензия
MIT - делай что хочешь!

Сделано с ❤️ для любителей моря.
