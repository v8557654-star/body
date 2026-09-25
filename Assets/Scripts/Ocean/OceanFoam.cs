using UnityEngine;

namespace SeaDrift.Ocean
{
    // Добавляет пену на гребнях волн через вершинные цвета или частицы
    public class OceanFoam : MonoBehaviour
    {
        public OceanManager ocean;
        public ParticleSystem foamParticles;
        public float foamThreshold = 1.2f;
        public float checkInterval = 0.2f;

        private float timer;

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0) return;
            timer = checkInterval;

            if (ocean == null) ocean = OceanManager.Instance;
            if (ocean == null) return;

            float height = ocean.GetWaveHeightAt(transform.position);
            if (height > foamThreshold && foamParticles != null)
            {
                foamParticles.Emit(1);
            }
        }
    }
}
