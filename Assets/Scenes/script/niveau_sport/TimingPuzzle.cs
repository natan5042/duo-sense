using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Puzzle de timing bip + zone verte
public class TimingPuzzle : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource bipSource;
    public AudioClip bipClip;
    [Tooltip("Intervalle entre deux bips (s)")]
    public float bipInterval = 2f;
    [Tooltip("Tolérance après le bip pour valider le clic (s)")]
    public float allowedWindow = 0.25f;

    [Header("Mouvement du curseur")]
    public RectTransform marker;          // l'indicateur qui bouge
    public RectTransform goodZone;        // la zone verte à viser
    public Vector2 markerRange = new Vector2(-200f, 200f); // déplacement horizontal (en px)
    public float movePeriod = 2f;         // temps pour un aller-retour (s)

    [Header("Résultat")]
    public GameObject[] cloudsToHide;     // nuages à désactiver quand c'est réussi
    public AudioSource successSource;
    public AudioClip successClip;
    public AudioSource failSource;
    public AudioClip failClip;

    float lastBipTime = -999f;
    bool solved;

    void Start()
    {
        StartCoroutine(BipLoop());
    }

    IEnumerator BipLoop()
    {
        while (!solved)
        {
            if (bipSource != null && bipClip != null)
            {
                bipSource.spatialBlend = 0f; // son UI en 2D
                bipSource.PlayOneShot(bipClip);
            }
            Debug.Log("[TimingPuzzle] Bip joué");
            lastBipTime = Time.time;
            yield return new WaitForSeconds(bipInterval);
        }
    }

    void Update()
    {
        if (solved) return;
        AnimateMarker();

        if (Input.GetMouseButtonDown(0))
        {
            bool timingOK = Time.time - lastBipTime <= allowedWindow;
            bool posOK = IsMarkerInZone();

            if (timingOK && posOK)
            {
                Solve();
            }
            else
            {
                Debug.Log($"[TimingPuzzle] Raté: timingOK={timingOK} posOK={posOK} (dtBip={Time.time - lastBipTime:F2}s, window={allowedWindow})");
                PlayFail();
            }
        }
    }

    void AnimateMarker()
    {
        if (marker == null) return;
        float t = Mathf.PingPong(Time.time, movePeriod) / movePeriod;
        float x = Mathf.Lerp(markerRange.x, markerRange.y, t);
        var pos = marker.anchoredPosition;
        pos.x = x;
        marker.anchoredPosition = pos;
    }

    bool IsMarkerInZone()
    {
        if (marker == null || goodZone == null) return false;
        // Suppose que marker et goodZone partagent le même parent RectTransform
        Vector2 half = goodZone.rect.size * 0.5f;
        Vector2 center = goodZone.anchoredPosition;
        Vector2 localPos = marker.anchoredPosition;
        return Mathf.Abs(localPos.x - center.x) <= half.x && Mathf.Abs(localPos.y - center.y) <= half.y;
    }

    void Solve()
    {
        solved = true;
        Debug.Log("[TimingPuzzle] Réussi: nuages désactivés");
        if (cloudsToHide != null)
        {
            foreach (var c in cloudsToHide)
            {
                if (c != null) c.SetActive(false);
            }
        }
        if (successSource != null && successClip != null)
        {
            successSource.spatialBlend = 0f;
            successSource.PlayOneShot(successClip);
        }
    }

    void PlayFail()
    {
        if (failSource != null && failClip != null)
        {
            failSource.spatialBlend = 0f;
            failSource.PlayOneShot(failClip);
        }
    }
}
