using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class VfxBrowserRow : MonoBehaviour, ISelectHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private UnityEngine.UI.Button button;
    [SerializeField] private UnityEngine.UI.Image selectedMarker;
    private VfxTestingBrowser browser;
    public VfxPreviewDefinition Definition { get; private set; }
    public UnityEngine.UI.Button Button => button;

    public void Bind(VfxTestingBrowser owner, VfxPreviewDefinition definition)
    {
        browser = owner;
        Definition = definition;
        GameText.Bind(label, definition.displayName);
        button.onClick.AddListener(Confirm);
    }

    public void ShowSelected(bool selected) => selectedMarker.enabled = selected;
    public void OnSelect(BaseEventData data) => browser?.Select(this);
    private void Confirm() { browser.Select(this); browser.Replay(); }
}
