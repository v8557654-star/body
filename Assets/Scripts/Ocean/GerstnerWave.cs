using UnityEngine;

namespace SeaDrift.Ocean
{
    [System.Serializable]
    public struct GerstnerWave
    {
        [Tooltip("Направление волны в XZ")]
        public Vector2 direction;
        [Tooltip("Крутизна волны 0-1")]
        [Range(0f, 1f)]
        public float steepness;
        [Tooltip("Длина волны")]
        public float wavelength;
        [Tooltip("Скорость")]
        public float speed;
        [Tooltip("Амплитуда (высота)")]
        public float amplitude;

        public GerstnerWave(Vector2 dir, float steep, float waveLen, float spd, float amp)
        {
            direction = dir.normalized;
            steepness = steep;
            wavelength = waveLen;
            speed = spd;
            amplitude = amp;
        }

        public static GerstnerWave Default(int index)
        {
            // Пресеты для красивого океана
            switch (index % 8)
            {
                case 0: return new GerstnerWave(new Vector2(1f, 0.3f), 0.12f, 60f, 8f, 1.5f);
                case 1: return new GerstnerWave(new Vector2(0.7f, 0.7f), 0.08f, 35f, 6f, 0.8f);
                case 2: return new GerstnerWave(new Vector2(-0.3f, 1f), 0.05f, 20f, 4f, 0.4f);
                case 3: return new GerstnerWave(new Vector2(0.2f, -1f), 0.04f, 12f, 3f, 0.25f);
                case 4: return new GerstnerWave(new Vector2(-0.8f, -0.4f), 0.06f, 45f, 7f, 0.9f);
                case 5: return new GerstnerWave(new Vector2(0.5f, -0.8f), 0.03f, 8f, 2.5f, 0.15f);
                case 6: return new GerstnerWave(new Vector2(-1f, 0.1f), 0.02f, 6f, 2f, 0.1f);
                default: return new GerstnerWave(new Vector2(0.1f, 1f), 0.1f, 80f, 10f, 2.0f);
            }
        }
    }
}
