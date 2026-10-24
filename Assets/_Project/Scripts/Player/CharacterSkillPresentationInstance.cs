using UnityEngine;

public abstract class CharacterSkillPresentationInstance : MonoBehaviour
{
    public abstract void Pulse(int pulse);
    public abstract void End();
}
