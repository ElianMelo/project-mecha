using UnityEngine;

public class EnemyPresentation : MonoBehaviour
{
    private Renderer[] renderers;
    private MaterialPropertyBlock block;
    private float flashUntil;
    private Color flash;
    private Vector3 originalScale;
    private bool dying;
    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(); block = new MaterialPropertyBlock(); originalScale = transform.localScale;
    }
    public void Telegraph() { flash = new Color(2f, 0.35f, 0.06f); flashUntil = Time.time + 0.45f; }
    public void Hit(float damage)
    {
        flash = Color.white * 2f; flashUntil = Time.time + 0.12f;
        CombatEffect.Burst(transform.position + Vector3.up * 0.7f, new Color(1f, 0.65f, 0.22f), 14);
        CombatEffect.DamageNumber(transform.position + Vector3.up * 1.4f, damage);
    }
    public void Death() { dying = true; CombatEffect.Burst(transform.position + Vector3.up * 0.7f, new Color(1f, 0.3f, 0.15f), 26); }
    private void Update()
    {
        if (dying) transform.localScale = Vector3.Lerp(transform.localScale, originalScale * 0.05f, Time.deltaTime * 9f);
        bool active = Time.time < flashUntil;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            if (active) { block.SetColor("_BaseColor", flash); block.SetColor("_Color", flash); r.SetPropertyBlock(block); }
            else r.SetPropertyBlock(null);
        }
    }
}
