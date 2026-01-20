using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Affiche des sous-titres en bas, centrés, avec défilement auto si le texte dépasse.
public class SubtitleUI : MonoBehaviour
{
    public enum SubtitleHorizontalAlign { Left, Center, Right }

    public static SubtitleUI Instance;

    [Header("Références UI")]
    public Text subtitleText;
    public TextMeshProUGUI subtitleTMP;
    public CanvasGroup canvasGroup;

    [Header("Réglages")]
    public float fadeDuration = 0.15f;
    public float horizontalPadding = 64f; // marge gauche/droite pour limiter la largeur
    public float offsetY = 60f;            // position verticale (bas)
    public float offsetX = 0f;             // position horizontale
    public bool stretchWidth = false;      // false = bloc réduit
    [Range(0f,1f)] public float anchorY = 0f; // 0 = bas
    public SubtitleHorizontalAlign horizontalAlign = SubtitleHorizontalAlign.Center;
    public bool autoAssignChildren = true;
    public bool debugLogs = false;
    public int maxLines = 3;               // limite douce en lignes
    [Range(0.3f,1f)] public float viewportWidthFactor = 0.55f; // largeur relative max
    public bool forceCenter = true;        // force l'ancrage bas-centre

    [Header("Défilement si trop long")]
    public bool enableAutoScroll = true;
    public float scrollSpeed = 120f;          // pixels/sec
    public float scrollPause = 0.5f;          // pause avant le mouvement
    public float scrollReturnDuration = 0.3f; // inutilisé si one-way
    public bool ensureMaskOnParent = true;    // ajoute un RectMask2D sur le parent pour découper le texte

    Coroutine displayRoutine;
    Coroutine scrollRoutine;
    RectTransform currentTextRT;
    Vector2 baseAnchoredPos;

    public static SubtitleUI GetOrFindInstance()
    {
        if (Instance != null) return Instance;

        var all = Resources.FindObjectsOfTypeAll<SubtitleUI>();
        if (all != null && all.Length > 0)
        {
            Instance = all[0];
            return Instance;
        }

        Debug.LogWarning("SubtitleUI : aucun instance trouvée dans la scène. Place un objet avec SubtitleUI dans un Canvas Overlay.");
        return null;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        AutoAssignIfNeeded();
        ConfigureTargets();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void ShowSubtitle(string text, float duration)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!HasTextTarget()) return;

        StopScroll();

        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (canvasGroup != null && !canvasGroup.gameObject.activeSelf) canvasGroup.gameObject.SetActive(true);

        SetText(text);
        ResizeForText();

