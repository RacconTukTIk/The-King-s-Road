using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

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

    // Приватные ссылки на AudioSource (будут созданы автоматически)
    private AudioSource musicSource;
    private AudioSource sfxSource;

    private Dictionary<string, AudioClip> soundDictionary = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateAudioSources(); // Создаем AudioSource
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CreateAudioSources()
    {
        Debug.Log("Создаю AudioSource компоненты...");

        // Создаем Music Source
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.name = "MusicSource";
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = musicVolume;

        // Создаем SFX Source
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.name = "SfxSource";
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;

        Debug.Log($"AudioSource созданы: Music={musicSource}, SFX={sfxSource}");
    }

    private void InitializeAudio()
    {
        // Настройка аудио клипов
        if (buttonClickSound == null)
        {
            Debug.LogWarning("ButtonClickSound не назначен! Создаю тестовый...");
            buttonClickSound = CreateTestSound(800, 0.1f); // Тестовый звук
        }

        if (constructionSound == null)
        {
            constructionSound = CreateTestSound(400, 0.3f);
        }

        if (completeSound == null)
        {
            completeSound = CreateTestSound(1200, 0.2f);
        }

        // Populate sound dictionary
        soundDictionary["main_menu_music"] = mainMenuMusic;
        soundDictionary["footstep"] = footstepSound;
        soundDictionary["button_click"] = buttonClickSound;
        soundDictionary["construction"] = constructionSound;
        soundDictionary["complete"] = completeSound;

        Debug.Log($"Аудио менеджер инициализирован. Звуков: {soundDictionary.Count}");

        // Тестовое воспроизведение через 1 секунду
        StartCoroutine(TestPlayback());
    }

    private AudioClip CreateTestSound(float frequency, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            // Затухающий синус
            float amplitude = Mathf.Exp(-i / (sampleRate * 0.05f));
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / sampleRate) * amplitude * 0.3f;
        }

        AudioClip clip = AudioClip.Create("TestSound", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private IEnumerator TestPlayback()
    {
        yield return new WaitForSeconds(1f);

        // Тестируем звук
        Debug.Log("Тест: воспроизвожу button_click...");
        PlaySFX("button_click", 0.5f);

        // Если есть музыка - запускаем
        if (mainMenuMusic != null)
        {
            yield return new WaitForSeconds(0.5f);
            PlayMusic("main_menu_music");
            Debug.Log("Музыка запущена");
        }
    }

    public void PlayMusic(string clipName)
    {
        if (soundDictionary.ContainsKey(clipName) && soundDictionary[clipName] != null)
        {
            musicSource.clip = soundDictionary[clipName];
            musicSource.Play();
            Debug.Log($"Запущена музыка: {clipName}");
        }
        else
        {
            Debug.LogWarning($"Музыка '{clipName}' не найдена или равна null");
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
            Debug.Log($"Воспроизведение SFX: {clipName}, громкость: {sfxVolume * volumeScale}");
        }
        else
        {
            Debug.LogWarning($"SFX '{clipName}' не найден или равен null");
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