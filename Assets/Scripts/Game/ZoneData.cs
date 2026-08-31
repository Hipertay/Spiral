using UnityEngine;

[System.Serializable]
public class ZoneData
{
    public float startAngle;      // текущий начальный угол (градусы)
    public float width;           // текущая ширина (градусы)
    public GameObject gameObject;
    public Material material;

    public float shrinkSpeed;     // скорость уменьшения (градусов/сек), 0 = не уменьшается
    public float rotationSpeed;   // скорость вращения (градусов/сек), 0 = не вращается

    public ZoneData(float start, float w, GameObject obj, Material mat, float shrink = 0f, float rot = 0f)
    {
        startAngle = start;
        width = w;
        gameObject = obj;
        material = mat;
        shrinkSpeed = shrink;
        rotationSpeed = rot;
    }

    // Обновление состояния зоны. Возвращает true, если зона должна быть удалена (width <= 0)
    public bool UpdateZone(float deltaTime)
    {
        // Вращение
        if (rotationSpeed != 0f)
        {
            startAngle += rotationSpeed * deltaTime;
            if (startAngle >= 360f) startAngle -= 360f;
            if (startAngle < 0f) startAngle += 360f;
            gameObject.transform.rotation = Quaternion.Euler(0f, 0f, startAngle);
        }

        // Уменьшение ширины
        if (shrinkSpeed != 0f)
        {
            width -= shrinkSpeed * deltaTime;
            if (width <= 0f)
                return true; // зона исчезает

            material.SetFloat("_FillAmount", width / 360f);
        }

        return false;
    }

    // Проверка, содержит ли зона угол (с учётом перехода через 0)
    public bool ContainsAngle(float angle)
    {
        float end = startAngle + width;
        if (end <= 360f)
            return angle >= startAngle && angle <= end;
        else
            return angle >= startAngle || angle <= end - 360f;
    }
}