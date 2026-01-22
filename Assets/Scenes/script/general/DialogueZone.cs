using UnityEngine;

// Zone qui joue un clip de dialogue quand le joueur entre dans le trigger.
public class DialogueZone : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip[] dialogueClips;

    [Header("Sous-titres (même ordre que les clips)")]
    [TextArea]
    public string[] subtitles;
    public SubtitleUI subtitleUI; // si vide on tente SubtitleUI.Instance
    public bool showSubtitles = true;
    public float subtitleHoldExtra = 0.25f; // garde l'affichage un peu après la fin
    public bool fallbackToClipName = true;   // si pas de texte, utilise le nom du clip
    public string fallbackText = "";        // texte par defaut si rien d'autre

    [Header("Options")]
    public string requiredTag = "Player"; // acteur attendu
    public bool playOnce = true;           // evite les repetitions
    public bool disableColliderAfterPlay = true;

    [Header("Audio source (optionnel)")]
    public AudioSource overrideSource;     // utilise cette source sinon on prend celle de l'acteur

    bool hasPlayed;

    void Awake()
    {
        if (overrideSource != null)
        {
            overrideSource.spatialBlend = 0f; // 2D pour etre audible partout
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryPlay(other.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        TryPlay(other.gameObject);
    }

    void TryPlay(GameObject actor)
    {
        if (playOnce && hasPlayed) return;
        if (actor == null) return;
        if (!string.IsNullOrEmpty(requiredTag) && !actor.CompareTag(requiredTag)) return;

        int clipIndex;
        var clip = PickClip(out clipIndex);
        if (clip == null) return;

        var source = overrideSource;
        if (source == null)
        {
            source = actor.GetComponent<AudioSource>();
        }

        if (source != null)
        {
            if (!source.enabled) source.enabled = true;
            source.spatialBlend = 0f; // 2D pour le dialogue
        }

        bool played = false;
        if (source != null && source.enabled && source.gameObject.activeInHierarchy)
        {
            source.PlayOneShot(clip);
            played = true;
        }

        if (!played)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : transform.position);
        }

        if (showSubtitles)
        {
            var text = GetSubtitle(clipIndex, clip);
            if (!string.IsNullOrWhiteSpace(text))
            {
                var ui = subtitleUI != null ? subtitleUI : SubtitleUI.GetOrFindInstance();
                if (ui != null)
                {
                    float duration = Mathf.Max(clip.length + subtitleHoldExtra, 0.25f);
                    ui.ShowSubtitle(text, duration);
                }
                else
                {
                    Debug.LogWarning($"DialogueZone '{name}' : pas de SubtitleUI trouvé dans la scène.");
                }
            }
            else
            {
                Debug.LogWarning($"DialogueZone '{name}' : texte de sous-titre introuvable pour l'index {clipIndex}.");
            }
        }

        hasPlayed = true;

        if (disableColliderAfterPlay)
        {
            var col3D = GetComponent<Collider>();
            if (col3D != null) col3D.enabled = false;

            var col2D = GetComponent<Collider2D>();
            if (col2D != null) col2D.enabled = false;
        }
    }

    AudioClip PickClip(out int index)
    {
        index = -1;
        if (dialogueClips == null || dialogueClips.Length == 0) return null;
        if (dialogueClips.Length == 1)
        {
            index = 0;
            return dialogueClips[0];
        }
        index = Random.Range(0, dialogueClips.Length);
        return dialogueClips[index];
    }

    string GetSubtitle(int index, AudioClip clip)
    {
        // 1) tableau vide ? -> fallback
        if (subtitles == null || subtitles.Length == 0)
        {
            return GetFallbackSubtitle(clip);
        }

        // 2) index clamp
        if (index < 0) index = 0;
        if (index >= subtitles.Length)
        {
            index = subtitles.Length - 1; // evite de perdre l'affichage si moins de sous-titres que de clips
            Debug.LogWarning($"DialogueZone '{name}' : pas assez de sous-titres, on reutilise le dernier.");
        }

        var txt = subtitles[index];
        if (string.IsNullOrWhiteSpace(txt))
        {
            return GetFallbackSubtitle(clip);
        }
        return txt;
    }

    string GetFallbackSubtitle(AudioClip clip)
    {
        if (!string.IsNullOrWhiteSpace(fallbackText)) return fallbackText;
        if (fallbackToClipName && clip != null) return clip.name;
        return null;
    }
}
