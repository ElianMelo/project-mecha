using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

// Device-local JSON storage. Callers own when a group of changes is committed.
public static class GameSave
{
    public const int CurrentVersion = 1;
    public static string SavePath => Path.Combine(Application.persistentDataPath, "Settings.json");
    public static string BackupPath => SavePath + ".bak";
    public static string LastError { get; private set; }
    private static readonly Regex Segment = new Regex(@"^[A-Z][a-z0-9]*(?:[A-Z][a-z0-9]+)*$");
    private static JObject data;
    private static bool dirty, writesBlocked, primaryValid;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        data = null; dirty = false; writesBlocked = false; primaryValid = false; LastError = null;
        Application.quitting -= OnQuit; Application.quitting += OnQuit;
    }
    private static void OnQuit() { if (data != null) Save(); }

    public static bool HasKey(string key) => Find(key) != null;
    public static int GetInt(string key, int fallback = 0)
    {
        var value = Find(key);
        if (value?.Type != JTokenType.Integer) return fallback;
        try { return value.Value<int>(); } catch (Exception error) when (IsNumberError(error)) { return fallback; }
    }
    public static float GetFloat(string key, float fallback = 0f)
    {
        var value = Find(key);
        if (value?.Type != JTokenType.Float && value?.Type != JTokenType.Integer) return fallback;
        try
        {
            float number = value.Value<float>();
            return float.IsNaN(number) || float.IsInfinity(number) ? fallback : number;
        }
        catch (Exception error) when (IsNumberError(error)) { return fallback; }
    }
    public static bool GetBool(string key, bool fallback = false)
    {
        var value = Find(key); return value?.Type == JTokenType.Boolean ? value.Value<bool>() : fallback;
    }
    public static string GetString(string key, string fallback = "")
    {
        var value = Find(key); return value?.Type == JTokenType.String ? value.Value<string>() : fallback;
    }
    public static string GetJson(string key)
    {
        var value = Find(key); return value is JObject || value is JArray ? value.ToString(Formatting.None) : null;
    }
    public static void SetInt(string key, int value) => Set(key, new JValue(value));
    public static void SetFloat(string key, float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value));
        Set(key, new JValue(value));
    }
    public static void SetBool(string key, bool value) => Set(key, new JValue(value));
    public static void SetString(string key, string value) => Set(key, new JValue(value ?? ""));
    public static void SetJson(string key, string json)
    {
        var value = JToken.Parse(json);
        if (!(value is JObject) && !(value is JArray)) throw new ArgumentException("Save JSON must be an object or array.", nameof(json));
        Set(key, value);
    }
    private static string[] Parts(string key)
    {
        var parts = (key ?? "").Split('.');
        if (parts.Length < 3 || parts[0] != "Mecha" || Array.Exists(parts, part => !Segment.IsMatch(part)))
            throw new ArgumentException("Save keys must use Mecha.Category.Name with PascalCase words, for example Mecha.Controls.Overrides.", nameof(key));
        return parts;
    }
    private static JToken Find(string key)
    {
        var parts = Parts(key); EnsureLoaded(); JToken value = data;
        foreach (string part in parts) { value = (value as JObject)?[part]; if (value == null) break; }
        return value;
    }
    private static void Set(string key, JToken value)
    {
        var parts = Parts(key); EnsureLoaded(); var parent = data;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (!(parent[parts[i]] is JObject child)) { child = new JObject(); parent[parts[i]] = child; }
            parent = child;
        }
        string name = parts[parts.Length - 1];
        if (JToken.DeepEquals(parent[name], value)) return;
        parent[name] = value; dirty = true;
    }
    private static void EnsureLoaded()
    {
        if (data != null) return;
        data = new JObject { ["Version"] = CurrentVersion, ["Mecha"] = new JObject() };
        try
        {
            if (File.Exists(SavePath))
            {
                try { data = Read(SavePath); primaryValid = true; }
                catch (JsonException error)
                {
                    Debug.LogWarning("Settings JSON is invalid; trying its backup. " + error.Message);
                    // Preserve the original for recovery before replacing it with recovered/default data.
                    File.Copy(SavePath, SavePath + ".Corrupt." + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
                    data = File.Exists(BackupPath) ? Read(BackupPath) : data; dirty = true;
                }
            }
            else if (File.Exists(BackupPath)) { data = Read(BackupPath); dirty = true; }
        }
        catch (Exception error) when (IsStorageError(error) || error is JsonException || error is NotSupportedException)
        {
            // Never overwrite unreadable files or downgrade a newer save format.
            writesBlocked = true; Report(error);
        }
        if (!writesBlocked)
        {
            LegacySettingsMigration.Import();
            if (dirty) Save();
        }
    }
    private static JObject Read(string path)
    {
        var document = JObject.Parse(File.ReadAllText(path));
        var version = document["Version"];
        if (version?.Type != JTokenType.Integer) throw new JsonException("Settings Version is missing or invalid.");
        if (!int.TryParse(version.ToString(), out int number) || number > CurrentVersion) throw new NotSupportedException("Settings were written by a newer game version.");
        if (number != CurrentVersion || !(document["Mecha"] is JObject)) throw new JsonException("Settings structure is invalid.");
        return document;
    }
    public static bool Save()
    {
        EnsureLoaded();
        if (writesBlocked) return false;
        if (!dirty) return true;
        string temporary = SavePath + ".tmp";
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            byte[] bytes = new UTF8Encoding(false).GetBytes(data.ToString(Formatting.Indented));
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (File.Exists(SavePath)) File.Replace(temporary, SavePath, primaryValid ? BackupPath : null);
            else File.Move(temporary, SavePath);
            primaryValid = true; dirty = false; LastError = null; return true;
        }
        catch (Exception error) when (IsStorageError(error) || error is NotSupportedException)
        { Report(error); return false; }
    }
    private static bool IsStorageError(Exception error) => error is IOException || error is UnauthorizedAccessException || error is System.Security.SecurityException;
    private static bool IsNumberError(Exception error) => error is OverflowException || error is InvalidCastException || error is FormatException;
    private static void Report(Exception error) { LastError = error.Message; Debug.LogWarning("Settings could not be saved or loaded at " + SavePath + ": " + error.Message); }
}
