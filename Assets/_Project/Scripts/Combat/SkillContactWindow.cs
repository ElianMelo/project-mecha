using System.Collections.Generic;
using UnityEngine;

// Runtime contact state only. Authored envelopes remain in the AttackPiece asset.
internal sealed class SkillContactWindow
{
    public int piece, pulse, facing;
    public float started, expires;
    public Vector3 previousOrigin;
    public readonly HashSet<IDamageable> targets = new HashSet<IDamageable>();
}
