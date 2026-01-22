using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownWheelchair : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public bool isStuck = false;
    
    [Header("Visuals (Спрайты)")]
    public SpriteRenderer spriteRenderer; 
    public Sprite viewDown; 
    public Sprite viewUp;   
    public Sprite viewSide; 
    
    public float holdDistance = 1.2f; // Немного увеличил дистанцию для наглядности

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isCaptured = false;
    private Vector3 originalScale;
    private Vector2 lastDir = Vector2.down; 

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void Start()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) originalScale = spriteRenderer.transform.localScale;
    }

    void Update()
    {
        // Блокируем движение если активна мини-игра готовки
        if (CookingGame.IsCookingGameActive || ClothesLineGame.IsMiniGameActive)
        {
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        if (isStuck || isCaptured) return;

        // Управление стрелочками
        float x = 0f; float y = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) x = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) x = 1f;
        if (Input.GetKey(KeyCode.UpArrow)) y = 1f;
        if (Input.GetKey(KeyCode.DownArrow)) y = -1f;
        moveInput = new Vector2(x, y).normalized;
        
        if (moveInput.magnitude > 0.01f) lastDir = moveInput;

        UpdateVisuals(moveInput.magnitude > 0.01f ? moveInput : lastDir);
    }

    void FixedUpdate()
    {
        if (isStuck || isCaptured) return;
        rb.linearVelocity = moveInput * moveSpeed;
    }

    public void UpdateVisuals(Vector2 dir)
    {
        if (spriteRenderer == null) return;

        if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x))
        {
            if (dir.y > 0 && viewUp != null) spriteRenderer.sprite = viewUp;
            else if (dir.y < 0 && viewDown != null) spriteRenderer.sprite = viewDown;
        }
        else if (Mathf.Abs(dir.x) > 0)
        {
            if (viewSide != null) spriteRenderer.sprite = viewSide;
        }

        if (dir.x < -0.01f)
            spriteRenderer.transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else if (dir.x > 0.01f)
            spriteRenderer.transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
    }

    public void SetCaptured(bool captured)
    {
        isCaptured = captured;
        if (captured) rb.linearVelocity = Vector2.zero;
    }

    public void DriveByPlayer(Vector3 playerPos, Vector2 dir)
    {
        // ЗАЩИТА: Если направление нулевое (глюк), не двигаем позицию, оставляем как было
        if (dir.magnitude < 0.01f) return;

        // 1. Обновляем визуал 
        UpdateVisuals(dir);

        // 2. Двигаем физику
        // Коляска всегда находится "Перед" игроком на расстоянии holdDistance
        Vector3 offset = new Vector3(dir.x, dir.y, 0).normalized * holdDistance;
        rb.MovePosition(playerPos + offset);
    }
    
    public void Recover() { isStuck = false; }
}