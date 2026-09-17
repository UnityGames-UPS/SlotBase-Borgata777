using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using Best.SocketIO;

public class AudioController : MonoBehaviour
{
    [SerializeField] private AudioSource bg_adudio;
    [SerializeField] internal AudioSource audioPlayer_wl;
    [SerializeField] internal AudioSource audioPlayer_button;
    [SerializeField] internal AudioSource audioSpin_button;
    [SerializeField] private AudioClip[] clips;
    [SerializeField] private AudioClip[] Bonusclips;
    [SerializeField] private AudioSource bg_audioBonus;
    [SerializeField] private AudioSource audioPlayer_Bonus;

    private readonly Dictionary<AudioSource, bool> preFocusMuteState = new Dictionary<AudioSource, bool>();
    private bool isForceMuted = false;

    private void Start()
    {
        // if (bg_adudio) bg_adudio.Play();
        audioPlayer_button.clip = clips[clips.Length - 1];
        audioSpin_button.clip = clips[clips.Length - 2];
    }

    // Focus-driven mute. Called from BOTH the JS bridge (UIManager.OnFocusChanged) and
    // Unity's native OnApplicationFocus, so it must be idempotent per direction — otherwise
    // the second call for the same blur re-captures the already-forced mute as the
    // "restore to" value and the audio stays silent after every refocus.
    internal void SetMuteAll(bool forceMute)
    {
        if (forceMute == isForceMuted) return;
        isForceMuted = forceMute;

        foreach (var source in AllSources())
        {
            if (source == null) continue;
            if (forceMute)
            {
                preFocusMuteState[source] = source.mute;
                source.mute = true;
            }
            else
            {
                source.mute = preFocusMuteState.TryGetValue(source, out bool prevMuted) ? prevMuted : source.mute;
            }
        }
    }

    private IEnumerable<AudioSource> AllSources()
    {
        yield return bg_adudio;
        yield return audioPlayer_wl;
        yield return audioPlayer_button;
        yield return audioSpin_button;
        yield return bg_audioBonus;
        yield return audioPlayer_Bonus;
    }

    internal void SwitchBGSound(bool isbonus)
    {
        if (isbonus)
        {
            if (bg_audioBonus) bg_audioBonus.enabled = true;
            // if (bg_adudio) bg_adudio.enabled = false;
        }
        else
        {
            if (bg_audioBonus) bg_audioBonus.enabled = false;
            // if (bg_adudio) bg_adudio.enabled = true;
        }
    }

    internal void PlayWLAudio(string type)
    {
        audioPlayer_wl.loop = false;
        int index = 0;
        switch (type)
        {
            case "spin":
                index = 0;
                audioPlayer_wl.loop = true;
                break;
            case "win":
                index = 1;
                break;
            case "lose":
                index = 2;
                break;
            case "spinStop":
                index = 3;
                break;
            case "megaWin":
                index = 4;
                break;
            case "phone":
                index = 7;
                break;
        }
        StopWLAaudio();
        audioPlayer_wl.clip = clips[index];
        audioPlayer_wl.Play();

    }

    internal void PlayBonusAudio(string type)
    {
        audioPlayer_wl.loop = false;
        int index = 0;
        switch (type)
        {
            case "win":
                index = 0;
                break;
            case "lose":
                index = 1;
                break;
            case "cycleSpin":
                index = 2;
                break;
        }
        StopBonusAaudio();
        audioPlayer_Bonus.clip = Bonusclips[index];
        audioPlayer_Bonus.Play();

    }

    internal void PlayButtonAudio()
    {
        audioPlayer_button.Play();
    }

    internal void PlaySpinButtonAudio()
    {
        audioSpin_button.Play();
    }

    internal void StopWLAaudio()
    {

        audioPlayer_wl.Stop();
        audioPlayer_wl.loop = false;

    }

    internal void StopBonusAaudio()
    {
        audioPlayer_Bonus.Stop();
        audioPlayer_Bonus.loop = false;
    }

    internal void StopBgAudio()
    {
        bg_adudio.Stop();
    }

    // User-toggle entry point (sound/music buttons). An explicit interaction proves the game
    // really has focus, so it clears any stale forced mute before applying the category state —
    // otherwise the button would appear dead, or its effect would be undone on the next refocus.
    internal void ToggleMute(bool toggle, string type = "all")
    {
        if (isForceMuted)
        {
            isForceMuted = false;
            preFocusMuteState.Clear();
        }

        switch (type)
        {
            case "bg":
                //   bg_adudio.mute = toggle;
                bg_audioBonus.mute = toggle;
                break;
            case "button":
                audioPlayer_button.mute = toggle;
                audioSpin_button.mute = toggle;
                break;
            case "wl":
                audioPlayer_wl.mute = toggle;
                audioPlayer_Bonus.mute = toggle;
                break;
            case "all":
                audioPlayer_wl.mute = toggle;
                //  bg_adudio.mute = toggle;
                audioPlayer_button.mute = toggle;
                audioSpin_button.mute = toggle;
                break;
        }
    }

    // Native/editor focus path — calls the SAME method the WebGL OnFocusChanged path calls.
    private void OnApplicationFocus(bool focus)
    {
        SetMuteAll(!focus);
    }
}
