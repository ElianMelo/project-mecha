using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Presentation/Animation")]
public class CharacterAnimationSettings : ScriptableObject
{
    [Serializable] public struct StateClip { public string state; public AnimationClip clip; }
    public RuntimeAnimatorController controller;
    public StateClip[] clips;
    public float crossFade = 0.035f, movementThreshold = 0.25f, hitPoseTime = 0.2f;
    [Min(0f)] public float meleeCrossFade = 0.012f;
    [Header("Locomotion playback (world units per second at 1x)")]
    [Min(0.01f)] public float runReferenceSpeed = 4.2f, dashReferenceSpeed = 10.5f;
    [Min(0.01f)] public float minimumLocomotionPlayback = 0.2f, maximumLocomotionPlayback = 2.5f;
    [Min(0f)] public float locomotionDamping = 0.05f;
    public AnimationClip Clip(string state)
    {
        foreach (var item in clips) if (item.state == state) return item.clip;
        return null;
    }
}
