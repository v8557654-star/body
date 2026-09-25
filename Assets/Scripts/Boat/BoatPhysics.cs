using UnityEngine;
using SeaDrift.Ocean;

namespace SeaDrift.Boat
{
    /// <summary>
    /// Реалистичная физика плавучести корабля.
    /// Идея: несколько точек плавучести, каждая проверяет погружение под волну.
    /// Чем глубже точка под водой - тем сильнее выталкивающая сила.
    /// + учитываем нормаль волны для качки.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BoatPhysics : MonoBehaviour
    {
        [Header("Плавучесть")]
        [Tooltip("Точки плавучести - расставь по днищу корабля")]
        public Transform[] buoyancyPoints;
        [Tooltip("Сила плавучести на 1 метр погружения")]
        public float buoyancyForce = 15f;
        [Tooltip("Демпфирование вертикальной скорости (гасит прыжки)")]
        public float waterDrag = 2f;
        [Tooltip("Угловое демпфирование от воды")]
        public float angularWaterDrag = 1.5f;
        [Tooltip("Глубина, после которой сила максимальна")]
        public float maxDepth = 3f;

        [Header("Стабилизация")]
        public float rollStabilization = 2f;
        public float pitchStabilization = 2f;

        [Header("Волны")]
        public float waveInfluence = 1f;
        public float wavePushStrength = 0.5f;

        private Rigidbody rb;
        private OceanManager ocean;

        // Для отладки
        public float[] submersionFactors;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = true;
            rb.angularDamping = 0.5f;
            rb.drag = 0.2f;

            if (buoyancyPoints == null || buoyancyPoints.Length == 0)
            {
                Debug.LogWarning("Нет точек плавучести! Создаю автоматически.");
                CreateDefaultBuoyancyPoints();
            }
            submersionFactors = new float[buoyancyPoints.Length];
        }

        void Start()
        {
            ocean = OceanManager.Instance;
            if (ocean == null) Debug.LogError("OceanManager не найден на сцене!");
        }

        void FixedUpdate()
        {
            if (ocean == null) return;
            if (buoyancyPoints.Length == 0) return;

            Vector3 totalBuoyancyForce = Vector3.zero;
            Vector3 totalTorque = Vector3.zero;
            int submergedCount = 0;

            for (int i = 0; i < buoyancyPoints.Length; i++)
            {
                Transform point = buoyancyPoints[i];
                Vector3 worldPos = point.position;

                // Высота воды в этой точке
                ocean.SampleWaves(worldPos, out Vector3 waterDisp, out Vector3 waterNormal, out Vector3 waterVel);

                float waterHeight = waterDisp.y; // ocean находится в 0 по Y, волны дают смещение
                // Если океан на другой высоте - прибавь ocean.transform.position.y
                waterHeight += ocean.transform.position.y;

                float depth = waterHeight - worldPos.y; // >0 если под водой

                float submersion = Mathf.Clamp01(depth / maxDepth);
                submersionFactors[i] = submersion;

                if (depth > 0f)
                {
                    submergedCount++;

                    // Архимедова сила: F = rho * g * V
                    // Упрощаем: сила пропорциональна погружению
                    float forceMagnitude = buoyancyForce * depth * rb.mass / buoyancyPoints.Length;
                    // Добавляем учет нормали - волна толкает вбок
                    Vector3 buoyancyDir = Vector3.Lerp(Vector3.up, waterNormal, 0.4f * waveInfluence);

                    Vector3 force = buoyancyDir * forceMagnitude;

                    // Применяем в точке
                    rb.AddForceAtPosition(force, worldPos, ForceMode.Force);
                    totalBuoyancyForce += force;

                    // Drag - сопротивление воды
                    Vector3 pointVel = rb.GetPointVelocity(worldPos);
                    Vector3 relativeVel = pointVel - waterVel * waveInfluence;
                    Vector3 drag = -relativeVel * waterDrag * submersion;
                    rb.AddForceAtPosition(drag, worldPos, ForceMode.Force);

                    // Толчок от движения волны
                    Vector3 wavePush = waterVel * wavePushStrength * submersion;
                    rb.AddForce(wavePush, ForceMode.Force);
                }
            }

            // Глобальное демпфирование когда в воде
            float submergedRatio = (float)submergedCount / buoyancyPoints.Length;
            if (submergedRatio > 0.1f)
            {
                rb.AddTorque(-rb.angularVelocity * angularWaterDrag * submergedRatio, ForceMode.Acceleration);

                // Авто-стабилизация крена
                Vector3 localUp = transform.up;
                float roll = Vector3.SignedAngle(localUp, Vector3.up, transform.forward);
                float pitch = Vector3.SignedAngle(localUp, Vector3.up, transform.right);

                rb.AddTorque(transform.forward * (-roll * rollStabilization * submergedRatio * 0.1f), ForceMode.Acceleration);
                rb.AddTorque(transform.right * (pitch * pitchStabilization * submergedRatio * 0.1f), ForceMode.Acceleration);
            }

            // Тонем если полностью под водой долго? Нет, корабль держится
            // Если вылетел из воды - гравитация сама сделает дело
        }

        void CreateDefaultBuoyancyPoints()
        {
            // Создает 6 точек: нос, корма, центр, борта
            buoyancyPoints = new Transform[6];
            GameObject holder = new GameObject("BuoyancyPoints");
            holder.transform.SetParent(transform);
            holder.transform.localPosition = Vector3.zero;

            Vector3[] positions = new Vector3[]
            {
                new Vector3(0, -0.5f, 4f),    // нос
                new Vector3(0, -0.5f, -4f),   // корма
                new Vector3(0, -0.8f, 0f),    // центр
                new Vector3(1.5f, -0.5f, 1f), // правый борт перед
                new Vector3(-1.5f, -0.5f, 1f),// левый борт перед
                new Vector3(0, -0.5f, -1.5f), // центр-корма
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject p = new GameObject($"Buoyancy_{i}");
                p.transform.SetParent(holder.transform);
                p.transform.localPosition = positions[i];
                buoyancyPoints[i] = p.transform;
            }
        }

        void OnDrawGizmos()
        {
            if (buoyancyPoints == null) return;
            foreach (var p in buoyancyPoints)
            {
                if (p == null) continue;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(p.position, 0.25f);
                Gizmos.DrawLine(p.position, p.position + Vector3.down * 1f);
            }
        }
    }
}
