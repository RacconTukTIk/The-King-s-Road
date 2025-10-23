using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI Elements")]
    public Image backgroundImage;    // ���� ������ ���
    public Image titleImage;         // �������� ����
    public Image playButtonImage;    // ������ ������ (PNG)
    public Image quitButtonImage;    // ������ ����� (PNG)

    [Header("Button Sprites")]
    public Sprite playButtonNormal;  // ������� ��������� ������
    public Sprite playButtonHover;   // ��� ��������� ������
    public Sprite quitButtonNormal;  // ������� ��������� �����
    public Sprite quitButtonHover;   // ��� ��������� �����

    [Header("Scene Settings")]
    public string gameSceneName = "GameScene"; // �������� ������� �����

    void Start()
    {
        // ����������, ��� ��� �������� �� �����
        if (playButtonImage == null || quitButtonImage == null)
        {
            Debug.LogError("�� ��������� ������ � ����������!");
            return;
        }

        // ������������� ��������� ������� ��� ������
        playButtonImage.sprite = playButtonNormal;
        quitButtonImage.sprite = quitButtonNormal;

        // ��������� ����������� ������� ��� ������
        SetupButtonEvents();

        Debug.Log("������� ���� ���������");
    }

    void SetupButtonEvents()
    {
        // ===== ������ "������" =====
        EventTrigger playTrigger = playButtonImage.gameObject.GetComponent<EventTrigger>();
        if (playTrigger == null)
            playTrigger = playButtonImage.gameObject.AddComponent<EventTrigger>();

        // ��������� �� ������
        EventTrigger.Entry playEnter = new EventTrigger.Entry();
        playEnter.eventID = EventTriggerType.PointerEnter;
        playEnter.callback.AddListener((data) => {
            if (playButtonHover != null)
            {
                playButtonImage.sprite = playButtonHover;
                Debug.Log("��������� �� ������ ������");
            }
        });
        playTrigger.triggers.Add(playEnter);

        // ���� � ������
        EventTrigger.Entry playExit = new EventTrigger.Entry();
        playExit.eventID = EventTriggerType.PointerExit;
        playExit.callback.AddListener((data) => {
            if (playButtonNormal != null)
            {
                playButtonImage.sprite = playButtonNormal;
                Debug.Log("���� � ������ ������");
            }
        });
        playTrigger.triggers.Add(playExit);

        // ���� �� ������
        EventTrigger.Entry playClick = new EventTrigger.Entry();
        playClick.eventID = EventTriggerType.PointerClick;
        playClick.callback.AddListener((data) => { OnPlayButtonClicked(); });
        playTrigger.triggers.Add(playClick);

        // ===== ������ "�����" =====
        EventTrigger quitTrigger = quitButtonImage.gameObject.GetComponent<EventTrigger>();
        if (quitTrigger == null)
            quitTrigger = quitButtonImage.gameObject.AddComponent<EventTrigger>();

        // ��������� �� ������
        EventTrigger.Entry quitEnter = new EventTrigger.Entry();
        quitEnter.eventID = EventTriggerType.PointerEnter;
        quitEnter.callback.AddListener((data) => {
            if (quitButtonHover != null)
            {
                quitButtonImage.sprite = quitButtonHover;
                Debug.Log("��������� �� ������ �����");
            }
        });
        quitTrigger.triggers.Add(quitEnter);

        // ���� � ������
        EventTrigger.Entry quitExit = new EventTrigger.Entry();
        quitExit.eventID = EventTriggerType.PointerExit;
        quitExit.callback.AddListener((data) => {
            if (quitButtonNormal != null)
            {
                quitButtonImage.sprite = quitButtonNormal;
                Debug.Log("���� � ������ �����");
            }
        });
        quitTrigger.triggers.Add(quitExit);

        // ���� �� ������
        EventTrigger.Entry quitClick = new EventTrigger.Entry();
        quitClick.eventID = EventTriggerType.PointerClick;
        quitClick.callback.AddListener((data) => { OnQuitButtonClicked(); });
        quitTrigger.triggers.Add(quitClick);
    }

    void OnPlayButtonClicked()
    {
        Debug.Log("������ ������ ������ - ������� � ����");

        // ����� �������� ���� �����
        // AudioManager.Instance.PlaySound("ButtonClick");

        // ����������� �������� ������� �����
        LoadGameScene();
    }

    void OnQuitButtonClicked()
    {
        Debug.Log("������ ������ ����� - �������� ����");

        // ����� �������� ���� �����
        // AudioManager.Instance.PlaySound("ButtonClick");

        // ����������� ����� �� ����
        QuitGame();
    }

    void LoadGameScene()
    {
        // ��������� ������������� �����
        if (SceneExists(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError($"����� '{gameSceneName}' �� ������� � Build Settings!");

            // �������� ��������� ����� �� ������� 1 (������ ������ ������� �����)
            if (SceneManager.sceneCountInBuildSettings > 1)
            {
                Debug.Log("�������� ��������� ����� � �������� 1");
                SceneManager.LoadScene(1);
            }
            else
            {
                Debug.LogError("� Build Settings ��� ������� ����!");
                // ������� ������� ����� �� ����
                CreateFallbackScene();
            }
        }
    }

    bool SceneExists(string sceneName)
    {
        // ���������, ���������� �� ����� � Build Settings
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
        Debug.Log("������� ��������� �����...");

        // ������� ����
        Destroy(gameObject);

        // ������� ������� ������
        GameObject camera = new GameObject("Main Camera");
        camera.AddComponent<Camera>();
        camera.tag = "MainCamera";

        // ������� ����
        GameObject light = new GameObject("Directional Light");
        Light lightComp = light.AddComponent<Light>();
        lightComp.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);

        // ������� �����
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.position = new Vector3(0, -1, 0);
        ground.transform.localScale = new Vector3(2, 1, 2);

        // ��������� ����� � �����������
        GameObject canvasObj = new GameObject("InfoCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        GameObject textObj = new GameObject("InfoText");
        textObj.transform.SetParent(canvasObj.transform);
        Text text = textObj.AddComponent<Text>();
        text.text = "������ ������� ����� � Build Settings!\n\n����������:\n��� - ��������� ������\n����� ���� ������ ������";
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        // ����������� RectTransform
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Debug.Log("��������� ����� �������!");
    }

    // ��������� ���������� ��� ��������
    void Update()
    {
        // Enter ��� ������ - ������ ����
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            OnPlayButtonClicked();
        }

        // Escape - �����
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnQuitButtonClicked();
        }
    }
}