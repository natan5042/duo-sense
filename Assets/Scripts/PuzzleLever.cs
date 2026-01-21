using UnityEngine;

// Interactive lever for puzzle rows (upper/lower). Handles input, toggle, anim, sound.
public class PuzzleLever : MonoBehaviour
{
    [Header("Input")]
    public KeyCode interactKeyPlayer1 = KeyCode.F;        // клавиша для игрока на WASD
    public KeyCode interactKeyPlayer2 = KeyCode.Keypad1;  // клавиша для игрока на стрелках
    public float interactRadius = 1.5f;

    [Header("Player References")]
    public Transform player1; // ссылка на объект игрока 1 в сцене
    public Transform player2; // ссылка на объект игрока 2 в сцене

    [Header("Visuals & Sound")]
    public Animator leverAnimator;
    public string leverBoolName = "isOn";
    public AudioSource leverSound;

    public bool IsOn { get; private set; }

    private bool player1InRange;
    private bool player2InRange;
    private bool lastFrameKey1Pressed = false;
    private bool lastFrameKey2Pressed = false;

    void Update()
    {
        CheckPlayer();

        bool key1Pressed = Input.GetKey(interactKeyPlayer1);
        bool key2Pressed = Input.GetKey(interactKeyPlayer2);

        // Игрок 1 (WASD) - только при нажатии, не удержании
        if (player1InRange && key1Pressed && !lastFrameKey1Pressed)
        {
            Toggle();
        }

        // Игрок 2 (стрелки) - только при нажатии, не удержании
        if (player2InRange && key2Pressed && !lastFrameKey2Pressed)
        {
            Toggle();
        }

        lastFrameKey1Pressed = key1Pressed;
        lastFrameKey2Pressed = key2Pressed;
    }

    void CheckPlayer()
    {
        player1InRange = false;
        player2InRange = false;

        Vector2 pos = transform.position;

        if (player1 != null)
        {
            float dist1 = Vector2.Distance(pos, player1.position);
            player1InRange = dist1 <= interactRadius;
        }
        else
        {
            Debug.LogWarning($"[PuzzleLever] {gameObject.name}: player1 не привязан!");
        }

        if (player2 != null)
        {
            float dist2 = Vector2.Distance(pos, player2.position);
            player2InRange = dist2 <= interactRadius;
        }
        else
        {
            Debug.LogWarning($"[PuzzleLever] {gameObject.name}: player2 не привязан!");
        }
    }

    public void Toggle()
    {
        IsOn = !IsOn;

        if (leverAnimator != null)
        {
            if (leverAnimator.enabled)
            {
                // Проверяем, что это именно наш Animator, а не общий
                if (leverAnimator.gameObject == gameObject || leverAnimator.transform.IsChildOf(transform) || transform.IsChildOf(leverAnimator.transform))
                {
                    leverAnimator.SetBool(leverBoolName, IsOn);
                    Debug.Log($"[PuzzleLever] {gameObject.name}: Toggle -> IsOn = {IsOn}, установлен параметр {leverBoolName} на Animator {leverAnimator.gameObject.name}");
                }
                else
                {
                    Debug.LogError($"[PuzzleLever] {gameObject.name}: leverAnimator принадлежит другому объекту ({leverAnimator.gameObject.name})! Каждый рычаг должен иметь свой Animator!");
                }
            }
            else
            {
                Debug.LogWarning($"[PuzzleLever] {gameObject.name}: Animator отключен (enabled = false)!");
            }
        }
        else
        {
            Debug.LogWarning($"[PuzzleLever] {gameObject.name}: leverAnimator не привязан!");
        }

        if (leverSound)
            leverSound.Play();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}

