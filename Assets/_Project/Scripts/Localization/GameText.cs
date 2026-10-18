using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization.Components;
using TMPro;

// Values live exclusively in Unity String Tables. Code keeps stable entry references.
public static class GameText
{
    private static readonly Dictionary<string, StringTableEntry> cache = new Dictionary<string, StringTableEntry>();
    public static int Revision { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        cache.Clear(); Revision = 0;
        LocalizationSettings.SelectedLocaleChanged -= LocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += LocaleChanged;
    }

    private static void LocaleChanged(Locale locale) { cache.Clear(); Revision++; }

    public static string Get(string table, string key, params object[] arguments)
    {
        string id = table + "/" + key;
        if (!cache.TryGetValue(id, out var entry))
        {
            entry = LocalizationSettings.StringDatabase.GetTableEntry(table, key).Entry;
            if (entry == null) return LocalizationSettings.StringDatabase.GetLocalizedString(table, key, arguments);
            cache[id] = entry;
        }
        return entry.GetLocalizedString(LocalizationSettings.SelectedLocale.Identifier.CultureInfo, arguments);
    }

    public static string Get(LocalizedString reference) => reference == null || reference.IsEmpty ? string.Empty : reference.GetLocalizedString();
    public static string Term(string category, object value) => Get(category, "term." + value);

    public static void BindFont(TMP_Text text)
    {
        if (text.GetComponent<LocalizeTmpFontEvent>() != null) return;
        var font = text.gameObject.AddComponent<LocalizeTmpFontEvent>();
        font.enabled = false;
        font.OnUpdateAsset.AddListener(value => { if (text != null) text.font = value; });
        font.AssetReference = new LocalizedTmpFont { TableReference = "Fonts", TableEntryReference = "Default" };
        font.enabled = true;
    }

    // Runtime UI creation takes a table entry, never an English fallback string.
    public static LocalizeStringEvent Bind(TMP_Text text, LocalizedString reference)
    {
        BindFont(text);
        var binding = text.GetComponent<LocalizeStringEvent>();
        if (binding == null)
        {
            binding = text.gameObject.AddComponent<LocalizeStringEvent>();
            binding.OnUpdateString.AddListener(value => { if (text != null) text.text = value; });
        }
        binding.StringReference = reference;
        binding.RefreshString();
        return binding;
    }
}
