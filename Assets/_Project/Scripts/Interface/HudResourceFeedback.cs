using TMPro;
using UnityEngine;

// Dynamic resource targets are not fixed animation curves; native Images display
// the smoothed value and loss trail, while Animator presents discrete changes.
public sealed class HudResourceFeedback : MonoBehaviour
{
    [SerializeField] private UiFeelSettings settings;
    [SerializeField] private UnityEngine.UI.Image fill, trail;
    [SerializeField] private TMP_Text valueLabel;
    [SerializeField] private Animator readout;
    [SerializeField] private bool armor;
    private float target = 1f, displayed = 1f, delayed = 1f, catchupAt;
    private bool initialized;
    private Color fillColor, textColor;
    private void Awake() { fillColor = fill.color; textColor = valueLabel.color; readout.enabled = settings.motionEnabled; }
    public void Present(float current, float maximum)
    {
        float value = Mathf.Clamp01(current / Mathf.Max(1f, maximum));
        if (!initialized) { initialized = true; target = displayed = delayed = value; }
        if (!Mathf.Approximately(value, target))
        {
            if (value < target) catchupAt = Time.unscaledTime + settings.damageTrailDelay;
            else delayed = value;
            target = value;
            if (readout.enabled) readout.SetTrigger("Pulse");
        }
        bool critical = armor && value > 0f && value <= settings.criticalHealth;
        fill.color = critical ? settings.criticalColor : fillColor;
        valueLabel.color = critical ? settings.criticalColor : textColor;
        if (readout.enabled) readout.SetBool("Low", critical);
    }
    private void Update()
    {
        if (!initialized) return;
        displayed = settings.motionEnabled ? Mathf.Lerp(displayed, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime / settings.resourceResponse)) : target;
        if (!settings.motionEnabled || delayed < displayed) delayed = displayed;
        else if (Time.unscaledTime >= catchupAt) delayed = Mathf.MoveTowards(delayed, target, settings.damageTrailSpeed * Time.unscaledDeltaTime);
        fill.fillAmount = displayed; trail.fillAmount = Mathf.Max(displayed, delayed);
    }
}
