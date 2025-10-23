using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI Elements")]
    public Image backgroundImage;    // Твой задний фон
    public Image titleImage;         // Название игры
    public Image playButtonImage;    // Кнопка Играть (PNG)
    public Image quitButtonImage;    // Кнопка Выход (PNG)

    [Header("Button Sprites")]
    public Sprite playButtonNormal;  // Обычное состояние Играть
    public Sprite playButtonHover;   // При наведении Играть
    public Sprite quitButtonNormal;  // Обычное состояние Выход
    public Sprite quitButtonHover;   // При наведении Выход

    [Header("Scene Settings")]
    public string gameSceneName = "GameScene"; // Название игровой сцены

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

        Debug.Log("Главное меню загружено");
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
                Debug.Log("Наведение на кнопку Играть");
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
                Debug.Log("Уход с кнопки Играть");
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
                Debug.Log("Наведение на кнопку Выход");
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
                Debug.Log("Уход с кнопки Выход");
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

        // Можно добавить звук клика
        // AudioManager.Instance.PlaySound("ButtonClick");

        // Немедленная загрузка игровой сцены
        LoadGameScene();
    }

    void OnQuitButtonClicked()
    {
        Debug.Log("Нажата кнопка Выход - закрытие игры");

        // Можно добавить звук клика
        // AudioManager.Instance.PlaySound("ButtonClick");

        // Немедленный выход из игры
        QuitGame();
    }

    void LoadGameScene()
    {
        // Проверяем существование сцены
        if (SceneExists(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError($"Сцена '{gameSceneName}' не найдена в Build Settings!");

            // Пытаемся загрузить сцену по индексу 1 (обычно первая игровая сцена)
            if (SceneManager.sceneCountInBuildSettings > 1)
            {
                Debug.Log("Пытаемся загрузить сцену с индексом 1");
                SceneManager.LoadScene(1);
            }
            else
            {
                Debug.LogError("В Build Settings нет игровых сцен!");
                // Создаем простую сцену на лету
                CreateFallbackScene();
            }
        }
    }

    bool SceneExists(string sceneName)
    {
        // Проверяем, существует ли сцена в Build Settings
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

    void CreateFallbackScene()
    {
        Debug.Log("Создаем временную сцену...");

        // Очищаем меню
        Destroy(gameObject);

        // Создаем базовую камеру
        GameObject camera = new GameObject("Main Camera");
        camera.AddComponent<Camera>();
        camera.tag = "MainCamera";

        // Создаем свет
        GameObject light = new GameObject("Directional Light");
        Light lightComp = light.AddComponent<Light>();
        lightComp.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);

        // Создаем землю
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.position = new Vector3(0, -1, 0);
        ground.transform.localScale = new Vector3(2, 1, 2);

        // Добавляем текст с инструкцией
        GameObject canvasObj = new GameObject("InfoCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        GameObject textObj = new GameObject("InfoText");
        textObj.transform.SetParent(canvasObj.transform);
        Text text = textObj.AddComponent<Text>();
        text.text = "Добавь игровую сцену в Build Settings!\n\nУправление:\nЛКМ - создавать юнитов\nЮниты сами строят здания";
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        // Настраиваем RectTransform
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Debug.Log("Временная сцена создана!");
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