using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public ArrowController arrowController;
    public ZoneManager zoneManager;
    public TextMeshProUGUI scoreText;

    private int score = 0;

    void Start()
    {
        UpdateScoreUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            OnPressButton();
    }

    public void OnPressButton()
    {
        if (arrowController.IsFrozen)
            return;

        float arrowAngle = arrowController.ArrowAngle;
        float halfWidth = arrowController.ArrowWidth / 2f;
        float arrowStart = arrowAngle - halfWidth;
        float arrowEnd = arrowAngle + halfWidth;
        ZoneData arrowZone = new ZoneData(arrowStart, arrowController.ArrowWidth, null, null);

        ZoneData hitZone = null;
        foreach (var zone in zoneManager.Zones)
        {
            if (zoneManager.ZonesOverlap(arrowZone, zone))
            {
                hitZone = zone;
                break;
            }
        }

        if (hitZone != null)
        {
            // Попадание
            float zoneStart = hitZone.startAngle;
            float zoneEnd = hitZone.startAngle + hitZone.width;
            // Нормализуем для вывода (если end > 360, то покажем как [start, 360) и [0, end-360])
            if (zoneEnd > 360f)
            {
                Debug.Log($"Попадание! Стрелка [{arrowStart:F1}°, {arrowEnd:F1}°] пересекла зону [{zoneStart:F1}°, 360°) или [0°, {zoneEnd - 360f:F1}°]");
            }
            else
            {
                Debug.Log($"Попадание! Стрелка [{arrowStart:F1}°, {arrowEnd:F1}°] пересекла зону [{zoneStart:F1}°, {zoneEnd:F1}°]");
            }

            zoneManager.RemoveZone(hitZone);
            score++;
            UpdateScoreUI();
        }
        else
        {
            // Промах – выводим все зоны для информации
            string zonesInfo = "";
            foreach (var zone in zoneManager.Zones)
            {
                float zStart = zone.startAngle;
                float zEnd = zone.startAngle + zone.width;
                if (zEnd > 360f)
                    zonesInfo += $"[{zStart:F1}°, 360°)∪[0°, {zEnd - 360f:F1}°] ";
                else
                    zonesInfo += $"[{zStart:F1}°, {zEnd:F1}°] ";
            }
            Debug.Log($"Промах! Стрелка [{arrowStart:F1}°, {arrowEnd:F1}°] не пересекла ни одну зону. Зоны на поле: {zonesInfo}");
            arrowController.Freeze();
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }
}