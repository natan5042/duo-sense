using UnityEngine;

// Zone qui joue un clip de dialogue quand le joueur entre dans le trigger.
public class DialogueZone : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip[] dialogueClips;

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

        var clip = PickClip();
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

        hasPlayed = true;

        if (disableColliderAfterPlay)
        {
            var col3D = GetComponent<Collider>();
            if (col3D != null) col3D.enabled = false;

            var col2D = GetComponent<Collider2D>();
            if (col2D != null) col2D.enabled = false;
        }
    }

    AudioClip PickClip()
    {
        if (dialogueClips == null || dialogueClips.Length == 0) return null;
        if (dialogueClips.Length == 1) return dialogueClips[0];
        int index = Random.Range(0, dialogueClips.Length);
        return dialogueClips[index];
    }
}
