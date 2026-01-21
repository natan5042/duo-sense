using UnityEditor;
using UnityEngine;

// Vérifie à l'ouverture de l'éditeur si les ressources essentielles TextMeshPro sont présentes
// et affiche une instruction claire si elles manquent. N'effectue AUCUNE importation automatique
// (évite l'erreur "Cannot import package in play mode").
[InitializeOnLoad]
static class CheckTMPResources
{
    static CheckTMPResources()
    {
        EditorApplication.delayCall += RunCheck;
    }

    private static void RunCheck()
    {
        // Ne rien faire en mode Play
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        // Vérifier l'existence du type TMPro pour savoir si le package est installé
        var tmpType = System.Type.GetType("TMPro.TMP_Settings, Unity.TextMeshPro");
        if (tmpType == null)
        {
            Debug.LogWarning("TextMesh Pro package non présent (TMPro types introuvables). Si vous utilisez TMP, installez-le via Package Manager.");
            return;
        }

        // Rechercher un asset TMP_Settings dans le projet
        string[] guids = AssetDatabase.FindAssets("t:TMP_Settings");
        if (guids == null || guids.Length == 0)
        {
            Debug.LogError("TextMesh Pro Essential Resources introuvables. Pour les importer : Window > TextMesh Pro > Import TMP Essential Resources. Ne lancez PAS le Play Mode pendant l'import.");
        }
        else
        {
            // ok
        }
    }
}
