using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

// Joue une vidéo d'intro puis charge la scène suivante
public class IntroCinematic : MonoBehaviour
{
    [SerializeField] private VideoPlayer player;
    [SerializeField] private string nextSceneName = "HUB"; // Nom exact de la scène à charger après la vidéo
    [SerializeField] private bool allowSkip = true;        // Autoriser le skip avec n'importe quelle touche

    private bool loading;

    private void Awake()
    {
        if (player == null) player = GetComponent<VideoPlayer>();
    }

    private void Start()
    {
        if (player != null)
        {
            player.loopPointReached += OnVideoEnded;
            player.Play();
        }
        else
        {
            LoadNext();
        }
    }

    private void Update()
    {
        if (loading) return;
        if (allowSkip && Input.anyKeyDown)
        {
            LoadNext();
        }
    }

    private void OnVideoEnded(VideoPlayer vp)
    {
        LoadNext();
    }

    private void LoadNext()
    {
        if (loading) return;
        loading = true;

        if (player != null)
        {
            player.loopPointReached -= OnVideoEnded;
            player.Stop();
        }

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
