using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Simple manager de progression des niveaux basé sur PlayerPrefs.
// Fournit les méthodes statiques utilisées par le menu de sélection.
public static class LevelProgressManager
{
    private const string PREF_KEY = "LevelProgressManager_Unlocked";
    private const string SAVE_DIRECTORY_NAME = "saves";
    private const string SAVE_FILE_NAME = "level_progress.json";
    private const int MAX_SLOTS = 3;
    private const int DEFAULT_SLOT = 1; // slots sont 1..3 pour correspondre au visuel du menu

    private static HashSet<string> unlocked = new HashSet<string>();
    private static int currentSlot = DEFAULT_SLOT;

    private static string SaveDirectoryPath => Path.Combine(Application.persistentDataPath, SAVE_DIRECTORY_NAME);
    private static string SaveFilePath => Path.Combine(SaveDirectoryPath, SAVE_FILE_NAME);

    [System.Serializable]
    private class SaveFile
    {
        public int activeSlot = DEFAULT_SLOT;
        public SlotData[] slots;
    }

    [System.Serializable]
    private class SlotData
    {
        public int slotNumber;
        public string displayName; // pour montrer dans le JSON quel slot c'est
        public string lastWriteUtc; // ISO8601 pour "visuel" dans le fichier
        public string[] unlocked;
    }

    public static void Load()
    {
        unlocked.Clear();
        if (File.Exists(SaveFilePath))
        {
            try
            {
                string json = File.ReadAllText(SaveFilePath);
                PopulateFromJson(json);
                return;
            }
            catch
            {
                unlocked.Clear();
            }
        }

        // Fallback: migration depuis l'ancienne sauvegarde PlayerPrefs si présente
        if (PlayerPrefs.HasKey(PREF_KEY))
        {
            string json = PlayerPrefs.GetString(PREF_KEY);
            PopulateFromOldPrefs(json);
            Save();
        }
        else
        {
            // Si aucune donnée, initialise 3 slots vides et écrit le fichier dès le premier Save()
            EnsureSlotsExist(null);
            Save();
        }
    }

    private static void Save()
    {
        var saveFile = LoadOrCreateSaveFile();

        // Met à jour le slot actif avec l'état courant
        var slot = GetOrCreateSlot(saveFile, currentSlot);
        slot.unlocked = new string[unlocked.Count];
        unlocked.CopyTo(slot.unlocked);
        slot.lastWriteUtc = System.DateTime.UtcNow.ToString("o");

        saveFile.activeSlot = currentSlot;

        string json = JsonUtility.ToJson(saveFile, true);

        try
        {
            if (!Directory.Exists(SaveDirectoryPath))
            {
                Directory.CreateDirectory(SaveDirectoryPath);
            }

            File.WriteAllText(SaveFilePath, json);
        }
        catch
        {
            // Ignore IO errors to avoid crashing the game on write failure
        }
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

    // Verrouille tout et ne laisse qu'une scène déverrouillée (utile pour les tests)
    public static void LockAllExcept(string sceneName)
    {
        unlocked.Clear();
        if (!string.IsNullOrEmpty(sceneName))
        {
            unlocked.Add(sceneName);
        }
        Save();
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

    public static void SetActiveSlot(int slotNumber)
    {
        int clamped = Mathf.Clamp(slotNumber, 1, MAX_SLOTS);
        if (clamped == currentSlot) return;

        currentSlot = clamped;

        // Recharge les données du slot sélectionné
        var saveFile = LoadOrCreateSaveFile();
        var slot = GetOrCreateSlot(saveFile, currentSlot);

        unlocked.Clear();
        if (slot.unlocked != null)
        {
            foreach (var s in slot.unlocked)
            {
                if (!string.IsNullOrEmpty(s)) unlocked.Add(s);
            }
        }

        // Sauvegarde l'info d'activeSlot pour que le JSON reflète le slot courant
        Save();
    }

    private static SaveFile LoadOrCreateSaveFile()
    {
        SaveFile data = null;

        if (File.Exists(SaveFilePath))
        {
            try
            {
                string json = File.ReadAllText(SaveFilePath);
                data = JsonUtility.FromJson<SaveFile>(json);
            }
            catch
            {
                data = null;
            }
        }

        return EnsureSlotsExist(data);
    }

    private static SaveFile EnsureSlotsExist(SaveFile data)
    {
        if (data == null)
        {
            data = new SaveFile();
        }

        if (data.slots == null || data.slots.Length != MAX_SLOTS)
        {
            var slots = new SlotData[MAX_SLOTS];
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                slots[i] = new SlotData
                {
                    slotNumber = i + 1,
                    displayName = $"Sauvegarde {i + 1}",
                    lastWriteUtc = string.Empty,
                    unlocked = new string[0]
                };
            }

            // Si on avait déjà un tableau, on recopie les existants
            if (data.slots != null)
            {
                for (int i = 0; i < Mathf.Min(data.slots.Length, MAX_SLOTS); i++)
                {
                    if (data.slots[i] != null)
                    {
                        slots[i].unlocked = data.slots[i].unlocked ?? new string[0];
                        slots[i].lastWriteUtc = data.slots[i].lastWriteUtc;
                    }
                }
            }

            data.slots = slots;
        }

        if (data.activeSlot < 1 || data.activeSlot > MAX_SLOTS)
        {
            data.activeSlot = DEFAULT_SLOT;
        }

        return data;
    }

    private static SlotData GetOrCreateSlot(SaveFile data, int slotNumber)
    {
        data = EnsureSlotsExist(data);
        int index = Mathf.Clamp(slotNumber, 1, MAX_SLOTS) - 1;
        if (data.slots[index] == null)
        {
            data.slots[index] = new SlotData
            {
                slotNumber = slotNumber,
                displayName = $"Sauvegarde {slotNumber}",
                lastWriteUtc = string.Empty,
                unlocked = new string[0]
            };
        }
        return data.slots[index];
    }

    private static void PopulateFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var data = JsonUtility.FromJson<SaveFile>(json);
            data = EnsureSlotsExist(data);
            currentSlot = data.activeSlot;

            unlocked.Clear();
            var slot = GetOrCreateSlot(data, currentSlot);
            if (slot.unlocked != null)
            {
                foreach (var s in slot.unlocked)
                {
                    if (!string.IsNullOrEmpty(s)) unlocked.Add(s);
                }
            }
        }
        catch
        {
            unlocked.Clear();
        }
    }

    // Migration simple depuis l'ancien format PlayerPrefs (un seul slot)
    private static void PopulateFromOldPrefs(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var old = JsonUtility.FromJson<OldSaveData>(json);
            if (old != null && old.unlocked != null)
            {
                unlocked.Clear();
                foreach (var s in old.unlocked)
                {
                    if (!string.IsNullOrEmpty(s)) unlocked.Add(s);
                }
            }
        }
        catch
        {
            unlocked.Clear();
        }
    }

    [System.Serializable]
    private class OldSaveData
    {
        public string[] unlocked;
    }
}
