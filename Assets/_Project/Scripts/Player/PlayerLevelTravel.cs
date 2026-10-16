using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth), typeof(PlayerInputs))]
public class PlayerLevelTravel : NetworkBehaviour
{
    private void OnEnable() => GetComponent<PlayerInputs>().OnInteractInput += Interact;
    private void OnDisable() => GetComponent<PlayerInputs>().OnInteractInput -= Interact;
    private void Interact()
    {
        if (IsOwner) RequestTravel();
    }
    [ServerRpc]
    private void RequestTravel() => ProceduralLevelDirector.Instance?.TryTravel(GetComponent<PlayerHealth>());
}
