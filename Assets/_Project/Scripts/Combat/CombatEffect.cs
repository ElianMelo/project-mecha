using UnityEngine;

// Short-lived procedural effects use original geometry and shared materials.
public class CombatEffect : MonoBehaviour
{
    private static Material sharedMaterial;
    private LineRenderer line;
    private TMPro.TextMeshPro text;
    private float start, lifetime;
    private Color color;
    private Vector3 motion;
    private float expansion;
    public static Material Material
    {
        get
        {
            if (sharedMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                sharedMaterial = new Material(shader) { name = "Mecha energy (runtime)", hideFlags = HideFlags.HideAndDontSave };
                sharedMaterial.SetFloat("_Surface", 1f);
                sharedMaterial.SetFloat("_Blend", 2f);
                sharedMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                sharedMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                sharedMaterial.SetInt("_ZWrite", 0);
                sharedMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                sharedMaterial.renderQueue = 3000;
            }
            return sharedMaterial;
        }
    }
    public static void Arc(Vector3 position, int facing, float radius, float from, float to, Color tint, float width = 0.12f, Vector3 velocity = default)
    {
        var go = new GameObject("Sword energy arc"); go.transform.position = position;
        var effect = go.AddComponent<CombatEffect>();
        effect.start = Time.time; effect.lifetime = 0.28f; effect.color = tint; effect.motion = velocity; effect.expansion = 0.3f;
        effect.line = go.AddComponent<LineRenderer>();
        effect.line.sharedMaterial = Material; effect.line.useWorldSpace = false;
        effect.line.positionCount = 30; effect.line.widthMultiplier = width;
        effect.line.numCapVertices = 4; effect.line.startColor = effect.line.endColor = tint;
        for (int i = 0; i < 30; i++)
        {
            float angle = Mathf.Lerp(from, to, i / 29f) * Mathf.Deg2Rad;
            effect.line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius * facing, Mathf.Sin(angle) * radius, 0f));
        }
    }
    public static void Burst(Vector3 position, Color tint, int amount)
    {
        var go = new GameObject("Impact sparks"); go.transform.position = position;
        var particles = go.AddComponent<ParticleSystem>(); particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main; main.duration = 0.4f; main.loop = false; main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f); main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startColor = tint; main.gravityModifier = 0.25f; main.maxParticles = 48;
        var emission = particles.emission; emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)amount) });
        var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.1f;
        var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = Material;
        particles.Play(); Destroy(go, 0.9f);
    }
    public static void DamageNumber(Vector3 position, float damage)
    {
        var go = new GameObject("Damage"); go.transform.position = position + Vector3.back * 0.4f;
        var effect = go.AddComponent<CombatEffect>(); effect.start = Time.time; effect.lifetime = 0.65f; effect.motion = Vector3.up * 1.2f;
        effect.color = new Color(1f, 0.85f, 0.35f); effect.text = go.AddComponent<TMPro.TextMeshPro>();
        effect.text.text = Mathf.RoundToInt(damage).ToString(); effect.text.fontSize = 4f;
        effect.text.alignment = TMPro.TextAlignmentOptions.Center; effect.text.rectTransform.sizeDelta = new Vector2(2f, 1f);
        effect.text.color = effect.color;
        if (Camera.main != null) go.transform.rotation = Camera.main.transform.rotation;
    }
    private void Update()
    {
        float t = (Time.time - start) / lifetime;
        if (t >= 1f) { Destroy(gameObject); return; }
        transform.position += motion * Time.deltaTime;
        transform.localScale = Vector3.one * (1f + expansion * t);
        Color faded = color; faded.a = 1f - t;
        if (line != null) line.startColor = line.endColor = faded;
        if (text != null) text.color = faded;
    }
}
