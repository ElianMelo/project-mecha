using UnityEngine;

public sealed class LevelPortal : MonoBehaviour
{
    private ProceduralLevelDirector director;
    private LineRenderer ring;
    public void Configure(ProceduralLevelDirector owner)
    {
        director = owner;
        var visual = new GameObject("Portal Ring"); visual.transform.SetParent(transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.95f, -0.2f);
        ring = visual.AddComponent<LineRenderer>(); ring.useWorldSpace = false; ring.loop = true;
        ring.positionCount = 64; ring.widthMultiplier = 0.09f; ring.sharedMaterial = director.Settings.portalMaterial;
        for (int i = 0; i < 64; i++) { float a = i * Mathf.PI * 2f / 64; ring.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.55f, Mathf.Sin(a) * 0.85f, 0f)); }
    }
    private void Update()
    {
        if (ring == null) return;
        Color color = director.IsClear ? director.Settings.portalColor : new Color(0.24f, 0.3f, 0.4f, 0.65f);
        ring.startColor = ring.endColor = color;
        ring.transform.localScale = Vector3.one * (director.IsClear ? 1f + 0.04f * Mathf.Sin(Time.time * 3f) : 0.8f);
    }
}
