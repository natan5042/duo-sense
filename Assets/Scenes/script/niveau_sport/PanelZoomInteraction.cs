using UnityEngine;
using UnityEngine.UI;

public class PanelZoomInteraction : MonoBehaviour, IInteractable
{
    public Sprite zoomSprite;
    [Range(0.3f, 1f)]
    public float imageHeightRatio = 0.75f;
    public bool dimBackground = true;
    public Color dimColor = new Color(0f, 0f, 0f, 0.65f);
    public float interactionDistance = 4f;
    public KeyCode interactKey1 = KeyCode.S;
    public KeyCode interactKey2 = KeyCode.DownArrow;
    public AudioClip openSound;
    public AudioSource audioSource;

    private Transform panelTransform;
    private bool isZoomed;
    private bool isPlayerNear;
    private Transform playerTransform;
    private GameObject overlayRoot;
    private Image overlayImage;

    void Start()
    {
        panelTransform = transform;
        if (audioSource == null && openSound != null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null && openSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (GetComponent<Collider2D>() == null)
        {
            var trigger = gameObject.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = interactionDistance;
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        float dist = Vector3.Distance(panelTransform.position, playerTransform.position);
        isPlayerNear = dist <= interactionDistance;

        if (!isPlayerNear)
        {
            if (isZoomed) SetZoomed(false);
            return;
        }

        if (Input.GetKeyDown(interactKey1) || Input.GetKeyDown(interactKey2))
        {
            SetZoomed(!isZoomed);
        }
    }

    void SetZoomed(bool zoom)
    {
        isZoomed = zoom;
        if (zoom)
            ShowOverlay();
        else
            HideOverlay();
    }

    Sprite GetSpriteToShow()
    {
        if (zoomSprite != null) return zoomSprite;
        var sr = GetComponent<SpriteRenderer>();
        return sr != null ? sr.sprite : null;
    }

    void ShowOverlay()
    {
        Sprite sprite = GetSpriteToShow();
        if (sprite == null) return;

        if (overlayRoot == null)
            BuildOverlay();

        if (overlayImage != null)
            overlayImage.sprite = sprite;

        overlayRoot.SetActive(true);

        if (openSound != null && audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = openSound;
            audioSource.Play();
        }
    }

    void HideOverlay()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(false);
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    void BuildOverlay()
    {
        overlayRoot = new GameObject("PanelZoomOverlay");
        overlayRoot.transform.SetParent(null);

        var canvas = overlayRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        var scaler = overlayRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        overlayRoot.AddComponent<GraphicRaycaster>();

        if (dimBackground)
        {
            GameObject dimGo = new GameObject("Dim");
            dimGo.transform.SetParent(overlayRoot.transform, false);
            var dimImage = dimGo.AddComponent<Image>();
            dimImage.color = dimColor;
            dimImage.raycastTarget = true;
            RectTransform dimRect = dimGo.GetComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
        }

        GameObject imgGo = new GameObject("PanelImage");
        imgGo.transform.SetParent(overlayRoot.transform, false);
        overlayImage = imgGo.AddComponent<Image>();
        overlayImage.preserveAspect = true;
        overlayImage.raycastTarget = true;

        RectTransform imgRect = imgGo.GetComponent<RectTransform>();
        imgRect.anchorMin = new Vector2(0.5f, 0.5f);
        imgRect.anchorMax = new Vector2(0.5f, 0.5f);
        imgRect.pivot = new Vector2(0.5f, 0.5f);
        imgRect.anchoredPosition = Vector2.zero;
        float h = 1080f * imageHeightRatio;
        float w = 1920f * 0.85f;
        imgRect.sizeDelta = new Vector2(w, h);

        overlayRoot.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null || other.GetComponent<WheelchairMovement>() != null || other.GetComponent<TopDownMovement>() != null)
        {
            isPlayerNear = true;
            playerTransform = other.transform;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.transform == playerTransform)
        {
            isPlayerNear = false;
            playerTransform = null;
            if (isZoomed) SetZoomed(false);
        }
    }

    public void Interact(PlayerInteraction player)
    {
        if (!isPlayerNear && player != null)
            isPlayerNear = Vector3.Distance(panelTransform.position, player.transform.position) <= interactionDistance;
        if (isPlayerNear)
            SetZoomed(!isZoomed);
    }
}
