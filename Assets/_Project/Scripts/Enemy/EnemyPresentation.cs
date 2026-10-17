using UnityEngine;

public class EnemyPresentation : MonoBehaviour
{
    [SerializeField] private EnemySettings settings;
    private IHitFeedback feedback;
    private CharacterHitFeedback flash;
    private CharacterAnimator animationView;
    private void Awake() { feedback=GetComponent<IHitFeedback>(); flash=GetComponent<CharacterHitFeedback>(); animationView=GetComponent<CharacterAnimator>(); }
    public void Telegraph() { flash?.Telegraph(); animationView?.Telegraph(settings.windup); }
    public void Telegraph(float duration) { flash?.Telegraph(); animationView?.Telegraph(duration); }
    public void Hit(float damage,Vector3 direction,float stun) { feedback?.Hit(damage,direction); animationView?.Reaction(stun); }
    public void Attack() => GameAudio.Play(GameAudioCue.EnemyAttack, transform.position, transform);
    public void Death()
    {
        GameAudio.Play(GameAudioCue.EnemyDeath, transform.position);
        animationView?.Death(settings.deathDuration);
    }
}
