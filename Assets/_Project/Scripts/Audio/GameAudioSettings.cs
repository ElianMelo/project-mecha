using System;
using UnityEngine;
using UnityEngine.Audio;

public enum GameAudioBus { Master, UI, SFX, Ambient, Music }
public enum GameAudioCue
{
    UiMove, UiConfirm, UiCancel, UiOpen, UiClose, UiError, Notification,
    Slash, Combo, Finisher, AirSlash, DashSlash, RisingSlash, DiveStrike, CriticalStrike, MagneticPull,
    MegaSlash, SwordFire, CriticalX, SkillPulse, Hit, PlayerHit, EnemyAttack, EnemyDeath, PlayerDeath,
    Gun, BulletImpact, Throw, LanceImpact, Footstep, EnemyFootstep, Jump, Land, Dash, CrateHit, CrateBreak,
    Heal, Revive, PortalOpen, PortalTravel, PortalHum, MegaSlashImpact, SwordFireImpact, CriticalXImpact
}

[CreateAssetMenu(menuName = "Mecha/Audio/Settings")]
public sealed class GameAudioSettings : ScriptableObject
{
    [Serializable]
    public sealed class Cue
    {
        public GameAudioCue id;
        public GameAudioBus bus = GameAudioBus.SFX;
        public AudioClip[] clips = Array.Empty<AudioClip>();
        [Range(0f, 1f)] public float volume = 0.5f;
        public Vector2 pitch = new Vector2(0.94f, 1.06f);
        [Range(0, 255)] public int priority = 120;
        [Min(1)] public int maxInstances = 6;
        [Min(0f)] public float minimumInterval = 0.025f;
        [Tooltip("Never reclaimed by transient sounds. Use for complete skills and portal cues.")]
        public bool protect;
        public bool loop;
        [Range(0f, 1f)] public float spatialBlend = 0.8f;
    }
    public AudioMixer mixer;
    public AudioMixerGroup ui, sfx, ambient, music;
    public Cue[] cues = Array.Empty<Cue>();
    public AudioClip[] musicPlaylist = Array.Empty<AudioClip>();
    public AudioClip menuMusic;
    public AudioClip[] ambientPlaylist = Array.Empty<AudioClip>();
    [Range(0f, 1f)] public float defaultMaster = 0.8f, defaultUI = 0.75f, defaultSfx = 0.8f, defaultAmbient = 0.45f, defaultMusic = 0.5f;
    [Range(0f, 1f)] public float musicGain = 0.35f, ambientGain = 0.3f;
    [Min(8)] public int transientVoices = 48;
    [Min(1)] public int releaseVoices = 8;
    [Min(0.005f)] public float releaseSeconds = 0.035f;
    [Min(0.1f)] public float crossfadeSeconds = 1.5f;
    [Min(0.1f)] public float minimumDistance = 8f, maximumDistance = 45f;
    [Min(0.1f)] public float footstepDistance = 1.7f, minimumStepInterval = 0.18f;
    [Min(0f)] public float landingSpeed = 2f;
    public float DefaultVolume(GameAudioBus bus) => bus == GameAudioBus.Master ? defaultMaster : bus == GameAudioBus.UI ? defaultUI :
        bus == GameAudioBus.Music ? defaultMusic : bus == GameAudioBus.Ambient ? defaultAmbient : defaultSfx;
    public AudioMixerGroup Group(GameAudioBus bus) => bus == GameAudioBus.UI ? ui : bus == GameAudioBus.Music ? music : bus == GameAudioBus.Ambient ? ambient : sfx;
}
