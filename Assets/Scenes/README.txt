Сцена Main.unity создается в Unity:

1. Создай новую сцену Main
2. Hierarchy:
   - OceanManager (Empty) -> OceanManager.cs + InfiniteOcean.cs
     - Назначь материал с шейдером SeaDrift/Ocean
   - Directional Light (Sun)
   - Boat (Cube 3x1.2x9) -> Rigidbody + BoatPhysics + BoatController
     - Вложи 6 пустых точек как в BoatPhysics.cs
     - Добавь визуал (модель лодки или примитивы)
     - Tag: Player
   - Main Camera -> FollowCamera.cs (target = Boat)
   - GameManager (Empty) -> GameManager.cs
   - Buoys (Empty) -> внутри 5-8 сфер с SphereCollider(isTrigger) + Buoy.cs
   - Skybox + Fog

3. Project Settings:
   - Physics: Default Solver Iterations 10
   - Time: Fixed Timestep 0.02

4. Materials:
   - Ocean Material: Shader SeaDrift/Ocean
     - Deep Color: (0.02,0.15,0.3)
     - Shallow Color: (0.05,0.5,0.6)
     - Smoothness 0.9

Готово! Жми Play.
