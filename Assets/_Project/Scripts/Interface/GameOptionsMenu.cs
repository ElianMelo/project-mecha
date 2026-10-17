using UnityEngine;
using UnityEngine.EventSystems;

// Main options owns the screen flow; each settings section owns its own values.
public sealed class GameOptionsMenu : MonoBehaviour
{
    public static GameOptionsMenu Instance { get; private set; }
    [SerializeField] private GameObject home, video;
    [SerializeField] private AudioConfigurationMenu audio;
    [SerializeField] private VideoConfigurationMenu videoSettings;
    [SerializeField] private UnityEngine.UI.Button resume, lobby, audioButton, videoButton, videoBack;
    private GameObject previousSelection;
    private UnityEngine.UI.Selectable returnSelection;
    public bool IsVisible => home.activeSelf || video.activeSelf || audio.IsVisible;
    private void Awake()
    {
        Instance = this;
        videoSettings.Initialize();
        resume.onClick.AddListener(Close);
        lobby.onClick.AddListener(OpenLobby);
        audioButton.onClick.AddListener(() => { returnSelection = audioButton; home.SetActive(false); audio.Show(); });
        videoButton.onClick.AddListener(() => { returnSelection = videoButton; home.SetActive(false); video.SetActive(true); videoSettings.Refresh(); });
        videoBack.onClick.AddListener(Back);
    }
    public void Open()
    {
        if (IsVisible) return;
        previousSelection = EventSystem.current?.currentSelectedGameObject;
        returnSelection = resume; ShowHome();
        GameAudio.Play(GameAudioCue.UiOpen);
    }
    public void ShowHome()
    {
        audio.Hide(); video.SetActive(false); home.SetActive(true);
        EventSystem.current?.SetSelectedGameObject((returnSelection != null ? returnSelection : resume).gameObject);
    }
    public void Back()
    {
        if (video.activeSelf && videoSettings.ConsumeBack()) return;
        if (audio.IsVisible || video.activeSelf) { ShowHome(); GameAudio.Play(GameAudioCue.UiCancel); }
        else Close();
    }
    public void Close()
    {
        bool visible = IsVisible;
        audio.Hide(); video.SetActive(false); home.SetActive(false);
        if (!visible) return;
        GameManager.Instance?.SuppressGameplayInput();
        EventSystem.current?.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
        GameAudio.Play(GameAudioCue.UiClose);
    }
    public void OpenLobby() { Close(); GameManager.Instance?.ShowLobby(); }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
