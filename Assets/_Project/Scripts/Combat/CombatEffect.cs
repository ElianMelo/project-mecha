using UnityEngine;

// Floating combat text is separate from ParticleSystem effects.
public class CombatEffect : MonoBehaviour
{
    private TMPro.TextMeshPro label;
    private CombatVfxSettings settings;
    private float expires;
    public static void DamageNumber(Vector3 position,float damage,CombatVfxSettings settings)
    {
        var go=new GameObject("Damage number"); go.transform.position=position;
        var effect=go.AddComponent<CombatEffect>(); effect.settings=settings; effect.expires=Time.time+settings.damageNumberLifetime;
        effect.label=go.AddComponent<TMPro.TextMeshPro>(); GameText.BindFont(effect.label); effect.label.text=Mathf.RoundToInt(damage).ToString();
        effect.label.fontSize=settings.damageNumberSize; effect.label.alignment=TMPro.TextAlignmentOptions.Center;
        effect.label.rectTransform.sizeDelta=new Vector2(2f,1f); effect.label.color=settings.damageNumberColor;
        if(Camera.main!=null) go.transform.rotation=Camera.main.transform.rotation;
    }
    private void Update()
    {
        if(Time.time>=expires){Destroy(gameObject);return;}
        transform.position+=Vector3.up*(settings.damageNumberSpeed*Time.deltaTime);
        var color=settings.damageNumberColor; color.a=(expires-Time.time)/settings.damageNumberLifetime; label.color=color;
    }
}
