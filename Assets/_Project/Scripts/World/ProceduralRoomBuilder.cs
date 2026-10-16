using Unity.Cinemachine;
using UnityEngine;

public sealed class ProceduralRoomBuilder : MonoBehaviour
{
    private GameObject generated;
    public void Build(ProceduralLevelLayout layout, ProceduralLevelSettings settings)
    {
        if (generated != null) { generated.SetActive(false); Destroy(generated); }
        // Authored room remains available in edit mode; runtime geometry is disposable.
        var ground = GameObject.Find("Ground"); if (ground != null) ground.SetActive(false);
        foreach (var platform in FindObjectsByType<OneWayPlatform>(FindObjectsSortMode.None)) platform.gameObject.SetActive(false);
        var props = FindObjectsByType<DestructibleProp>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        System.Array.Sort(props, (a, b) => string.CompareOrdinal(a.name, b.name));
        for (int i = 0; i < props.Length; i++)
            props[i].transform.position = new Vector3(Mathf.Lerp(layout.Bounds.xMin + 1f, layout.Bounds.xMax - 1f,
                (i + 1f) / (props.Length + 1f)), settings.floorHeight + settings.scenario.boxSize.y * 0.5f, settings.scenario.propDepth);
        foreach (string name in new[] {"Left", "Right", "Top"}) { var old = GameObject.Find(name); if (old != null) old.SetActive(false); }
        generated = new GameObject("Generated Room");
        float floor = settings.floorHeight;
        Surface("Room Floor", new Vector3(layout.Bounds.center.x, floor - 0.25f, 0f),
            new Vector3(layout.Bounds.width, 0.5f, 3f), false, settings.scenario.floorSprite, settings.scenario.floorFaceHeight, settings);
        foreach (var deck in layout.Platforms)
            Surface(deck.name, new Vector3(deck.position.x, deck.position.y, 0f),
                new Vector3(deck.width, settings.platformThickness, 2.4f), true, settings.scenario.floatingSprite, settings.scenario.floatingFaceHeight, settings);
        Wall("Room Left", new Vector3(layout.Bounds.xMin - 0.2f, layout.Bounds.center.y, 0f), new Vector3(0.4f, layout.Bounds.height + 8f, 6f));
        Wall("Room Right", new Vector3(layout.Bounds.xMax + 0.2f, layout.Bounds.center.y, 0f), new Vector3(0.4f, layout.Bounds.height + 8f, 6f));
        Wall("Room Ceiling", new Vector3(layout.Bounds.center.x, layout.Bounds.yMax + 0.5f, 0f), new Vector3(layout.Bounds.width, 1f, 6f));
        Physics.SyncTransforms();
        var rig = FindFirstObjectByType<RoomCameraRig>();
        if (rig != null)
        {
            rig.GetComponent<RoomCameraZoom>()?.SetRoomWidth(layout.Shape == LevelShape.Rising ? layout.Bounds.width : 0f);
            rig.SetRoom(layout.Bounds, layout.PlayerStart);
        }
        var confiner = FindFirstObjectByType<CinemachineConfiner2D>();
        if (confiner != null && confiner.BoundingShape2D is BoxCollider2D box)
        {
            box.transform.position = Vector3.zero; box.size = layout.Bounds.size; box.offset = layout.Bounds.center;
            Physics2D.SyncTransforms();
            confiner.InvalidateBoundingShapeCache(); confiner.InvalidateLensCache();
        }
    }
    private void Wall(string name, Vector3 position, Vector3 size)
    {
        var go = new GameObject(name); go.transform.SetParent(generated.transform); go.transform.position = position;
        go.AddComponent<BoxCollider>().size = size;
    }
    private void Surface(string name, Vector3 position, Vector3 size, bool oneWay, Sprite sprite, float faceHeight, ProceduralLevelSettings settings)
    {
        var go = new GameObject(name); go.SetActive(false); go.transform.SetParent(generated.transform); go.transform.position = position;
        go.layer = oneWay ? 6 : 7; go.tag = oneWay ? "Platform" : "Untagged";
        go.AddComponent<BoxCollider>().size = size;
        if (oneWay) go.AddComponent<OneWayPlatform>().Configure(settings.oneWay);
        // Tile existing art at its authored scale instead of stretching a whole floor.
        float tileWidth = Mathf.Min(size.x, settings.scenario.floorTileWidth);
        for (float left = -size.x * 0.5f; left < size.x * 0.5f - 0.01f; left += tileWidth)
        {
            float width = Mathf.Min(tileWidth, size.x * 0.5f - left);
            var face = new GameObject("Platform Face"); face.transform.SetParent(go.transform, false);
            face.transform.localPosition = new Vector3(left + width * 0.5f, size.y * 0.5f - sprite.bounds.max.y * faceHeight / sprite.bounds.size.y, -0.02f);
            face.transform.localScale = new Vector3(width / sprite.bounds.size.x, faceHeight / sprite.bounds.size.y, 1f);
            face.AddComponent<SpriteRenderer>().sprite = sprite;
        }
        go.SetActive(true);
    }
}
