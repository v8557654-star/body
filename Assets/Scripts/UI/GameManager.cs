using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeaDrift
{
    public class GameManager : MonoBehaviour
    {
        [Header("Геймплей")]
        public int buoysToCollect = 5;
        private int collected = 0;
        public float gameTime = 0f;
        public bool isStorm = false;

        [Header("Шторм")]
        public float stormChance = 0.1f;
        public float stormDuration = 30f;
        private float stormTimer = 0f;

        public static GameManager Instance;

        void Awake() { Instance = this; }

        void Update()
        {
            gameTime += Time.deltaTime;

            // Случайный шторм
            if (!isStorm && Random.value < stormChance * Time.deltaTime)
            {
                StartStorm();
            }
            if (isStorm)
            {
                stormTimer -= Time.deltaTime;
                if (stormTimer <= 0) EndStorm();
            }

            if (Input.GetKeyDown(KeyCode.R)) Restart();
        }

        public void CollectBuoy()
        {
            collected++;
            Debug.Log($"Буй собран! {collected}/{buoysToCollect}");
            if (collected >= buoysToCollect)
            {
                Debug.Log("ПОБЕДА! Все буи собраны!");
            }
        }

        void StartStorm()
        {
            isStorm = true;
            stormTimer = stormDuration;
            var ocean = Ocean.OceanManager.Instance;
            if (ocean != null)
            {
                ocean.waveHeightMultiplier = 2.5f;
                ocean.timeScale = 1.8f;
            }
            RenderSettings.fogDensity = 0.02f;
            Debug.Log("ШТОРМ НАЧАЛСЯ!");
        }

        void EndStorm()
        {
            isStorm = false;
            var ocean = Ocean.OceanManager.Instance;
            if (ocean != null)
            {
                ocean.waveHeightMultiplier = 1f;
                ocean.timeScale = 1f;
            }
            RenderSettings.fogDensity = 0.005f;
            Debug.Log("Шторм закончился");
        }

        void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        void OnGUI()
        {
            GUI.Box(new Rect(Screen.width - 210, 10, 200, 90), "SEA DRIFT");
            GUI.Label(new Rect(Screen.width - 200, 35, 180, 20), $"Буи: {collected}/{buoysToCollect}");
            GUI.Label(new Rect(Screen.width - 200, 55, 180, 20), $"Время: {gameTime:F1}с");
            if (isStorm) GUI.Label(new Rect(Screen.width - 200, 75, 180, 20), "!!! ШТОРМ !!!");
        }
    }

    public class Buoy : MonoBehaviour
    {
        public float bobSpeed = 1f;
        public float bobHeight = 0.3f;
        private Vector3 startPos;
        private bool collected = false;

        void Start() { startPos = transform.position; }

        void Update()
        {
            if (collected) return;
            // Покачивание + следование волне
            float waveY = 0f;
            if (Ocean.OceanManager.Instance != null)
                waveY = Ocean.OceanManager.Instance.GetWaveHeightAt(transform.position);

            transform.position = new Vector3(startPos.x, waveY + Mathf.Sin(Time.time * bobSpeed) * bobHeight, startPos.z);
            transform.Rotate(Vector3.up, 20f * Time.deltaTime);
        }

        void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            if (other.GetComponent<Boat.BoatController>() != null)
            {
                collected = true;
                GameManager.Instance?.CollectBuoy();
                // Эффект
                Destroy(gameObject, 0.1f);
            }
        }
    }
}
