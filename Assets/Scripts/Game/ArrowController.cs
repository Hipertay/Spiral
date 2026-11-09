using UnityEngine;

public class ArrowController : MonoBehaviour
{
    [Header("References")]
    public GameObject arrow;

    [Header("Settings")]
    public float rotationSpeed = 100f;
    public float freezeDuration = 1.5f;
    public float arrowWidth = 10f;

    [Tooltip("Где появляются Fly-объекты: расстояние от центра стрелки как доля её высоты (0.45 при высоте 660 — примерно кончик).")]
    [Range(0f, 0.5f)]
    public float flySpawnRadius = 0.45f;

    private bool isFrozen;
    private float freezeTimer;

    private bool slowActive;
    private float slowTimer;
    private float slowFactor = 1f;

    public bool IsFrozen => isFrozen;
    public bool IsRunning { get; private set; }
    public float ArrowAngle => NormalizeAngle(arrow.transform.eulerAngles.z);
    public float ArrowWidth => arrowWidth;

    /// <summary>
    /// Мировая точка стрелки для появления Fly-объектов: вдоль «носа» стрелки на
    /// flySpawnRadius от её центра (0 — центр, 0.5 — самый край).
    /// </summary>
    public Vector3 FlySpawnPosition
    {
        get
        {
            if (arrow == null)
                return transform.position;

            RectTransform rect = arrow.transform as RectTransform;

            if (rect == null)
                return arrow.transform.position;

            float distance = Mathf.Abs(rect.rect.height) * flySpawnRadius;

            return arrow.transform.TransformPoint(new Vector3(0f, distance, 0f));
        }
    }

    /// <summary>Текущая скорость вращения с учётом временного замедления.</summary>
    public float EffectiveSpeed => slowActive ? rotationSpeed * slowFactor : rotationSpeed;

    /// <summary>Множитель скорости от реликов (ПОПУТНЫЙ ВЕТЕР / ЯКОРЬ). Ставит GameManager.</summary>
    public float SpeedMultiplier { get; set; } = 1f;

    void Update()
    {
        if (!IsRunning)
            return;

        if (slowActive)
        {
            slowTimer -= Time.deltaTime;

            if (slowTimer <= 0f)
            {
                slowActive = false;
                slowTimer = 0f;
                slowFactor = 1f;
            }
        }

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
            arrow.transform.Rotate(Vector3.forward, -EffectiveSpeed * SpeedMultiplier * Time.deltaTime);
        }
    }

    /// <summary>
    /// Запускает вращение с заданной скоростью. Угол стрелки не сбрасывается.
    /// </summary>
    public void StartRotation(float speed)
    {
        rotationSpeed = speed;
        isFrozen = false;
        freezeTimer = 0f;
        slowActive = false;
        slowTimer = 0f;
        slowFactor = 1f;
        IsRunning = true;
    }

    /// <summary>Полная остановка вращения (пауза боя на анимацию врага).</summary>
    public void StopRotation()
    {
        IsRunning = false;
        isFrozen = false;
        freezeTimer = 0f;
    }

    /// <summary>Продолжает вращение с текущей скоростью после StopRotation.</summary>
    public void ResumeRotation()
    {
        IsRunning = true;
    }

    /// <summary>
    /// Временно замедляет стрелку до factor от текущей скорости на duration секунд,
    /// затем возвращает прежнюю скорость.
    /// </summary>
    public void SlowDown(float factor, float duration)
    {
        if (!IsRunning || duration <= 0f)
            return;

        slowFactor = Mathf.Clamp(factor, 0.01f, 1f);
        slowTimer = duration;
        slowActive = true;
    }

    /// <summary>Заморозка на заданное время (СНОРОВКА сокращает паузу).</summary>
    public void Freeze(float duration)
    {
        if (!IsRunning)
            return;

        isFrozen = true;
        freezeTimer = Mathf.Max(0f, duration);
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }
}