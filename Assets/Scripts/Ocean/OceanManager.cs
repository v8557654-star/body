using UnityEngine;

namespace SeaDrift.Ocean
{
    /// <summary>
    /// Главный менеджер океана. Считает волны Герстнера.
    /// Это сердце физики - все корабли спрашивают у него высоту волны.
    /// </summary>
    public class OceanManager : MonoBehaviour
    {
        public static OceanManager Instance { get; private set; }

        [Header("Волны")]
        public GerstnerWave[] waves = new GerstnerWave[8];
        [Tooltip("Общий множитель высоты")]
        [Range(0f, 3f)] public float waveHeightMultiplier = 1f;
        [Tooltip("Скорость времени волн")]
        public float timeScale = 1f;

        [Header("Оптимизация")]
        public bool useJobs = true;

        private float _time;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (waves == null || waves.Length == 0)
            {
                waves = new GerstnerWave[8];
                for (int i = 0; i < 8; i++) waves[i] = GerstnerWave.Default(i);
            }
        }

        void Update()
        {
            _time += Time.deltaTime * timeScale;
        }

        /// <summary>
        /// Получить высоту, нормаль и смещение воды в точке мира
        /// </summary>
        public float GetWaveHeightAt(Vector3 worldPos)
        {
            Vector3 displacement = Vector3.zero;
            SampleWaves(worldPos, out displacement, out _, out _);
            return displacement.y;
        }

        public bool SampleWaves(Vector3 worldPos, out Vector3 displacement, out Vector3 normal, out Vector3 velocity)
        {
            displacement = Vector3.zero;
            normal = Vector3.up;
            velocity = Vector3.zero;

            Vector3 tangent = Vector3.right;
            Vector3 binormal = Vector3.forward;

            float time = _time;

            foreach (var w in waves)
            {
                if (w.wavelength <= 0.01f) continue;

                float k = 2f * Mathf.PI / w.wavelength; // волновое число
                float c = Mathf.Sqrt(9.8f / k); // фазовая скорость по дисперсии
                float a = w.amplitude * waveHeightMultiplier;
                float steep = w.steepness;

                Vector2 dir = w.direction.normalized;
                float f = k * (Vector2.Dot(dir, new Vector2(worldPos.x, worldPos.z)) - c * time * w.speed * 0.1f);
                float sinF = Mathf.Sin(f);
                float cosF = Mathf.Cos(f);

                // Gerstner displacement
                // x = x0 - dir.x * a * sin(f) * steep
                // y = a * cos(f)
                // z = z0 - dir.y * a * sin(f) * steep

                displacement.x += -dir.x * a * sinF * steep;
                displacement.y += a * cosF;
                displacement.z += -dir.y * a * sinF * steep;

                // Скорость частицы воды (для drag)
                float wa = k * c * w.speed * 0.1f;
                velocity.x += dir.x * a * wa * cosF * steep;
                velocity.y += -a * wa * sinF;
                velocity.z += dir.y * a * wa * cosF * steep;

                // Нормаль (производные)
                float ddx = -k * dir.x * dir.x * steep * a * cosF;
                float ddy = -k * dir.x * a * sinF;
                float ddz = -k * dir.x * dir.y * steep * a * cosF;

                float ddx2 = -k * dir.y * dir.x * steep * a * cosF;
                float ddy2 = -k * dir.y * a * sinF;
                float ddz2 = -k * dir.y * dir.y * steep * a * cosF;

                // Упрощенная нормаль через кросс
                Vector3 n = new Vector3(-ddy, 1f - ddx - ddz2, -ddy2);
                // Накапливаем (усреднение не точное, но визуально ок)
                tangent.x += ddx;
                tangent.y += ddy;
                tangent.z += ddz;

                binormal.x += ddx2;
                binormal.y += ddy2;
                binormal.z += ddz2;
            }

            normal = Vector3.Cross(binormal, tangent).normalized;
            if (normal.y < 0) normal = -normal;

            return true;
        }

        public Vector3 GetWaterNormalAt(Vector3 worldPos)
        {
            SampleWaves(worldPos, out _, out Vector3 normal, out _);
            return normal;
        }

        public Vector3 GetWaterVelocityAt(Vector3 worldPos)
        {
            SampleWaves(worldPos, out _, out _, out Vector3 vel);
            return vel;
        }

        // Для шейдера - отдать параметры
        public Vector4[] GetWaveDataForShader()
        {
            Vector4[] data = new Vector4[waves.Length * 2];
            for (int i = 0; i < waves.Length; i++)
            {
                var w = waves[i];
                float k = 2f * Mathf.PI / w.wavelength;
                float c = Mathf.Sqrt(9.8f / k);
                data[i * 2] = new Vector4(w.direction.x, w.direction.y, w.steepness, w.wavelength);
                data[i * 2 + 1] = new Vector4(w.amplitude * waveHeightMultiplier, w.speed, k, c);
            }
            return data;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            // Визуализация волн
            Gizmos.color = Color.cyan;
            for (int x = -50; x <= 50; x += 10)
            {
                for (int z = -50; z <= 50; z += 10)
                {
                    Vector3 pos = transform.position + new Vector3(x, 0, z);
                    float h = GetWaveHeightAt(pos);
                    Gizmos.DrawSphere(pos + Vector3.up * h, 0.3f);
                }
            }
        }
#endif
    }
}
