using UnityEngine;

namespace Crossroads.UI
{
    // Central audio playback for the game (spec 12.3): one looping music source + one SFX source.
    // Content-agnostic - clips come from the Theme, so cloning swaps audio via the theme (J8) with no
    // code. The AudioSources are created at runtime (not serialized), so the wire tool only AddComponents
    // this and the bootstrap drives it.
    public sealed class AudioDirector : MonoBehaviour
    {
        private AudioSource _music;
        private AudioSource _sfx;

        // Mute toggles, persisted in PlayerPrefs so the choice survives restarts (spec 9.5 polish).
        private const string MusicPref = "cr.audio.music";
        private const string SfxPref = "cr.audio.sfx";
        private bool _musicOn = true;
        private bool _sfxOn = true;
        private bool _prefsLoaded;
        private AudioClip _lastMusicClip;     // remembered so un-muting can resume the right track
        private float _lastMusicVolume = 0.45f;

        public bool MusicEnabled { get { LoadPrefs(); return _musicOn; } }
        public bool SfxEnabled { get { LoadPrefs(); return _sfxOn; } }

        private void LoadPrefs()
        {
            if (_prefsLoaded) return;
            _musicOn = PlayerPrefs.GetInt(MusicPref, 1) != 0;
            _sfxOn = PlayerPrefs.GetInt(SfxPref, 1) != 0;
            _prefsLoaded = true;
        }

        public void SetMusicEnabled(bool on)
        {
            LoadPrefs();
            _musicOn = on;
            PlayerPrefs.SetInt(MusicPref, on ? 1 : 0); PlayerPrefs.Save();
            if (!on) StopMusic();
            else if (_lastMusicClip != null) PlayMusic(_lastMusicClip, _lastMusicVolume);
        }

        public void SetSfxEnabled(bool on)
        {
            LoadPrefs();
            _sfxOn = on;
            PlayerPrefs.SetInt(SfxPref, on ? 1 : 0); PlayerPrefs.Save();
        }

        // The active director + its UI click clip, so any generic UI button can play a click without
        // each component wiring an AudioDirector reference (same static-helper pattern as UIFonts).
        private static AudioDirector _active;
        private AudioClip _clickClip;

        public void ConfigureUiClick(AudioClip click) { _active = this; _clickClip = click; }
        public static void PlayClick() { if (_active != null) _active.PlaySfx(_active._clickClip); }

        // Starts (or keeps) a looping music track. No-op if the clip is null or already playing. Remembers
        // the requested track so a later un-mute can resume it; honors the music toggle.
        public void PlayMusic(AudioClip clip, float volume = 0.45f)
        {
            if (clip == null) return;
            LoadPrefs();
            _lastMusicClip = clip;
            _lastMusicVolume = volume;
            if (!_musicOn) return;
            Ensure();
            if (_music.clip == clip && _music.isPlaying) return;
            _music.clip = clip;
            _music.volume = volume;
            _music.loop = true;
            _music.Play();
        }

        public void StopMusic()
        {
            if (_music != null) _music.Stop();
        }

        // Fires a one-shot sound effect. No-op if the clip is null or sound is toggled off (covers UI
        // clicks and the card swipe/appear sounds, which all route through here).
        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            LoadPrefs();
            if (!_sfxOn) return;
            Ensure();
            _sfx.PlayOneShot(clip, volume);
        }

        private void Ensure()
        {
            if (_music != null) return;
            var existing = GetComponents<AudioSource>();
            if (existing.Length >= 2) { _music = existing[0]; _sfx = existing[1]; }
            else
            {
                _music = gameObject.AddComponent<AudioSource>();
                _sfx = gameObject.AddComponent<AudioSource>();
            }
            _music.playOnAwake = false; _music.loop = true;
            _sfx.playOnAwake = false;
        }
    }
}
