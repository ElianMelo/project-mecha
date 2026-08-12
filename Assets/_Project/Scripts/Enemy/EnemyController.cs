using FishNet.Object;
using UnityEngine;

public class EnemyController : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Sword"))
        {
            RequestDespawn();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDespawn()
    {
        Despawn();
    }
}
