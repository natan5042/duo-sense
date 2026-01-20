using System.Collections.Generic;
using UnityEngine;

// Simple manager de progression des niveaux basé sur PlayerPrefs.
// Fournit les méthodes statiques utilisées par le menu de sélection.
public static class LevelProgressManager
{
    private const string PREF_KEY = "LevelProgressManager_Unlocked";

    private static HashSet<string> unlocked = new HashSet<string>();

    [System.Serializable]
    private class SaveData
    {
        public string[] unlocked;
    }

    public static void Load()
    {
        unlocked.Clear();
        if (PlayerPrefs.HasKey(PREF_KEY))
        {
            string json = PlayerPrefs.GetString(PREF_KEY);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var data = JsonUtility.FromJson<SaveData>(json);
                    if (data != null && data.unlocked != null)
                    {
                        foreach (var s in data.unlocked)
                        {
                            if (!string.IsNullOrEmpty(s)) unlocked.Add(s);
                        }
                    }
                }
                catch
                {
                    // Ignore parse errors and continue with empty set
                    unlocked.Clear();
                }
            }
        }
    }

    private static void Save()
    {
        var data = new SaveData();
        data.unlocked = new string[unlocked.Count];
        unlocked.CopyTo(data.unlocked);
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(PREF_KEY, json);
        PlayerPrefs.Save();
    }

    public static bool IsUnlocked(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        return unlocked.Contains(sceneName);
    }

    // Retourne true si l'état a changé (déverrouillage)
    public static bool Unlock(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        if (unlocked.Contains(sceneName)) return false;
        unlocked.Add(sceneName);
        Save();
        return true;
    }

    public static void EnsureDefaultUnlocked(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (!unlocked.Contains(sceneName))
        {
            unlocked.Add(sceneName);
            Save();
        }
    }
}
