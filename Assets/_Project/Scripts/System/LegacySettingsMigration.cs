using System;
using UnityEngine;

// Import the previous registry-backed settings once. Keep the originals for rollback.
internal static class LegacySettingsMigration
{
    public static void Import()
    {
        if (GameSave.GetBool(GameSaveKeys.LegacyMigration)) return;
        foreach (GameAudioBus bus in Enum.GetValues(typeof(GameAudioBus)))
        {
            string destination = GameSaveKeys.AudioVolume(bus), previous = "Mecha.Audio." + bus;
            if (!GameSave.HasKey(destination) && PlayerPrefs.HasKey(previous))
            {
                float value = PlayerPrefs.GetFloat(previous);
                if (!float.IsNaN(value) && !float.IsInfinity(value)) GameSave.SetFloat(destination, value);
            }
        }
        foreach (string name in new[] { "Antialiasing", "TextureLimit", "VSync", "FrameLimit", "Width", "Height", "Mode", "Windowed" })
        {
            string previous = "Mecha.Video." + name;
            string destination = GameSaveKeys.VideoPrefix + (name == "VSync" ? "Vsync" : name);
            if (!GameSave.HasKey(destination) && PlayerPrefs.HasKey(previous)) GameSave.SetInt(destination, PlayerPrefs.GetInt(previous));
        }
        if (!GameSave.HasKey(GameSaveKeys.ControlsOverrides))
        {
            string previous = PlayerPrefs.HasKey(GameSaveKeys.ControlsOverrides) ? GameSaveKeys.ControlsOverrides : "mecha.controls.overrides.v1";
            if (PlayerPrefs.HasKey(previous))
            {
                try { GameSave.SetJson(GameSaveKeys.ControlsOverrides, PlayerPrefs.GetString(previous)); }
                catch (Newtonsoft.Json.JsonException error) { Debug.LogWarning("Previous control remaps could not be imported; the original preference was retained. " + error.Message); }
            }
        }
        GameSave.SetBool(GameSaveKeys.LegacyMigration, true);
    }
}
