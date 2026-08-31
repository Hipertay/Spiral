using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RingGame : MonoBehaviour
{
    [Header("References")]
    public GameObject arrow;
    public GameObject zonePrefab;
    public Transform zonesParent;
    public TextMeshProUGUI scoreText;

    [Header("Settings")]
    public float rotationSpeed = 100f;
    public float freezeDuration = 1.5f;
    public int initialZoneCount = 2;
    public int maxZoneCount = 5;
    public float spawnInterval = 2f;
    public float minZoneWidth = 10f;
    public float maxZoneWidth = 30f;
    public float arrowWidth = 5f; // <-- новая настройка

    private List<ZoneData> zones = new List<ZoneData>();
    private int score = 0;
    private float freezeTimer = 0f;
    private bool isFrozen = false;

    [System.Serializable]
    public class ZoneData
    {
        public float startAngle;
        public float width;
        public GameObject gameObject;
        public Material material;

        public ZoneData(float start, float w, GameObject obj, Material mat)
        {
            startAngle = start;
            width = w;
            gameObject = obj;
            material = mat;
        }

        // Получить интервалы в [0,360) для этой зоны
        public List<(float, float)> GetIntervals()
        {
            List<(float, float)> intervals = new List<(float, float)>();
            float start = NormalizeAngle(startAngle);
            float end = start + width;
            if (end > 360f)
            {
                intervals.Add((start, 360f));
                intervals.Add((0f, end - 360f));
            }
            else
            {
                intervals.Add((start, end));
            }
            return intervals;
        }

        private float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle < 0f) angle += 360f;
            return angle;
        }
    }

    void Start()
    {
        for (int i = 0; i < initialZoneCount; i++)
            GenerateZone();
        StartCoroutine(SpawnRoutine());
        UpdateScoreUI();
    }

    void Update()
    {
        if (isFrozen)
        {
            freezeTimer -= Time.deltaTime;
            if (freezeTimer <= 0f)
            {
                isFrozen = false;
                freezeTimer = 0f;
            }
        }
        else
        {
            arrow.transform.Rotate(Vector3.forward, -rotationSpeed * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            OnPressButton();
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (zones.Count < maxZoneCount)
                GenerateZone();
        }
    }

    public void OnPressButton()
    {
        if (isFrozen) return;

        float arrowCenter = NormalizeAngle(arrow.transform.eulerAngles.z);
        float halfWidth = arrowWidth / 2f;
        float arrowStart = arrowCenter - halfWidth;
        // Создаём временную зону для стрелки
        ZoneData arrowZone = new ZoneData(arrowStart, arrowWidth, null, null);

        ZoneData hitZone = null;
        foreach (var zone in zones)
        {
            if (ZonesOverlap(zone, arrowZone))
            {
                hitZone = zone;
                break;
            }
        }

        if (hitZone != null)
        {
            zones.Remove(hitZone);
            Destroy(hitZone.gameObject);
            score++;
            UpdateScoreUI();
        }
        else
        {
            isFrozen = true;
            freezeTimer = freezeDuration;
        }
    }

    private void GenerateZone()
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            float width = Random.Range(minZoneWidth, maxZoneWidth);
            float start = Random.Range(0f, 360f);

            ZoneData testZone = new ZoneData(start, width, null, null);

            bool overlaps = false;
            foreach (var existing in zones)
            {
                if (ZonesOverlap(existing, testZone))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                GameObject obj = Instantiate(zonePrefab, zonesParent);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.rotation = Quaternion.Euler(0f, 0f, start);

                Image image = obj.GetComponent<Image>();
                if (image == null)
                {
                    Debug.LogError("Zone prefab must have an Image component!");
                    Destroy(obj);
                    return;
                }

                Material mat = Instantiate(image.material);
                image.material = mat;
                mat.SetFloat("_FillAmount", width / 360f);
                mat.SetFloat("_Symmetric", 0f);

                ZoneData newZone = new ZoneData(start, width, obj, mat);
                zones.Add(newZone);
                return;
            }
        }

        // fallback
        float fallbackWidth = Random.Range(minZoneWidth, maxZoneWidth);
        float fallbackStart = Random.Range(0f, 360f);

        GameObject fallbackObj = Instantiate(zonePrefab, zonesParent);
        fallbackObj.transform.localPosition = Vector3.zero;
        fallbackObj.transform.rotation = Quaternion.Euler(0f, 0f, fallbackStart);

        Image fallbackImage = fallbackObj.GetComponent<Image>();
        Material fallbackMat = Instantiate(fallbackImage.material);
        fallbackImage.material = fallbackMat;
        fallbackMat.SetFloat("_FillAmount", fallbackWidth / 360f);
        fallbackMat.SetFloat("_Symmetric", 0f);

        ZoneData fallbackZone = new ZoneData(fallbackStart, fallbackWidth, fallbackObj, fallbackMat);
        zones.Add(fallbackZone);
    }

    private bool ZonesOverlap(ZoneData a, ZoneData b)
    {
        List<(float, float)> intervalsA = a.GetIntervals();
        List<(float, float)> intervalsB = b.GetIntervals();
        foreach (var intA in intervalsA)
            foreach (var intB in intervalsB)
                if (SegmentsOverlap(intA.Item1, intA.Item2, intB.Item1, intB.Item2))
                    return true;
        return false;
    }

    private bool SegmentsOverlap(float a1, float a2, float b1, float b2) => a1 < b2 && b1 < a2;

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }
}