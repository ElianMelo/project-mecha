using UnityEngine;

// Physical contact/travel and cosmetic spray are independent from gameplay knockback.
[System.Serializable]
public struct DamageContact
{
    public Vector3 Point;
    public Vector3 Incoming;
    public Vector3 SprayDirection;
    public bool IsValid => Incoming.sqrMagnitude > .000001f;
    public DamageContact(Vector3 point, Vector3 incoming, Vector3 sprayDirection = default)
    {
        Point = point;
        Incoming = incoming.sqrMagnitude > .000001f ? incoming.normalized : Vector3.zero;
        SprayDirection = sprayDirection.sqrMagnitude > .000001f ? sprayDirection.normalized : Incoming;
    }
}
