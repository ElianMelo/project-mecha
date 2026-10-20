using UnityEngine;

public class EnemyPresentation : MonoBehaviour
{
    [SerializeField] private EnemySettings settings;
    private IHitFeedback feedback;
    private CharacterHitFeedback flash;
    private CharacterAnimator animationView;
    private CombatReaction reactions;
    private void Awake() { feedback=GetComponent<IHitFeedback>(); flash=GetComponent<CharacterHitFeedback>(); animationView=GetComponent<CharacterAnimator>(); reactions=GetComponent<CombatReaction>(); }
    public void Telegraph() { flash?.Telegraph(); animationView?.Telegraph(settings.windup); }
    public void Telegraph(float duration, bool special = false)
    {
        if (special) flash?.Telegraph(duration); else flash?.Telegraph();
        animationView?.Telegraph(duration, special);
    }
    public void Hit(float damage,Vector3 direction,float stun,DamageContact contact = default) { feedback?.Hit(damage,direction,contact); if(reactions!=null)reactions.Hit(direction,stun); else animationView?.Reaction(stun); }
    public void Attack() => GameAudio.Play(GameAudioCue.EnemyAttack, transform.position, transform);
    public void Death()
    {
        GameAudio.Play(GameAudioCue.EnemyDeath, transform.position);
        if (reactions != null) reactions.Die(settings.deathDuration); else animationView?.Death(settings.deathDuration);
    }
}
