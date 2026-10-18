using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;

public sealed class GeneralConfigurationMenu : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown language;
    [SerializeField] private UnityEngine.UI.Button back;
    private bool initialized;

    public void Refresh()
    {
        // English is the sole approved locale. Unity Localization owns its selection.
        if (!initialized)
        {
            language.ClearOptions();
            language.AddOptions(new System.Collections.Generic.List<string> { GameText.Get("Settings", "language.english") });
            language.onValueChanged.AddListener(_ => {
                var english = LocalizationSettings.AvailableLocales.GetLocale("en");
                if (english != null) LocalizationSettings.SelectedLocale = english;
            });
            initialized = true;
        }
        language.SetValueWithoutNotify(0);
        language.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnUp = back, selectOnDown = back };
        back.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnUp = language, selectOnDown = language };
        EventSystem.current?.SetSelectedGameObject(language.gameObject);
    }
    public bool ConsumeBack()
    {
        if (!language.IsExpanded) return false;
        language.Hide(); language.Select(); return true;
    }
    private void OnDisable() { if (language != null) language.Hide(); }
}
