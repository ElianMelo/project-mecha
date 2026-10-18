using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class AudioConfigurationMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private UnityEngine.UI.Slider[] sliders;
    [SerializeField] private TMP_Text[] readouts;
    [SerializeField] private UnityEngine.UI.Button close, defaults;
    private readonly List<CanvasGroup> suspended = new List<CanvasGroup>();
    private readonly List<bool> interaction = new List<bool>(), raycasts = new List<bool>();
    private GameObject previousSelection;
    public bool IsVisible => panel != null && panel.activeSelf;
    private void Awake()
    {
        for (int i = 0; i < sliders.Length; i++)
        {
            int index = i;
            sliders[i].onValueChanged.AddListener(value => Change(index, value));
        }
        close.onClick.AddListener(() => GameOptionsMenu.Instance?.Back()); defaults.onClick.AddListener(ResetVolumes);
    }
    public void Show()
    {
        if (IsVisible || GameAudio.Instance == null) return;
        previousSelection = EventSystem.current?.currentSelectedGameObject;
        suspended.Clear(); interaction.Clear(); raycasts.Clear();
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.transform == transform || canvas.transform.IsChildOf(transform) || canvas.isRootCanvas == false) continue;
            var group = canvas.GetComponent<CanvasGroup>();
            if (group == null) continue;
            suspended.Add(group); interaction.Add(group.interactable); raycasts.Add(group.blocksRaycasts);
            group.interactable = false; group.blocksRaycasts = false;
        }
        Refresh(); panel.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(sliders[0].gameObject);
        GameAudio.Play(GameAudioCue.UiOpen);
    }
    public void Hide()
    {
        if (!IsVisible) return;
        panel.SetActive(false); RestoreInteraction();
        GameAudio.Instance?.SaveVolumes(); GameManager.Instance?.SuppressGameplayInput();
        EventSystem.current?.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
        GameAudio.Play(GameAudioCue.UiClose);
    }
    private void RestoreInteraction()
    {
        for (int i = 0; i < suspended.Count; i++) if (suspended[i] != null)
        { suspended[i].interactable = interaction[i]; suspended[i].blocksRaycasts = raycasts[i]; }
        suspended.Clear(); interaction.Clear(); raycasts.Clear();
    }
    private void OnDisable() { if (IsVisible) { panel.SetActive(false); RestoreInteraction(); GameAudio.Instance?.SaveVolumes(); } }
    private void Change(int index, float value)
    {
        GameAudio.Instance?.SetVolume((GameAudioBus)index, value);
        readouts[index].text = Mathf.RoundToInt(value * 100f) + "%";
    }
    private void Refresh()
    {
        for (int i = 0; i < sliders.Length; i++)
        {
            float value = GameAudio.Instance.Volume((GameAudioBus)i);
            sliders[i].SetValueWithoutNotify(value); readouts[i].text = Mathf.RoundToInt(value * 100f) + "%";
        }
    }
    private void ResetVolumes() { GameAudio.Instance.RestoreDefaults(); Refresh(); }
}
