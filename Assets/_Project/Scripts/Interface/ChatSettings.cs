using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Interface/Chat")]
public sealed class ChatSettings : ScriptableObject
{
    [Min(1)] public int visibleMessages = 3;
    [Min(1f)] public float messageLifetime = 35f;
    [Min(0.1f)] public float sendInterval = 0.5f;
    [Range(20, 256)] public int characterLimit = 160;
    public Vector2 anchor = Vector2.one;
    public Vector2 size = new Vector2(310f, 126f), offset = new Vector2(-20f, -18f);
}
