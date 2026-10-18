using System;

// Project-owned save paths use PascalCase segments. Package payloads retain their native schema.
public static class GameSaveKeys
{
    public const string ControlsOverrides = "Mecha.Controls.Overrides";
    public const string VideoPrefix = "Mecha.Video.";
    public const string LegacyMigration = "Mecha.Migration.PlayerPrefs";

    public static string AudioVolume(GameAudioBus bus)
    {
        switch (bus)
        {
            case GameAudioBus.Master: return "Mecha.Audio.Master";
            case GameAudioBus.UI: return "Mecha.Audio.Ui";
            case GameAudioBus.SFX: return "Mecha.Audio.Sfx";
            case GameAudioBus.Ambient: return "Mecha.Audio.Ambient";
            case GameAudioBus.Music: return "Mecha.Audio.Music";
            default: throw new ArgumentOutOfRangeException(nameof(bus));
        }
    }
}
