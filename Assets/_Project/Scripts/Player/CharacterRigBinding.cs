using UnityEngine;

// Explicit sockets keep rig/hierarchy knowledge out of gameplay systems.
public sealed class CharacterRigBinding : MonoBehaviour
{
    public Animator animator;
    public Transform skillHand, leftArm, gun;
    public MeleeBlade blade;
}
