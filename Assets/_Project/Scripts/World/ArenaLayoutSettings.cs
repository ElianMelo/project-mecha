using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/World/Arena Layout")]
public class ArenaLayoutSettings : ScriptableObject
{
    [Serializable] public struct PlatformData
    {
        public string name;
        public Vector2 position;
        public float width;
    }
    public Vector2 horizontalLimits = new Vector2(-36f, 36f);
    public Vector2 cameraVerticalLimits = new Vector2(-5.6f, 8.8f);
    public float floorHeight = -3.5f, floorThickness = 0.5f, floorDepth = 3f;
    public float platformThickness = 0.22f, platformDepth = 2.4f, ceilingHeight = 8.8f;
    public Vector3 playerStart = new Vector3(-27f, -3.35f, 0f);
    public PlatformData[] platforms;
    public Vector3[] enemySpawns;
}
