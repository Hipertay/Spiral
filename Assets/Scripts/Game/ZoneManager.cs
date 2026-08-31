using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZoneManager : MonoBehaviour
{
    [Header("References")]
    public GameObject zonePrefab;
    public Transform zonesParent;

    [Header("Settings")]
    public int initialZoneCount = 2;
    public int maxZoneCount = 5;
    public float spawnInterval = 2f;
    public float minZoneWidth = 10f;
    public float maxZoneWidth = 30f;

    // ƒиапазоны скоростей дл€ разных типов зон
    public float minShrinkSpeed = 2f;
    public float maxShrinkSpeed = 8f;
    public float minRotationSpeed = 20f;
    public float maxRotationSpeed = 60f;

    private List<ZoneData> zones = new List<ZoneData>();
    public List<ZoneData> Zones => zones;

    void Start()
    {
        for (int i = 0; i < initialZoneCount; i++)
            GenerateZone();

        StartCoroutine(SpawnRoutine());
    }

    void Update()
    {
        List<ZoneData> toRemove = new List<ZoneData>();
        foreach (var zone in zones)
        {
            if (zone.UpdateZone(Time.deltaTime))
                toRemove.Add(zone);
        }

        foreach (var zone in toRemove)
        {
            zones.Remove(zone);
            Destroy(zone.gameObject);
        }
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

    private void GenerateZone()
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            float width = Random.Range(minZoneWidth, maxZoneWidth);
            float start = Random.Range(0f, 360f);
            ZoneData testZone = new ZoneData(start, width, null, null);

            if (!HasOverlap(testZone))
            {
                CreateZoneObject(start, width);
                return;
            }
        }

        // fallback
        float fallbackWidth = Random.Range(minZoneWidth, maxZoneWidth);
        float fallbackStart = Random.Range(0f, 360f);
        CreateZoneObject(fallbackStart, fallbackWidth);
    }

    private void CreateZoneObject(float startAngle, float width)
    {
        GameObject obj = Instantiate(zonePrefab, zonesParent);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.rotation = Quaternion.Euler(0f, 0f, startAngle);

        Image image = obj.GetComponent<Image>();
        if (image == null)
        {
            Debug.LogError("[ZoneManager] ѕрефаб зоны не содержит Image!");
            Destroy(obj);
            return;
        }

        Material mat = Instantiate(image.material);
        image.material = mat;
        mat.SetFloat("_FillAmount", width / 360f);
        mat.SetFloat("_Symmetric", 0f);

        // ќпредел€ем тип зоны случайно
        float shrink = 0f, rot = 0f;
        int type = Random.Range(0, 4); // 0-3
        switch (type)
        {
            case 0: // только уменьшение
                shrink = Random.Range(minShrinkSpeed, maxShrinkSpeed);
                break;
            case 1: // только вращение
                rot = Random.Range(minRotationSpeed, maxRotationSpeed) * (Random.value > 0.5f ? 1f : -1f);
                break;
            case 2: // и уменьшение, и вращение
                shrink = Random.Range(minShrinkSpeed, maxShrinkSpeed);
                rot = Random.Range(minRotationSpeed, maxRotationSpeed) * (Random.value > 0.5f ? 1f : -1f);
                break;
            case 3: // статична€ (ничего не делает)
                break;
        }

        ZoneData newZone = new ZoneData(startAngle, width, obj, mat, shrink, rot);
        zones.Add(newZone);
    }

    private bool HasOverlap(ZoneData newZone)
    {
        foreach (var existing in zones)
            if (ZonesOverlap(existing, newZone))
                return true;
        return false;
    }

    public bool ZonesOverlap(ZoneData a, ZoneData b)
    {
        List<(float, float)> intervalsA = GetIntervals(a);
        List<(float, float)> intervalsB = GetIntervals(b);
        foreach (var intA in intervalsA)
            foreach (var intB in intervalsB)
                if (SegmentsOverlap(intA.Item1, intA.Item2, intB.Item1, intB.Item2))
                    return true;
        return false;
    }

    private List<(float, float)> GetIntervals(ZoneData zone)
    {
        List<(float, float)> intervals = new List<(float, float)>();
        float start = NormalizeAngle(zone.startAngle);
        float end = zone.startAngle + zone.width;
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

    private bool SegmentsOverlap(float a1, float a2, float b1, float b2) => a1 < b2 && b1 < a2;
    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    public void RemoveZone(ZoneData zone)
    {
        if (zones.Contains(zone))
        {
            zones.Remove(zone);
            Destroy(zone.gameObject);
        }
    }
}