using UnityEngine;

public class ZoneBehavior : MonoBehaviour
{
    [Header("Zone Type Settings")]
    public bool isRotating = false;        // вращается ли зона
    public bool isShrinking = false;       // уменьшается ли зона
    public float rotationSpeed = 30f;      // скорость вращения (градусы/сек)
    public float shrinkSpeed = 5f;         // скорость уменьшения (градусы/сек)

    private ZoneData zoneData;
    private float currentWidth;

    public void Initialize(ZoneData data)
    {
        zoneData = data;
        currentWidth = data.width;
    }

    void Update()
    {
        if (zoneData == null) return;

        // Вращение
        if (isRotating)
        {
            transform.Rotate(Vector3.forward, -rotationSpeed * Time.deltaTime);
            // Обновляем startAngle в данных (чтобы проверка попадания работала корректно)
            zoneData.startAngle = NormalizeAngle(transform.eulerAngles.z);
        }

        // Уменьшение
        if (isShrinking)
        {
            currentWidth -= shrinkSpeed * Time.deltaTime;
            if (currentWidth <= 0f)
            {
                // Зона исчезает – будет удалена ZoneManager
                return;
            }
            zoneData.width = currentWidth;
            zoneData.material.SetFloat("_FillAmount", currentWidth / 360f);
        }
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    // Метод для получения текущей ширины (используется ZoneManager)
    public float GetCurrentWidth() => currentWidth;

    // Метод для обновления ширины (используется ZoneManager при инициализации)
    public void SetWidth(float w)
    {
        currentWidth = w;
        zoneData.width = w;
        zoneData.material.SetFloat("_FillAmount", w / 360f);
    }
}