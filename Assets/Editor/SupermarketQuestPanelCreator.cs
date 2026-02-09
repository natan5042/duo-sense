using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Menu: Tools > Supermarché > Créer panneau de quêtes (lié à la caméra).
/// Crée un Canvas en Screen Space - Camera (donc fixé à l'écran et dessiné par la caméra), avec un panneau de texte pour les quêtes.
/// Pour corriger : sélectionner "QuestPanel" dans la hiérarchie, ajuster le RectTransform (position, taille), ou le Canvas > Render Mode / Sort Order.
/// </summary>
public static class SupermarketQuestPanelCreator
{
    [MenuItem("Tools/Supermarché/Créer panneau de quêtes (lié à la caméra)")]
    public static void CreateQuestPanel()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                Debug.LogError("Aucune caméra dans la scène. Créez une caméra puis relancez.");
                return;
            }
        }

        GameObject canvasGo = new GameObject("QuestCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 100f;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject panelGo = new GameObject("QuestPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        RectTransform panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.sizeDelta = new Vector2(360f, 260f);
        var panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0.15f, 0.12f, 0.22f, 0.92f);
        panelImage.raycastTarget = false;

        GameObject textGo = new GameObject("QuestText");
        textGo.transform.SetParent(panelGo.transform, false);
        RectTransform textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 10f);
        textRect.offsetMax = new Vector2(-10f, -10f);
        Text text = textGo.AddComponent<Text>();
        text.text = "Quêtes:\n\nAucune quête pour le moment.";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 18;
        text.color = new Color(1f, 1f, 1f, 1f);
        text.supportRichText = true;
        text.alignment = TextAnchor.UpperLeft;

        QuestUI questUI = canvasGo.AddComponent<QuestUI>();
        questUI.questText = text;
        questUI.questPanel = panelGo;

        canvasGo.AddComponent<ObstacleQuestStarter>();

        Undo.RegisterCreatedObjectUndo(canvasGo, "Create Quest Panel");
        Selection.activeGameObject = canvasGo;
        Debug.Log("Panneau de quêtes créé. Canvas en Screen Space - Camera : il suit la caméra. Pour déplacer : sélectionner QuestPanel et modifier RectTransform (Position X/Y, Width/Height).");
    }
}
