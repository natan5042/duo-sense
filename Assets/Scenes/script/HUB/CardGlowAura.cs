using UnityEngine;

// Crée une aura lumineuse qui scintille autour d'un objet (comme une carte de niveau)
public class CardGlowAura : MonoBehaviour
{
    [Header("Aura Settings")]
    public Color auraColor = new Color(1f, 0.9f, 0.3f, 0.8f); // Jaune doré
    public float auraSize = 2.5f; // Taille de l'aura par rapport à l'objet
    public float pulseSpeed = 2f;
    public float alphaMin = 0.4f;
    public float alphaMax = 0.9f;
    public float scaleMin = 0.9f;
    public float scaleMax = 1.1f;

    [Header("Rendering")]
    public string sortingLayerName = "Default";
    public int sortingOrder = -1;
    
    [Header("Auto Setup")]
    public bool createAuraAutomatically = true;
    public bool debugMode = true;
    
    private GameObject auraObject;
    private SpriteRenderer auraSpriteRenderer;
    private Vector3 baseScale;

    void Start()
    {
        if (createAuraAutomatically)
        {
            CreateAura();
        }
    }

    void CreateAura()
    {
        if (debugMode) Debug.Log("CardGlowAura: Création de l'aura...");
        
        // Créer un objet enfant pour l'aura
        auraObject = new GameObject("Aura_Glow");
        auraObject.transform.SetParent(transform);
        auraObject.transform.localPosition = Vector3.zero;
        
        // Ajouter un SpriteRenderer
        auraSpriteRenderer = auraObject.AddComponent<SpriteRenderer>();
        
        // Créer un sprite circulaire
        auraSpriteRenderer.sprite = CreateCircleSprite(256);
        auraSpriteRenderer.color = auraColor;
        
        // Configuration du rendu
        auraSpriteRenderer.sortingLayerName = sortingLayerName;
        auraSpriteRenderer.sortingOrder = sortingOrder;
        
        // Essayer de copier le sorting layer de l'objet parent si disponible
        SpriteRenderer parentSR = GetComponent<SpriteRenderer>();
        if (parentSR != null)
        {
            auraSpriteRenderer.sortingLayerName = parentSR.sortingLayerName;
            auraSpriteRenderer.sortingOrder = parentSR.sortingOrder - 1;
            if (debugMode) Debug.Log($"CardGlowAura: Utilise le layer '{parentSR.sortingLayerName}', order {auraSpriteRenderer.sortingOrder}");
        }
        
        // Définir la taille de l'aura
        auraObject.transform.localScale = Vector3.one * auraSize;
        baseScale = auraObject.transform.localScale;
        
        if (debugMode) Debug.Log($"CardGlowAura: Aura créée avec succès! Position: {auraObject.transform.position}, Scale: {auraObject.transform.localScale}");
    }

    void Update()
    {
        if (auraSpriteRenderer == null) return;

        // Calculer la pulsation
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(alphaMin, alphaMax, t);
        float scale = Mathf.Lerp(scaleMin, scaleMax, t);

        // Appliquer l'alpha
        Color c = auraSpriteRenderer.color;
        c.a = alpha;
        auraSpriteRenderer.color = c;

        // Appliquer le scale
        auraObject.transform.localScale = baseScale * scale;
    }

    // Créer un sprite circulaire pour l'aura
    Sprite CreateCircleSprite(int resolution)
    {
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[resolution * resolution];

        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                // Créer un dégradé radial
                float normalizedDistance = distance / radius;
                
                if (normalizedDistance > 1f)
                {
                    pixels[y * resolution + x] = new Color(1f, 1f, 1f, 0f);
                }
                else
                {
                    // Créer un glow plus prononcé
                    float alpha = 1f - normalizedDistance;
                    alpha = Mathf.Pow(alpha, 1.5f); // Rendre le centre plus lumineux
                    pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
        }

        texture.SetPixels(pixels);
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 50f);
        
        if (debugMode) Debug.Log("CardGlowAura: Sprite circulaire créé");
        
        return sprite;
    }

    void OnDestroy()
    {
        // Nettoyer la texture pour éviter les fuites mémoire
        if (auraSpriteRenderer != null && auraSpriteRenderer.sprite != null)
        {
            Destroy(auraSpriteRenderer.sprite.texture);
            Destroy(auraSpriteRenderer.sprite);
        }
    }
}
