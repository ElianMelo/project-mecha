using UnityEngine;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class ObjectiveArrowGraphic : UnityEngine.UI.MaskableGraphic
{
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        var r = rectTransform.rect;
        Vector2[] points = {new Vector2(r.xMax, 0f), new Vector2(r.xMin, r.yMax),
            new Vector2(r.xMin * 0.35f, 0f), new Vector2(r.xMin, r.yMin)};
        foreach (var p in points) mesh.AddVert(p, color, Vector2.zero);
        mesh.AddTriangle(0, 1, 2); mesh.AddTriangle(0, 2, 3);
    }
}
