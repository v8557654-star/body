using UnityEngine;

namespace SeaDrift.Boat
{
    public class BoatWake : MonoBehaviour
    {
        public ParticleSystem wakeParticles;
        public TrailRenderer leftTrail;
        public TrailRenderer rightTrail;
        public float minSpeedForWake = 2f;
        public float wakeIntensityMultiplier = 1f;

        private Rigidbody rb;
        private BoatController controller;

        void Awake()
        {
            rb = GetComponentInParent<Rigidbody>();
            controller = GetComponentInParent<BoatController>();
        }

        void Update()
        {
            float speed = rb.velocity.magnitude;
            float intensity = Mathf.InverseLerp(minSpeedForWake, controller != null ? controller.maxSpeed : 10f, speed);

            if (wakeParticles != null)
            {
                var emission = wakeParticles.emission;
                emission.rateOverTime = intensity * 50f * wakeIntensityMultiplier;

                var main = wakeParticles.main;
                main.startSpeed = speed * 0.5f;
            }

            if (leftTrail != null) leftTrail.emitting = intensity > 0.2f;
            if (rightTrail != null) rightTrail.emitting = intensity > 0.2f;
        }
    }
}
