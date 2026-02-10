using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI Elements")]
    public Image backgroundImage;
    public Image titleImage;
    public Image playButtonImage;
    public Image quitButtonImage;

    [Header("Button Sprites")]
    public Sprite playButtonNormal;
    public Sprite playButtonHover;
    public Sprite quitButtonNormal;
    public Sprite quitButtonHover;

    [Header("Scene Settings")]
    public string gameSceneName = "GameScene";

    void Start()
    {
        // Убеждаемся, что все элементы на месте
        if (playButtonImage == null || quitButtonImage == null)
        {
            Debug.LogError("Не назначены кнопки в инспекторе!");
            return;
        }

        // Устанавливаем начальные спрайты для кнопок
        playButtonImage.sprite = playButtonNormal;
        quitButtonImage.sprite = quitButtonNormal;

        // Добавляем обработчики событий для кнопок
        SetupButtonEvents();

        // Запускаем фоновую музыку
        PlayMenuMusic();

        Debug.Log("Главное меню загружено");
    }

    void PlayMenuMusic()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic("main_menu_music");
        }
    }

    void SetupButtonEvents()
    {
        // ===== КНОПКА "ИГРАТЬ" =====
        EventTrigger playTrigger = playButtonImage.gameObject.GetComponent<EventTrigger>();
        if (playTrigger == null)
            playTrigger = playButtonImage.gameObject.AddComponent<EventTrigger>();

        // Наведение на кнопку
        EventTrigger.Entry playEnter = new EventTrigger.Entry();
        playEnter.eventID = EventTriggerType.PointerEnter;
        playEnter.callback.AddListener((data) => {
            if (playButtonHover != null)
            {
                playButtonImage.sprite = playButtonHover;
                // Воспроизводим звук при наведении (опционально)
                // AudioManager.Instance.PlaySFX("button_click", 0.3f);
            }
        });
        playTrigger.triggers.Add(playEnter);

        // Уход с кнопки
        EventTrigger.Entry playExit = new EventTrigger.Entry();
        playExit.eventID = EventTriggerType.PointerExit;
        playExit.callback.AddListener((data) => {
            if (playButtonNormal != null)
            {
                playButtonImage.sprite = playButtonNormal;
            }
        });
        playTrigger.triggers.Add(playExit);

        // Клик по кнопке
        EventTrigger.Entry playClick = new EventTrigger.Entry();
        playClick.eventID = EventTriggerType.PointerClick;
        playClick.callback.AddListener((data) => { OnPlayButtonClicked(); });
        playTrigger.triggers.Add(playClick);

        // ===== КНОПКА "ВЫХОД" =====
        EventTrigger quitTrigger = quitButtonImage.gameObject.GetComponent<EventTrigger>();
        if (quitTrigger == null)
            quitTrigger = quitButtonImage.gameObject.AddComponent<EventTrigger>();

        // Наведение на кнопку
        EventTrigger.Entry quitEnter = new EventTrigger.Entry();
        quitEnter.eventID = EventTriggerType.PointerEnter;
        quitEnter.callback.AddListener((data) => {
            if (quitButtonHover != null)
            {
                quitButtonImage.sprite = quitButtonHover;
            }
        });
        quitTrigger.triggers.Add(quitEnter);

        // Уход с кнопки
        EventTrigger.Entry quitExit = new EventTrigger.Entry();
        quitExit.eventID = EventTriggerType.PointerExit;
        quitExit.callback.AddListener((data) => {
            if (quitButtonNormal != null)
            {
                quitButtonImage.sprite = quitButtonNormal;
            }
        });
        quitTrigger.triggers.Add(quitExit);

        // Клик по кнопке
        EventTrigger.Entry quitClick = new EventTrigger.Entry();
        quitClick.eventID = EventTriggerType.PointerClick;
        quitClick.callback.AddListener((data) => { OnQuitButtonClicked(); });
        quitTrigger.triggers.Add(quitClick);
    }

    void OnPlayButtonClicked()
    {
        Debug.Log("Нажата кнопка Играть - переход к игре");

        // Воспроизводим звук клика
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        // Немедленная загрузка игровой сцены
        LoadGameScene();
    }

    void OnQuitButtonClicked()
    {
        Debug.Log("Нажата кнопка Выход - закрытие игры");

        // Воспроизводим звук клика
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("button_click");
        }

        // Немедленный выход из игры
        QuitGame();
    }

    void LoadGameScene()
    {   
        //Остановка музыки при загрузке новой сцены
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }

        // Проверяем существование сцены
        if (SceneExists(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError($"Сцена '{gameSceneName}' не найдена в Build Settings!");
            // ... остальной код создания временной сцены
        }
    }

    bool SceneExists(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameInBuild = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameInBuild == sceneName)
                return true;
        }
        return false;
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Обработка клавиатуры для удобства
    void Update()
    {
        // Enter или Пробел - начать игру
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            OnPlayButtonClicked();
        }

        // Escape - выход
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnQuitButtonClicked();
        }
    }
}