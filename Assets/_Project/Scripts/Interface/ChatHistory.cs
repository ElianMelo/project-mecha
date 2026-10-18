using System.Collections.Generic;
using System.Text;

// Bounded, expiring client history. The clock is supplied so paused menus don't retain stale lines.
public sealed class ChatHistory
{
    private readonly Queue<(string line, float expires)> lines = new Queue<(string, float)>();
    public int Count => lines.Count;
    public void Add(string name, string message, float now, float lifetime, int capacity)
    {
        lines.Enqueue((Clean(name, 24, GameText.Get("Chat", "pilot.fallback")) + ": " + message, now + lifetime));
        while (lines.Count > System.Math.Max(1, capacity)) lines.Dequeue();
    }
    public string Render(float now)
    {
        while (lines.Count > 0 && lines.Peek().expires <= now) lines.Dequeue();
        var text = new StringBuilder();
        foreach (var line in lines) { if (text.Length > 0) text.Append('\n'); text.Append(line.line); }
        return text.ToString();
    }
    public static string Clean(string value, int limit, string fallback = "")
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var text = new StringBuilder();
        foreach (char c in value)
        {
            if (!char.IsControl(c)) text.Append(c);
            if (text.Length >= limit) break;
        }
        string result = text.ToString().Trim();
        return result.Length == 0 ? fallback : result;
    }
}
