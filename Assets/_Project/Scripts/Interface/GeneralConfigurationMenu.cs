using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;

public sealed class GeneralConfigurationMenu : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown language;
    [SerializeField] private TMP_Dropdown selfHighlight, playersHighlight;
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
            var values = new System.Collections.Generic.List<string> { GameText.Get("Settings", "option.off"), GameText.Get("Settings", "option.on") };
            selfHighlight.ClearOptions(); selfHighlight.AddOptions(values);
            playersHighlight.ClearOptions(); playersHighlight.AddOptions(values);
            selfHighlight.onValueChanged.AddListener(value => PlayerHighlightPreferences.SetSelf(value == 1));
            playersHighlight.onValueChanged.AddListener(value => PlayerHighlightPreferences.SetPlayers(value == 1));
            initialized = true;
        }
        language.SetValueWithoutNotify(0);
        selfHighlight.SetValueWithoutNotify(PlayerHighlightPreferences.ShowSelf ? 1 : 0);
        playersHighlight.SetValueWithoutNotify(PlayerHighlightPreferences.ShowPlayers ? 1 : 0);
        language.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnUp = back, selectOnDown = selfHighlight };
        selfHighlight.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnUp = language, selectOnDown = playersHighlight };
        playersHighlight.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnUp = selfHighlight, selectOnDown = back };
        back.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnUp = playersHighlight, selectOnDown = language };
        EventSystem.current?.SetSelectedGameObject(language.gameObject);
    }
    public bool ConsumeBack()
    {
        foreach(var dropdown in new[]{language,selfHighlight,playersHighlight})
            if(dropdown.IsExpanded) { dropdown.Hide(); dropdown.Select(); return true; }
        return false;
    }
    private void OnDisable() { foreach(var dropdown in new[]{language,selfHighlight,playersHighlight}) if(dropdown != null)dropdown.Hide(); }
}
