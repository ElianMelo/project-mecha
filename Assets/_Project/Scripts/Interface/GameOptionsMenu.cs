using UnityEngine;
using UnityEngine.EventSystems;

// Main options owns the screen flow; each settings section owns its own values.
public sealed class GameOptionsMenu : MonoBehaviour
{
    public static GameOptionsMenu Instance { get; private set; }
    [SerializeField] private GameObject home, video, general;
    [SerializeField] private MechaLobbyPanel lobbyPanel;
    [SerializeField] private MechaLobbyPanel pvpPanel;
    [SerializeField] private UnityEngine.UI.Button pvpButton;
    public bool LobbyVisible => lobbyPanel.IsVisible || pvpPanel != null && pvpPanel.IsVisible;
    [SerializeField] private GeneralConfigurationMenu generalSettings;
    [SerializeField] private AudioConfigurationMenu audio;
    [SerializeField] private VideoConfigurationMenu videoSettings;
    [SerializeField] private ControlsConfigurationMenu controls;
    [SerializeField] private UnityEngine.UI.Button controlsButton;
    [SerializeField] private UnityEngine.UI.Button swapMecha;
    [SerializeField] private UnityEngine.UI.Button resume, lobby, audioButton, videoButton, videoBack, generalButton, generalBack;
    private GameObject previousSelection;
    private UnityEngine.UI.Selectable returnSelection;
    public bool IsVisible => home.activeSelf || video.activeSelf || general.activeSelf || audio.IsVisible || LobbyVisible || controls != null && controls.IsVisible;
    private void Awake()
    {
        Instance = this;
        videoSettings.Initialize();
        resume.onClick.AddListener(Close);
        lobby.onClick.AddListener(OpenLobby);
        if (pvpButton != null) pvpButton.onClick.AddListener(OpenPvpLobby);
        audioButton.onClick.AddListener(() => { returnSelection = audioButton; home.SetActive(false); audio.Show(); });
        videoButton.onClick.AddListener(() => { returnSelection = videoButton; home.SetActive(false); video.SetActive(true); videoSettings.Refresh(); });
        videoBack.onClick.AddListener(Back);
        generalButton.onClick.AddListener(() => { returnSelection = generalButton; home.SetActive(false); general.SetActive(true); generalSettings.Refresh(); });
        generalBack.onClick.AddListener(Back);
        controlsButton.onClick.AddListener(() => { returnSelection = controlsButton; home.SetActive(false); controls.Show(); });
        if (swapMecha != null) swapMecha.onClick.AddListener(OpenMechaSelection);
    }
    private bool CanSwapMecha()
    {
        var network = GameManager.Instance.Network;
        var service = FindFirstObjectByType<MechaLobbyService>();
        return !network.ClientManager.Started && !network.ServerManager.Started && service?.Session == null && service?.Busy != true;
    }
    private void Update()
    {
        if (!home.activeSelf || swapMecha == null) return;
        swapMecha.interactable = CanSwapMecha();
        if (!swapMecha.interactable && EventSystem.current?.currentSelectedGameObject == swapMecha.gameObject)
            EventSystem.current.SetSelectedGameObject(resume.gameObject);
    }
    public void OpenMechaSelection()
    {
        if (!CanSwapMecha() || CharacterSelectionMenu.Instance == null || CharacterSelectionMenu.Instance.IsVisible) return;
        returnSelection = swapMecha; Close();
        CharacterSelectionMenu.Instance.Open(ShowHome);
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
        pvpPanel?.Hide();
        controls.Hide(); audio.Hide(); video.SetActive(false); general.SetActive(false); lobbyPanel.Hide(); home.SetActive(true);
        if (swapMecha != null) swapMecha.interactable = CanSwapMecha();
        EventSystem.current?.SetSelectedGameObject((returnSelection != null && returnSelection.IsInteractable() ? returnSelection : resume).gameObject);
    }
    public void Back()
    {
        if (controls.IsVisible && controls.ConsumeBack()) return;
        if (video.activeSelf && videoSettings.ConsumeBack()) return;
        if (general.activeSelf && generalSettings.ConsumeBack()) return;
        if (lobbyPanel.IsVisible && lobbyPanel.ConsumeBack()) return;
        if (pvpPanel != null && pvpPanel.IsVisible && pvpPanel.ConsumeBack()) return;
        if (controls.IsVisible || audio.IsVisible || video.activeSelf || general.activeSelf || LobbyVisible) { ShowHome(); GameAudio.Play(GameAudioCue.UiCancel); }
        else Close();
    }
    public void Close()
    {
        bool visible = IsVisible;
        pvpPanel?.Hide();
        controls.Hide(); audio.Hide(); video.SetActive(false); general.SetActive(false); lobbyPanel.Hide(); home.SetActive(false);
        if (!visible) return;
        GameManager.Instance?.SuppressGameplayInput();
        EventSystem.current?.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
        GameAudio.Play(GameAudioCue.UiClose);
    }
    public void OpenLobby()
    {
        if (FindFirstObjectByType<MechaLobbyService>()?.PvpLobby == true && (FindFirstObjectByType<MechaLobbyService>()?.Session != null || GameManager.Instance.Network.ClientManager.Started)) { OpenPvpLobby(); return; }
        OpenLobbySelected();
    }
    private void OpenLobbySelected()
    {
        pvpPanel?.Hide();
        if (!IsVisible) previousSelection = EventSystem.current?.currentSelectedGameObject;
        returnSelection = lobby;
        controls.Hide(); audio.Hide(); video.SetActive(false); general.SetActive(false); home.SetActive(false);
        lobbyPanel.Show();
    }
    public void OpenPvpLobby()
    {
        var service = FindFirstObjectByType<MechaLobbyService>();
        if (service != null && !service.PvpLobby && (service.Session != null || GameManager.Instance.Network.ClientManager.Started)) { OpenLobbySelected(); return; }
        OpenPvpSelected();
    }
    private void OpenPvpSelected()
    {
        if (!IsVisible) previousSelection = EventSystem.current?.currentSelectedGameObject;
        returnSelection = pvpButton;
        lobbyPanel.Hide(); controls.Hide(); audio.Hide(); video.SetActive(false); general.SetActive(false); home.SetActive(false);
        pvpPanel.Show();
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
