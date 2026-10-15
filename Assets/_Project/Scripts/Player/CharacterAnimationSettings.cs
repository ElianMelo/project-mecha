using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Presentation/Animation")]
public class CharacterAnimationSettings : ScriptableObject
{
    [Serializable] public struct StateClip { public string state; public AnimationClip clip; }
    public RuntimeAnimatorController controller;
    public StateClip[] clips;
    public float crossFade = 0.035f, movementThreshold = 0.25f, hitPoseTime = 0.2f;
    public AnimationClip Clip(string state)
    {
        foreach (var item in clips) if (item.state == state) return item.clip;
        return null;
    }
}
