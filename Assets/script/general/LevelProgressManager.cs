using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Simple JSON persistence for unlocked levels (scene names).
public static class LevelProgressManager
{
    [Serializable]
    private class LevelProgressData
    {
        public string version = "1.0";
        public List<string> unlockedScenes = new List<string>();
    }

    private static readonly string FileName = "level_progress.json";
    private static HashSet<string> _unlocked = new HashSet<string>();
    private static bool _loaded = false;

    // Stocke la sauvegarde directement dans le dossier du projet (workspace).
    // Fallback sur persistentDataPath si le chemin n'est pas résolvable.
    private static string SavePath
    {
        get
        {
            try
            {
                // Application.dataPath pointe sur <project>/Assets en éditeur
                string dataPath = Application.dataPath;
                string projectRoot = Directory.GetParent(dataPath)?.FullName;
                if (!string.IsNullOrEmpty(projectRoot))
                {
                    return Path.Combine(projectRoot, FileName);
                }
            }
            catch
            {
                // ignore et utilise le fallback
            }

            return Path.Combine(Application.persistentDataPath, FileName);
        }
    }

    public static void Load()
    {
        if (_loaded) return;

        try
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<LevelProgressData>(json);
                _unlocked = data != null && data.unlockedScenes != null
                    ? new HashSet<string>(data.unlockedScenes)
                    : new HashSet<string>();
            }
            else
            {
                _unlocked = new HashSet<string>();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"LevelProgressManager Load failed: {ex.Message}");
            _unlocked = new HashSet<string>();
        }

        _loaded = true;
    }

    public static void EnsureDefaultUnlocked(string firstScene)
    {
        if (string.IsNullOrEmpty(firstScene)) return;
        Load();
        bool changed = false;
        if (_unlocked.Count == 0)
        {
            _unlocked.Add(firstScene);
            changed = true;
        }
        else if (!_unlocked.Contains(firstScene))
        {
            _unlocked.Add(firstScene);
            changed = true;
        }

        if (changed) Save();
    }

    public static bool IsUnlocked(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        Load();
        return _unlocked.Contains(sceneName);
    }

    public static bool Unlock(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        Load();
        bool added = _unlocked.Add(sceneName);
        if (added) Save();
        return added;
    }

    public static void ResetProgress()
    {
        _unlocked = new HashSet<string>();
        Save();
    }

    private static void Save()
    {
        try
        {
            var data = new LevelProgressData
            {
                unlockedScenes = new List<string>(_unlocked)
            };

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"LevelProgressManager Save failed: {ex.Message}");
        }
    }
}
