using UnityEngine;

// Ajoute un léger scintillement (alpha + scale) sur un SpriteRenderer.
[RequireComponent(typeof(SpriteRenderer))]
public class AuraPulse : MonoBehaviour
{
    public float pulseSpeed = 3f;
    public float alphaMin = 0.35f;
    public float alphaMax = 0.9f;
    public float scaleMin = 0.9f;
    public float scaleMax = 1.1f;

    SpriteRenderer sr;
    Vector3 baseScale;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float a = Mathf.Lerp(alphaMin, alphaMax, t);
        float s = Mathf.Lerp(scaleMin, scaleMax, t);

        if (sr != null)
        {
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }

        transform.localScale = baseScale * s;
    }
}
