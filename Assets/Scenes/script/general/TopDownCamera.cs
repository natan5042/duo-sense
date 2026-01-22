using UnityEngine;

public class TopDownCamera : MonoBehaviour
{
    [Header("Targets")]
    public Transform target1; // Ирис
    public Transform target2; // Ахилл

    [Header("Settings")]
    public float smoothTime = 0.2f;
    public float minZoom = 5f;
    public float maxZoom = 10f;
    public float zoomLimiter = 50f; // Чем больше, тем меньше зум реагирует на дистанцию
    public Vector3 offset = new Vector3(0, 0, -10f);

    [Header("Juice")]
    public float lookAheadAmount = 2f; // Насколько камера "заглядывает" вперед по движению

    private Vector3 velocity;
    private Camera cam;
    private Vector3 averageVel; // Средняя скорость игроков для lookAhead

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (!target1 || !target2) return;

        Move();
        Zoom();
    }

    void Move()
    {
        Vector3 centerPoint = GetCenterPoint();

        // Рассчитываем точку "впереди"
        // Пытаемся взять Rigidbody для предсказания
        Rigidbody2D rb1 = target1.GetComponent<Rigidbody2D>();
        Rigidbody2D rb2 = target2.GetComponent<Rigidbody2D>();
        
        Vector2 v1 = rb1 ? rb1.linearVelocity : Vector2.zero;
        Vector2 v2 = rb2 ? rb2.linearVelocity : Vector2.zero;
        Vector2 avgV = (v1 + v2) / 2f;

        // Смещаем цель камеры по ходу движения
        Vector3 targetPos = centerPoint + offset + (Vector3)(avgV * lookAheadAmount * 0.1f);

        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
    }

    void Zoom()
    {
        float newZoom = Mathf.Lerp(minZoom, maxZoom, GetGreatestDistance() / zoomLimiter);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, newZoom, Time.deltaTime);
    }

    float GetGreatestDistance()
    {
        var bounds = new Bounds(target1.position, Vector3.zero);
        bounds.Encapsulate(target2.position);
        
        // Возвращаем ширину или высоту области, охватывающей игроков
        return Mathf.Max(bounds.size.x, bounds.size.y);
    }

    Vector3 GetCenterPoint()
    {
        var bounds = new Bounds(target1.position, Vector3.zero);
        bounds.Encapsulate(target2.position);
        return bounds.center;
    }
}