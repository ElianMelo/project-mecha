// Per-player cursors are runtime state; authored combo assets remain read-only.
public sealed class ComboSequencePlayer
{
    private ComboDefinition current;
    private int next;
    private float expires;
    public void Reset() { current = null; next = 0; expires = 0f; }
    public AttackPiece Consume(ComboDefinition sequence, float now)
    {
        if (sequence == null || sequence.attacks == null || sequence.attacks.Length == 0) return null;
        if (current != sequence || now > expires) { current = sequence; next = 0; }
        if (next >= sequence.attacks.Length)
        {
            if (!sequence.loop) return null;
            next = 0;
        }
        var piece = sequence.attacks[next++];
        expires = now + sequence.inputWindow;
        return piece;
    }
}
