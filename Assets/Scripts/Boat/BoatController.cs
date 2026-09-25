using UnityEngine;

namespace SeaDrift.Boat
{
    /// <summary>
    /// Управление кораблем. Простая, но приятная аркадная физика.
    /// W/S - газ/тормоз, A/D - руль, Space - стоп.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoatPhysics))]
    public class BoatController : MonoBehaviour
    {
        [Header("Двигатель")]
        public float enginePower = 8000f;
        public float maxSpeed = 15f;
        [Range(0f, 1f)] public float throttleResponse = 0.15f;

        [Header("Руль")]
        public float rudderPower = 2500f;
        public float rudderResponse = 2f;
        public Transform rudderVisual; // для анимации

        [Header("Эффекты")]
        public ParticleSystem wakeParticles;
        public TrailRenderer[] wakeTrails;
        public AudioSource engineAudio;

        private Rigidbody rb;
        private float throttleInput;
        private float rudderInput;
        private float currentThrottle;
        private float currentRudder;

        // Input
        private float vertical;
        private float horizontal;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            // Ввод
            vertical = Input.GetAxis("Vertical");   // W/S
            horizontal = Input.GetAxis("Horizontal"); // A/D

            // Плавный отклик
            currentThrottle = Mathf.Lerp(currentThrottle, vertical, throttleResponse);
            currentRudder = Mathf.Lerp(currentRudder, horizontal, rudderResponse * Time.deltaTime * 10f);

            // Звук двигателя
            if (engineAudio != null)
            {
                engineAudio.pitch = 0.7f + Mathf.Abs(currentThrottle) * 0.8f;
                engineAudio.volume = 0.3f + Mathf.Abs(currentThrottle) * 0.7f;
            }

            // Визуал руля
            if (rudderVisual != null)
            {
                rudderVisual.localRotation = Quaternion.Euler(0, currentRudder * 35f, 0);
            }

            // Эффекты следа
            float speed = rb.velocity.magnitude;
            if (wakeParticles != null)
            {
                var emission = wakeParticles.emission;
                emission.rateOverTime = speed * 10f + Mathf.Abs(currentThrottle) * 20f;
            }
        }

        void FixedUpdate()
        {
            // Ограничение макс скорости (мягкое)
            float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
            if (Mathf.Abs(forwardSpeed) > maxSpeed)
            {
                float dragFactor = (Mathf.Abs(forwardSpeed) - maxSpeed) / maxSpeed;
                rb.AddForce(-transform.forward * forwardSpeed * dragFactor * 2f, ForceMode.Acceleration);
            }

            // Тяга двигателя - вперед по корпусу
            // Чем быстрее уже едем - тем меньше ускорение (водное сопротивление)
            float speedFactor = 1f - Mathf.Clamp01(Mathf.Abs(forwardSpeed) / maxSpeed);
            Vector3 engineForce = transform.forward * currentThrottle * enginePower * speedFactor;
            rb.AddForce(engineForce, ForceMode.Force);

            // Руль работает только на ходу
            float rudderEffectiveness = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 2f);
            // На заднем ходу руль инвертируется
            float directionSign = Mathf.Sign(forwardSpeed);
            if (Mathf.Abs(currentThrottle) > 0.1f) directionSign = Mathf.Sign(currentThrottle);

            Vector3 rudderTorque = transform.up * currentRudder * rudderPower * rudderEffectiveness * directionSign;
            rb.AddTorque(rudderTorque, ForceMode.Force);

            // Боковое сопротивление (чтобы не скользил боком как на льду)
            Vector3 lateralVel = transform.right * Vector3.Dot(rb.velocity, transform.right);
            rb.AddForce(-lateralVel * 2f, ForceMode.Acceleration);

            // Экстренный тормоз пробелом
            if (Input.GetKey(KeyCode.Space))
            {
                rb.AddForce(-rb.velocity * 3f, ForceMode.Acceleration);
                rb.AddTorque(-rb.angularVelocity * 3f, ForceMode.Acceleration);
            }
        }

        void OnGUI()
        {
            // Простой спидометр
            float kmh = rb.velocity.magnitude * 3.6f;
            GUI.Label(new Rect(20, 20, 200, 25), $"Скорость: {kmh:F1} км/ч");
            GUI.Label(new Rect(20, 45, 300, 25), $"Газ: {currentThrottle:F2} | Руль: {currentRudder:F2}");
            GUI.Label(new Rect(20, 70, 400, 25), "W/S - газ, A/D - руль, SPACE - тормоз, Мышь - камера");
        }
    }
}
