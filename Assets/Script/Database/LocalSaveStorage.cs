using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class LocalSaveStorage
{
    private const string FileName = "save-data.json";

    [Serializable]
    private class Entry
    {
        public string key;
        public string value;
    }

    [Serializable]
    private class SaveFile
    {
        public List<Entry> entries = new List<Entry>();
    }

    private static SaveFile _saveFile;

    public static bool Has(string key) => TryGet(key, out _);

    public static bool TryGet(string key, out string value)
    {
        EnsureLoaded();
        foreach (var entry in _saveFile.entries)
        {
            if (entry.key != key) continue;
            value = entry.value;
            return true;
        }

        value = null;
        return false;
    }

    public static void Set(string key, string value)
    {
        EnsureLoaded();
        foreach (var entry in _saveFile.entries)
        {
            if (entry.key != key) continue;
            entry.value = value;
            Write();
            return;
        }

        _saveFile.entries.Add(new Entry { key = key, value = value });
        Write();
    }

    private static void EnsureLoaded()
    {
        if (_saveFile != null) return;

        string path = Path.Combine(Application.persistentDataPath, FileName);
        if (!File.Exists(path))
        {
            _saveFile = new SaveFile();
            return;
        }

        try
        {
            _saveFile = JsonUtility.FromJson<SaveFile>(File.ReadAllText(path)) ?? new SaveFile();
            if (_saveFile.entries == null) _saveFile.entries = new List<Entry>();
        }
        catch (Exception error)
        {
            Debug.LogError($"[LocalSave] Could not read save file: {error.Message}");
            _saveFile = new SaveFile();
        }
    }

    private static void Write()
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        string path = Path.Combine(Application.persistentDataPath, FileName);
        File.WriteAllText(path, JsonUtility.ToJson(_saveFile, true));
    }
}