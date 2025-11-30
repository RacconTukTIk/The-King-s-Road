using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip mainMenuMusic;
    public AudioClip footstepSound;
    public AudioClip buttonClickSound;
    public AudioClip constructionSound;
    public AudioClip completeSound;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
    [Range(0f, 1f)]
    public float sfxVolume = 0.7f;

    private Dictionary<string, AudioClip> soundDictionary = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudio()
    {
        // Configure audio sources
        musicSource.loop = true;
        musicSource.volume = musicVolume;

        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;

        // Populate sound dictionary
        soundDictionary["main_menu_music"] = mainMenuMusic;
        soundDictionary["footstep"] = footstepSound;
        soundDictionary["button_click"] = buttonClickSound;
        soundDictionary["construction"] = constructionSound;
        soundDictionary["complete"] = completeSound;
    }

    public void PlayMusic(string clipName)
    {
        if (soundDictionary.ContainsKey(clipName) && soundDictionary[clipName] != null)
        {
            musicSource.clip = soundDictionary[clipName];
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlaySFX(string clipName, float volumeScale = 1f)
    {
        if (soundDictionary.ContainsKey(clipName) && soundDictionary[clipName] != null)
        {
            sfxSource.PlayOneShot(soundDictionary[clipName], sfxVolume * volumeScale);
        }
    }

    public void PlaySFXAtPosition(string clipName, Vector3 position, float volumeScale = 1f)
    {
        if (soundDictionary.ContainsKey(clipName) && soundDictionary[clipName] != null)
        {
            AudioSource.PlayClipAtPoint(soundDictionary[clipName], position, sfxVolume * volumeScale);
        }
    }

    // Volume control methods
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
    }
}