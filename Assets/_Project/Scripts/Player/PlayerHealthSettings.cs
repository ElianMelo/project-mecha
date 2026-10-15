using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Player/Health")]
public class PlayerHealthSettings : ScriptableObject
{
    public float maxHealth = 100f, invincibility = 0.9f, hitStun = 0.3f, respawnInvincibility = 2f, fallLimit = -12f;
    public float defeatDuration = 1.1f;
}
