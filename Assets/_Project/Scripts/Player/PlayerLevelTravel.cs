using FishNet.Object;
using UnityEngine;

// Owner progress is presentation. The server measures its own hold duration.
[DefaultExecutionOrder(50)]
[RequireComponent(typeof(PlayerHealth), typeof(PlayerInputs))]
public class PlayerLevelTravel : NetworkBehaviour
{
    private PlayerInputs inputs;
    private PlayerHealth health;
    private PlayerAttack attack;
    private bool holding, serverHolding;
    private float began, serverBegan;
    private int room, serverRoom;
    private ProceduralLevelDirector Director => ProceduralLevelDirector.Instance;
    private bool Eligible => Director != null && Director.Layout != null && Director.IsClear && health.Health > 0f && !health.IsStunned &&
        (attack == null || !attack.IsBusy && !attack.ServerBusy) &&
        Vector3.Distance(transform.position, Director.Layout.Portal) <= Director.Settings.portalInteractionRange;
    public bool CanUsePortal => inputs != null && inputs.HasControl && Eligible;
    public bool IsHolding => holding;
    public float HoldProgress => holding && CanUsePortal ? Mathf.Clamp01((Time.time - began) / Director.Settings.portalHoldDuration) : 0f;
    private void Awake() { inputs = GetComponent<PlayerInputs>(); health = GetComponent<PlayerHealth>(); attack = GetComponent<PlayerAttack>(); }
    private void Update()
    {
        bool local = NetworkObject != null && (!IsSpawned || IsOwner);
        if (local)
        {
            bool wantsHold = CanUsePortal && inputs.AttackHeld;
            if (holding && (Director == null || Director.LevelNumber != room)) wantsHold = false;
            if (wantsHold != holding)
            {
                holding = wantsHold;
                if (holding) { began = Time.time; room = Director.LevelNumber; }
                if (IsSpawned) SetHold(holding);
            }
        }
        if (NetworkObject == null || !IsServerInitialized || !serverHolding) return;
        if (!Eligible || Director.LevelNumber != serverRoom) { serverHolding = false; return; }
        if (Time.time - serverBegan >= Director.Settings.portalHoldDuration)
        {
            serverHolding = false;
            Director.TryTravel(health);
        }
    }
    [ServerRpc]
    private void SetHold(bool pressed)
    {
        if (!pressed) { serverHolding = false; return; }
        if (serverHolding || !Eligible) return;
        serverHolding = true; serverBegan = Time.time; serverRoom = Director.LevelNumber;
    }
    public override void OnStopClient() { holding = false; base.OnStopClient(); }
    public override void OnStopServer() { serverHolding = false; base.OnStopServer(); }
    private void OnDisable() { holding = false; serverHolding = false; }
}
