using UnityEngine;

public class EnemyPresentation : MonoBehaviour
{
    [SerializeField] private EnemySettings settings;
    private IHitFeedback feedback;
    private CharacterHitFeedback flash;
    private CharacterAnimator animationView;
    private void Awake() { feedback=GetComponent<IHitFeedback>(); flash=GetComponent<CharacterHitFeedback>(); animationView=GetComponent<CharacterAnimator>(); }
    public void Telegraph() { flash?.Telegraph(); animationView?.Telegraph(settings.windup); }
    public void Hit(float damage,Vector3 direction,float stun) { feedback?.Hit(damage,direction); animationView?.Reaction(stun); }
    public void Death() => animationView?.Death(settings.deathDuration);
}
