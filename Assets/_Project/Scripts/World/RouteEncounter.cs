using FishNet.Object;

// Optional room-content extension. A chest room can complete on collection without routing changes.
// FishNet owns spawn/replication; implementations retain runtime state on this instance.
public abstract class RouteEncounter : NetworkBehaviour
{
    public abstract void BeginEncounter(ProceduralLevelLayout layout, int seed);
    public abstract bool IsComplete { get; }
    public virtual int Remaining => IsComplete ? 0 : 1;
}
