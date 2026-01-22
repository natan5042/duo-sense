using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownPlayer : MonoBehaviour
{
    [Header("Настройки Движения")]
    public float moveSpeed = 6f;
    
    [Header("Настройки Рывка")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public KeyCode dashKey = KeyCode.LeftShift;

    [Header("Взаимодействие")]
    public TopDownWheelchair wheelchair;
    public float interactRadius = 1.5f;
    public KeyCode interactKey = KeyCode.F;
    
    [Header("Визуал (Спрайты ДВИЖЕНИЯ)")]
    public SpriteRenderer spriteRenderer; 
    public Sprite viewDown; // Идет вниз (лицом)
    public Sprite viewUp;   // Идет вверх (спиной)
    public Sprite viewSide; // Идет вбок

    [Header("Визуал (Спрайты ПОКОЯ / Idle)")] // НОВЫЕ ПОЛЯ
    public Sprite idleDown; // Стоит лицом
    public Sprite idleUp;   // Стоит спиной
    public Sprite idleSide; // Стоит боком

    private Rigidbody2D rb;
    private Vector2 moveInput;
    
    // Инициализируем вектором вниз
    private Vector2 lastMoveDir = Vector2.down; 
    
    private bool isPushing = false;
    private bool isDashing = false;
    private float lastDashTime = -10f;
    private Vector3 originalScale;

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
        
        lastMoveDir = Vector2.down;
    }

    void Update()
    {
        // Блокируем движение если активна мини-игра
        if (ShelfGame.IsShelfGameActive || CookingGame.IsCookingGameActive || ClothesLineGame.IsMiniGameActive)
        {
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        // 1. Ввод
        float x = 0f; float y = 0f;
        if (Input.GetKey(KeyCode.A)) x = -1f;
        if (Input.GetKey(KeyCode.D)) x = 1f;
        if (Input.GetKey(KeyCode.W)) y = 1f;
        if (Input.GetKey(KeyCode.S)) y = -1f;
        moveInput = new Vector2(x, y).normalized;

        // Проверяем, двигаемся ли мы сейчас
        bool isMoving = moveInput.magnitude > 0.01f;

        // Обновляем последнее направление
        if (isMoving)
        {
            lastMoveDir = moveInput;
        }

        // 2. Рывок - ОТКЛЮЧЕН для ирис
        // if (Input.GetKeyDown(dashKey) && !isPushing && !isDashing)
        // {
        //     if (Time.time >= lastDashTime + dashCooldown) StartCoroutine(DashRoutine());
        // }

        // 3. Взять / Бросить
        if (Input.GetKeyDown(interactKey) && wheelchair != null)
        {
            if (isPushing) ReleaseWheelchair();
            else if (Vector2.Distance(transform.position, wheelchair.transform.position) <= interactRadius && !wheelchair.isStuck) 
                GrabWheelchair();
        }

        // 4. Обновляем картинку
        // Передаем: Двигаемся ли мы?, и В какую сторону смотреть?
        UpdateVisuals(isMoving, isMoving ? moveInput : lastMoveDir);
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        rb.linearVelocity = moveInput * moveSpeed;

        if (isPushing && wheelchair != null)
        {
            wheelchair.DriveByPlayer(transform.position, lastMoveDir);
        }
    }

    // Метод изменен: теперь он знает, двигаемся мы или нет
    void UpdateVisuals(bool isMoving, Vector2 dir)
    {
        if (spriteRenderer == null) return;

        // --- ВЫБОР СПРАЙТА ---
        if (isMoving)
        {
            // => ДВИЖЕНИЕ (используем спрайты view...)
            if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x))
            {
                if (dir.y > 0 && viewUp != null) spriteRenderer.sprite = viewUp;
                else if (dir.y < 0 && viewDown != null) spriteRenderer.sprite = viewDown;
            }
            else if (Mathf.Abs(dir.x) > 0)
            {
                if (viewSide != null) spriteRenderer.sprite = viewSide;
            }
        }
        else
        {
            // => ПОКОЙ / IDLE (используем новые спрайты idle...)
            // Мы используем dir (который сейчас равен lastMoveDir), чтобы понять, как мы стоим
            if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x))
            {
                if (dir.y > 0 && idleUp != null) spriteRenderer.sprite = idleUp;
                else if (dir.y < 0 && idleDown != null) spriteRenderer.sprite = idleDown;
            }
            else
            {
                // Если стоим боком (влево или вправо - неважно, флип сработает ниже)
                if (idleSide != null) spriteRenderer.sprite = idleSide;
            }
        }

        // --- ЗЕРКАЛИВАНИЕ (Flip) ---
        // Работает одинаково и для движения, и для покоя
        if (dir.x < -0.01f)
            spriteRenderer.transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else if (dir.x > 0.01f)
            spriteRenderer.transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
    }

    void GrabWheelchair()
    {
        IgnoreCollisions(true);
        isPushing = true;
        wheelchair.SetCaptured(true);
        if (lastMoveDir.magnitude < 0.01f) lastMoveDir = Vector2.down;
    }

    void ReleaseWheelchair()
    {
        isPushing = false;
        wheelchair.SetCaptured(false);
        IgnoreCollisions(false);
    }

    void IgnoreCollisions(bool ignore)
    {
        Collider2D[] myCols = GetComponentsInChildren<Collider2D>();
        Collider2D[] wCols = wheelchair.GetComponentsInChildren<Collider2D>();
        foreach (var m in myCols) foreach (var w in wCols) Physics2D.IgnoreCollision(m, w, ignore);
    }

    IEnumerator DashRoutine()
    {
        isDashing = true;
        lastDashTime = Time.time;
        Vector2 dir = moveInput.magnitude > 0 ? moveInput : lastMoveDir;
        rb.linearVelocity = dir * dashSpeed;
        yield return new WaitForSeconds(dashDuration);
        rb.linearVelocity = Vector2.zero;
        isDashing = false;
    }
}