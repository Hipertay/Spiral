using UnityEngine;

public class ArrowController : MonoBehaviour
{
    [Header("References")]
    public GameObject arrow;

    [Header("Settings")]
    public float rotationSpeed = 100f;
    public float freezeDuration = 1.5f;
    public float arrowWidth = 10f;

    private bool isFrozen = false;
    private float freezeTimer = 0f;

    public bool IsFrozen => isFrozen;
    public float ArrowAngle => NormalizeAngle(arrow.transform.eulerAngles.z);
    public float ArrowWidth => arrowWidth;

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
    }

    public void Freeze()
    {
        isFrozen = true;
        freezeTimer = freezeDuration;
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }
}