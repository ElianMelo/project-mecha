using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/World/Parallax Scene")]
public sealed class ParallaxSceneSettings : ScriptableObject
{
    [Serializable] public sealed class ImageLayer
    {
        public string name;
        public Sprite sprite;
        public ParallaxSettings motion;
        public Vector3 position;
        public Vector2 size;
        public Color tint = Color.white;
        public int sortingOrder;
        [Min(0), Tooltip("Additional image copies on each side, for full-room coverage.")]
        public int sideCopies = 1;
    }
    public ImageLayer[] layers;
}
