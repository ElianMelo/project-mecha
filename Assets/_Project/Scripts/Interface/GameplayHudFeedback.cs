using UnityEngine;

public sealed class GameplayHudFeedback : MonoBehaviour
{
    [SerializeField] private UiFeelSettings settings;
    [SerializeField] private HudResourceFeedback armor, energy;
    [SerializeField] private Animator combo;
    [SerializeField] private Animator[] skillReadouts;
    [SerializeField] private UnityEngine.UI.Image[] skillMeters;
    private float[] durations;
    private int[] previousStates;
    private int previousHits;
    private void Awake()
    {
        durations = new float[skillMeters.Length]; previousStates = new int[skillMeters.Length];
        for (int i = 0; i < previousStates.Length; i++) previousStates[i] = -1;
        combo.enabled = settings.motionEnabled;
        foreach (var readout in skillReadouts) readout.enabled = settings.motionEnabled;
    }
    public void Resources(float hp, float maxHp, float mana, float maxMana)
    { armor.Present(hp, maxHp); energy.Present(mana, maxMana); }
    public void Combo(bool visible, int hits)
    {
        if (combo.enabled)
        {
            combo.SetBool("Visible", visible);
            if (visible && hits != previousHits) combo.SetTrigger("Pulse");
        }
        else combo.GetComponent<CanvasGroup>().alpha = visible ? 1f : 0f;
        previousHits = visible ? hits : 0;
    }
    public void Skill(int index, float cooldown, bool affordable)
    {
        int state = cooldown > 0f ? 1 : affordable ? 0 : 2;
        durations[index] = cooldown > 0f ? Mathf.Max(durations[index], cooldown) : 0f;
        skillMeters[index].fillAmount = cooldown > 0f ? 1f - cooldown / Mathf.Max(0.01f, durations[index]) : 1f;
        skillMeters[index].color = state == 0 ? settings.focusColor : state == 2 ? settings.criticalColor : settings.energyTrailColor;
        if (previousStates[index] >= 0 && previousStates[index] != state && state != 2 && skillReadouts[index].enabled)
            skillReadouts[index].SetTrigger("Pulse");
        previousStates[index] = state;
    }
}
