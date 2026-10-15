using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/World/Parallax")]
public class ParallaxSettings : ScriptableObject
{
    public Vector2 movement = new Vector2(0.12f, 0.06f);
}
