using UnityEngine;

// Local display preferences, independent of network identity and authored squad colors.
public static class PlayerHighlightPreferences
{
    private static bool? self, players;
    public static bool ShowSelf => self ?? (self = GameSave.GetBool(GameSaveKeys.SelfHighlight, false)).Value;
    public static bool ShowPlayers => players ?? (players = GameSave.GetBool(GameSaveKeys.PlayersHighlight, true)).Value;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { self = players = null; }
    public static void SetSelf(bool value) { self = value; GameSave.SetBool(GameSaveKeys.SelfHighlight, value); GameSave.Save(); }
    public static void SetPlayers(bool value) { players = value; GameSave.SetBool(GameSaveKeys.PlayersHighlight, value); GameSave.Save(); }
}