        if (displayRoutine != null)
        {
            StopCoroutine(displayRoutine);
        }
        displayRoutine = StartCoroutine(ShowRoutine(duration));
    }

    IEnumerator ShowRoutine(float duration)
    {
        yield return StartCoroutine(FadeTo(1f));
        yield return new WaitForSeconds(duration);
        yield return StartCoroutine(FadeTo(0f));
        StopScroll();
    }

    IEnumerator FadeTo(float target)
    {
        if (canvasGroup == null)
        {
            SetVisible(target > 0.9f);
            yield break;
        }

        float start = canvasGroup.alpha;
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, time / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = target;
        SetVisible(target > 0.9f);
    }

    bool HasTextTarget()
    {
        bool ok = subtitleText != null || subtitleTMP != null;
        if (!ok && debugLogs)
        {
            Debug.LogWarning("SubtitleUI : aucun Text ni TMP assigné ou trouvé.");
        }
        return ok;
    }

    void SetText(string text)
    {
        if (subtitleText != null) subtitleText.text = text;
        if (subtitleTMP != null) subtitleTMP.text = text;
    }

    void SetVisible(bool visible)
    {
        if (subtitleText != null)
        {
            subtitleText.enabled = visible;
            if (!subtitleText.gameObject.activeSelf) subtitleText.gameObject.SetActive(true);
        }
        if (subtitleTMP != null)
        {
            subtitleTMP.enabled = visible;
            if (!subtitleTMP.gameObject.activeSelf) subtitleTMP.gameObject.SetActive(true);
        }
    }

    void ConfigureTargets()
    {
        ConfigureText(subtitleText);
        ConfigureTMP(subtitleTMP);
    }

    void AutoAssignIfNeeded()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (autoAssignChildren)
        {
            if (subtitleText == null)
            {
                subtitleText = GetComponentInChildren<Text>(true);
            }
            if (subtitleTMP == null)
            {
                subtitleTMP = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        var parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning("SubtitleUI : aucun Canvas parent trouvé. Ajoutez ce composant sous un Canvas en Screen Space Overlay.");
            }
        }
        else if (parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            if (debugLogs)
            {
                Debug.LogWarning("SubtitleUI : Canvas parent non Overlay. Placez-le en Screen Space Overlay pour afficher les sous-titres.");
            }
        }
    }

    void ConfigureText(Text t)
    {
        if (t == null) return;
        t.alignment = TextAnchor.LowerCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
    }

    void ConfigureTMP(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        tmp.alignment = TextAlignmentOptions.BottomGeoAligned;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.maxVisibleLines = int.MaxValue; // on laisse tout s'afficher et on masque/scroll
    }

    void ApplyLayout(RectTransform rt, float height, float preferredWidth, float maxWidth)
    {
        float pivotX = GetPivotX(horizontalAlign);
        float yAnchor = forceCenter ? 0f : anchorY;
        float clampedWidth = Mathf.Max(64f, Mathf.Min(preferredWidth, maxWidth));

        if (stretchWidth)
        {
            rt.anchorMin = new Vector2(0f, yAnchor);
            rt.anchorMax = new Vector2(1f, yAnchor);
            rt.pivot = new Vector2(0.5f, yAnchor);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = new Vector2(offsetX, offsetY);
        }
        else
        {
            rt.anchorMin = new Vector2(0.5f, yAnchor);
            rt.anchorMax = new Vector2(0.5f, yAnchor);
            rt.pivot = new Vector2(0.5f, yAnchor);
            rt.sizeDelta = new Vector2(clampedWidth, height);
            rt.anchoredPosition = new Vector2(offsetX, offsetY);
        }
    }

    void ResizeForText()
    {
        RectTransform rt = null;
        float maxByPadding = Screen.width - (horizontalPadding * 2f);
        float viewportWidth = Mathf.Clamp(Screen.width * viewportWidthFactor, 64f, maxByPadding);
        float viewportHeight = 0f;

        if (subtitleTMP != null)
        {
            rt = subtitleTMP.rectTransform;
            float availableWidth = viewportWidth;
            var preferredVec = subtitleTMP.GetPreferredValues(subtitleTMP.text, availableWidth, float.PositiveInfinity);
            float preferredHeightFull = Mathf.Max(preferredVec.y, 32f);
            float lineHeight = subtitleTMP.fontSize;
            float maxH = maxLines > 0 ? lineHeight * maxLines * 1.2f : preferredHeightFull;
            float targetHeight = Mathf.Min(preferredHeightFull, maxH);
            viewportHeight = targetHeight;
            ApplyLayout(rt, targetHeight + 8f, availableWidth, viewportWidth);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            float contentWidth = subtitleTMP.GetPreferredValues(subtitleTMP.text, float.PositiveInfinity, float.PositiveInfinity).x;
            HandleScrolling(rt, viewportWidth, contentWidth, targetHeight, preferredHeightFull);
        }
        else if (subtitleText != null)
        {
            rt = subtitleText.rectTransform;
            float availableWidth = viewportWidth;
            var settings = subtitleText.GetGenerationSettings(new Vector2(availableWidth, float.PositiveInfinity));
            float preferredHeightFull = subtitleText.cachedTextGeneratorForLayout.GetPreferredHeight(subtitleText.text, settings) / subtitleText.pixelsPerUnit;
            preferredHeightFull = Mathf.Max(preferredHeightFull, 32f);
            float lineHeight = subtitleText.fontSize;
            float maxH = maxLines > 0 ? lineHeight * maxLines * 1.2f : preferredHeightFull;
            float targetHeight = Mathf.Min(preferredHeightFull, maxH);
            viewportHeight = targetHeight;
            ApplyLayout(rt, targetHeight + 8f, availableWidth, viewportWidth);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            float contentWidth = subtitleText.cachedTextGeneratorForLayout.GetPreferredWidth(subtitleText.text, settings) / subtitleText.pixelsPerUnit;
            HandleScrolling(rt, viewportWidth, contentWidth, targetHeight, preferredHeightFull);
        }
    }

    void HandleScrolling(RectTransform rt, float viewportWidth, float contentWidth, float viewportHeight, float contentHeight)
    {
        currentTextRT = rt;
        baseAnchoredPos = rt.anchoredPosition;

        if (!enableAutoScroll || rt == null)
        {
            ResetScrollPosition();
            return;
        }

        bool overflowY = contentHeight > viewportHeight + 1f;

        if (!overflowY)
        {
            ResetScrollPosition();
            return;
        }

        EnsureMaskOnParent(rt);

        if (scrollRoutine != null) StopCoroutine(scrollRoutine);

        Vector2 start = baseAnchoredPos;
        Vector2 end = baseAnchoredPos;

        float overflow = contentHeight - viewportHeight;
        end = new Vector2(baseAnchoredPos.x, baseAnchoredPos.y + overflow); // monte doucement, une seule fois

        scrollRoutine = StartCoroutine(ScrollTextOneWay(rt, start, end));
    }

    IEnumerator ScrollTextOneWay(RectTransform rt, Vector2 start, Vector2 end)
    {
        rt.anchoredPosition = start;
        yield return new WaitForSeconds(scrollPause);

        float distance = Vector2.Distance(start, end);
        float duration = distance / Mathf.Max(10f, scrollSpeed);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            rt.anchoredPosition = Vector2.Lerp(start, end, p);
            yield return null;
        }

        rt.anchoredPosition = end;
    }

    void EnsureMaskOnParent(RectTransform rt)
    {
        if (!ensureMaskOnParent || rt == null) return;
        var parent = rt.parent as RectTransform;
        if (parent == null) return;
        if (parent.GetComponent<RectMask2D>() == null)
        {
            parent.gameObject.AddComponent<RectMask2D>();
        }
    }

    void StopScroll()
    {
        if (scrollRoutine != null)
        {
            StopCoroutine(scrollRoutine);
            scrollRoutine = null;
        }
        ResetScrollPosition();
    }

    void ResetScrollPosition()
    {
        if (currentTextRT != null)
        {
            currentTextRT.anchoredPosition = baseAnchoredPos;
        }
    }

    float GetPivotX(SubtitleHorizontalAlign align)
    {
        switch (align)
        {
            case SubtitleHorizontalAlign.Left: return 0f;
            case SubtitleHorizontalAlign.Right: return 1f;
            default: return 0.5f;
        }
    }
}
