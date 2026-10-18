using TMPro;
using UnityEngine;

// Presentation only: session membership and readiness remain owned by the service.
public sealed class LobbyPlayerCard : MonoBehaviour
{
    [SerializeField] private LobbySettings settings;
    [SerializeField] private UnityEngine.UI.Image border, background;
    [SerializeField] private TMP_Text pilotName, role, state;
    private Color targetColor;
    private string identity;
    private bool ready, initialized;
    private float punchAt = float.NegativeInfinity;

    public void Present(string id, string name, string roleText, bool isReady)
    {
        bool occupied = !string.IsNullOrEmpty(id);
        if (initialized && occupied && identity == id && !ready && isReady)
            punchAt = Time.unscaledTime;
        identity = id; ready = isReady;
        targetColor = !occupied ? settings.emptyColor : isReady ? settings.readyColor : settings.waitingColor;
        pilotName.text = occupied ? name : GameText.Get("Lobby", "slot.open");
        role.text = occupied ? roleText : GameText.Get("Lobby", "slot.invite");
        state.text = !occupied ? GameText.Get("Lobby", "slot.empty") : isReady ? GameText.Get("Lobby", "slot.ready") : GameText.Get("Lobby", "slot.waiting");
        if (!initialized) { border.color = state.color = targetColor; initialized = true; }
        background.color = Color.Lerp(settings.cardColor, targetColor, occupied ? 0.08f : 0.025f);
    }

    private void Update()
    {
        if (!initialized) return;
        float blend = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 5f / settings.transitionSeconds);
        border.color = Color.Lerp(border.color, targetColor, blend);
        state.color = border.color;
        float t = (Time.unscaledTime - punchAt) / settings.transitionSeconds;
        float punch = t < 1f ? Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * settings.readyPunch : 0f;
        transform.localScale = Vector3.one * (1f + punch);
    }
}
