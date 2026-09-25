using UnityEngine;

namespace SeaDrift.CameraSystem
{
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 20f;
        public float height = 8f;
        public float smoothSpeed = 5f;
        public float rotationSmooth = 3f;
        public Vector2 pitchLimits = new Vector2(-10f, 60f);

        private float yaw;
        private float pitch = 15f;
        private Vector3 velocity;

        void Start()
        {
            if (target == null)
            {
                var boat = FindObjectOfType<Boat.BoatController>();
                if (boat != null) target = boat.transform;
            }
            yaw = transform.eulerAngles.y;
            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
            }
        }

        void LateUpdate()
        {
            if (target == null) return;

            // Мышь
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                yaw += Input.GetAxis("Mouse X") * 3f;
                pitch -= Input.GetAxis("Mouse Y") * 3f;
                pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
            }

            // Зум колесом
            distance -= Input.GetAxis("Mouse ScrollWheel") * 5f;
            distance = Mathf.Clamp(distance, 5f, 50f);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 targetPos = target.position + Vector3.up * 1.5f;
            Vector3 desiredPos = targetPos - rotation * Vector3.forward * distance + Vector3.up * height * 0.3f;

            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, 1f / smoothSpeed);
            Quaternion lookRot = Quaternion.LookRotation(targetPos - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotationSmooth * 3f);

            // Легкая тряска от волн
            float waveShake = Mathf.Sin(Time.time * 1.2f) * 0.1f;
            transform.position += transform.right * waveShake;
        }
    }
}
