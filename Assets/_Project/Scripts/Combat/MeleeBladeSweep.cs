using System.Collections.Generic;
using UnityEngine;

// Native PhysX queries across the animated blade segment, after Animator evaluation.
// No damage, timing clock, animation correction or presentation lives here.
public sealed class MeleeBladeSweep
{
    public struct Contact
    {
        public Collider Collider;
        public IDamageable Target;
        public DamageContact Surface;
    }
    private Collider[] buffer=new Collider[32];
    private readonly HashSet<IDamageable> targets=new HashSet<IDamageable>();
    private readonly List<Contact> contacts=new List<Contact>();
    private Vector3 previousBase, previousTip;
    private float previousElapsed;
    private bool sampled;
    public void Reset(){sampled=false;targets.Clear();contacts.Clear();}
    public IReadOnlyList<Contact> Sample(MeleeBlade blade, float elapsed, float impact, float endTime,
        float depth, int layers, int obstructions, PlayerHealth source)
    {
        contacts.Clear();
        blade.Read(out var edgeBase,out var edgeTip,depth);
        if(!sampled){previousBase=edgeBase;previousTip=edgeTip;previousElapsed=elapsed;sampled=true;}
        float interval=elapsed-previousElapsed;
        if(elapsed >= impact && previousElapsed <= endTime && elapsed >= previousElapsed)
        {
            float from=interval>.000001f?Mathf.Clamp01((impact-previousElapsed)/interval):1f;
            float until=interval>.000001f?Mathf.Clamp01((endTime-previousElapsed)/interval):1f;
            Vector3 a0=Vector3.Lerp(previousBase,edgeBase,from), b0=Vector3.Lerp(previousTip,edgeTip,from);
            Vector3 a1=Vector3.Lerp(previousBase,edgeBase,until), b1=Vector3.Lerp(previousTip,edgeTip,until);
            float radius=blade.settings.contactRadius;
            int steps=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(Vector3.Distance(a0,a1),Vector3.Distance(b0,b1))/radius),1,blade.settings.maximumSweepSteps);
            Physics.SyncTransforms();
            var sourceBody=source.GetComponent<Collider>();
            Vector3 sourceCenter=sourceBody != null ? sourceBody.bounds.center : source.transform.position;
            for(int s=0;s<=steps;s++)
            {
                float fraction=(float)s/steps;
                Vector3 a=Vector3.Lerp(a0,a1,fraction), b=Vector3.Lerp(b0,b1,fraction);
                int count;
                while(true)
                {
                    count=Physics.OverlapCapsuleNonAlloc(a,b,radius,buffer,layers,QueryTriggerInteraction.Ignore);
                    if(count<buffer.Length)break;
                    if(buffer.Length>=256){buffer=Physics.OverlapCapsule(a,b,radius,layers,QueryTriggerInteraction.Ignore);count=buffer.Length;break;}
                    System.Array.Resize(ref buffer,buffer.Length*2);
                }
                for(int i=0;i<count;i++)
                {
                    var collider=buffer[i];var target=collider.GetComponentInParent<IDamageable>();
                    if(!CombatTargetRules.CanHit(source,target) || targets.Contains(target))continue;
                    Vector3 segment=b-a;
                    float along=segment.sqrMagnitude>.000001f?Mathf.Clamp01(Vector3.Dot(collider.bounds.center-a,segment)/segment.sqrMagnitude):0f;
                    Vector3 onBlade=Vector3.Lerp(a,b,along);
                    Vector3 point=collider.ClosestPoint(onBlade);
                    if(Physics.Linecast(a,point,obstructions,QueryTriggerInteraction.Ignore))continue;
                    Vector3 travel=Vector3.Lerp(edgeBase-previousBase,edgeTip-previousTip,along);
                    if(travel.sqrMagnitude<.000001f)travel=point-source.transform.position;
                    travel.z=0;
                    // Sparks continue away from the attacker through the contact.
                    // Keep measured blade travel separately for the physical contact.
                    Vector3 spray=point-sourceCenter;
                    spray.z=0;
                    if(spray.sqrMagnitude<.000001f)spray=source.transform.forward;
                    targets.Add(target);
                    contacts.Add(new Contact{Collider=collider,Target=target,Surface=new DamageContact(point,travel,spray)});
                }
            }
        }
        previousBase=edgeBase;previousTip=edgeTip;previousElapsed=elapsed;
        return contacts;
    }
}
