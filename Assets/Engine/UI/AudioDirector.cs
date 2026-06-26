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

        // Starts (or keeps) a looping music track. No-op if the clip is null or already playing.
        public void PlayMusic(AudioClip clip, float volume = 0.45f)
        {
            if (clip == null) return;
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

        // Fires a one-shot sound effect. No-op if the clip is null.
        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
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
