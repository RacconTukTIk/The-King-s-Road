using UnityEngine;
using System.Collections;

public class ConstructionSite : MonoBehaviour
{
    public int requiredPlanks = 10;
    public int deliveredPlanks = 0;
    public bool isComplete = false;

    [Header("Construction Sprites")]
    public Sprite[] constructionSprites;
    private SpriteRenderer spriteRenderer;

    [Header("Construction Effects")]
    public ParticleSystem buildEffect;
    public ParticleSystem completeEffect;

    [Header("Progress Bar - Frame Animation")]
    public Sprite[] progressBarFrames; // ���� 10 PNG ������
    public float frameRate = 10f; // �������� �������� (������ � �������)

    [Header("Progress Bar - Fill Settings")]
    public Transform fillTransform; // Transform ������� Fill
    public Color startColor = Color.red;
    public Color endColor = Color.green;

    private SpriteRenderer frameRenderer;
    private SpriteRenderer fillRenderer;
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private bool isProgressBarActive = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        InitializeProgressBar();
        UpdateConstructionSprite();
    }

    void Update()
    {
        // ��������� �������� ����� ���� ��������-��� �������
        if (isProgressBarActive && progressBarFrames != null && progressBarFrames.Length > 0)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / frameRate)
            {
                frameTimer = 0f;
                currentFrame = (currentFrame + 1) % progressBarFrames.Length;
                frameRenderer.sprite = progressBarFrames[currentFrame];
            }
        }
    }

    private void InitializeProgressBar()
    {
        // ������� ���������� Frame � Fill ����� �������� ��������
        Transform progressBarTransform = transform.Find("ConstructionProgressBar");

        if (progressBarTransform != null)
        {
            // ���� Frame
            Transform frameTransform = progressBarTransform.Find("Frame");
            if (frameTransform != null)
            {
                frameRenderer = frameTransform.GetComponent<SpriteRenderer>();
            }

            // ���� Fill
            Transform fillTransformObj = progressBarTransform.Find("Fill");
            if (fillTransformObj != null)
            {
                fillTransform = fillTransformObj;
                fillRenderer = fillTransform.GetComponent<SpriteRenderer>();

                // ���������� �������� ����������
                UpdateFillProgress(0f);
            }

            // ���������� �������� ���� ��������-���
            progressBarTransform.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("�� ������ ������ ConstructionProgressBar ����� �������� ��������!");
        }
    }

    public bool NeedsPlanks()
    {
        return !isComplete && deliveredPlanks < requiredPlanks;
    }

    public void DeliverPlank()
    {
        if (isComplete) return;

        deliveredPlanks++;
        Debug.Log($"���������� �����: {deliveredPlanks}/{requiredPlanks}");

        // ���������� ��������-��� ��� ������ ��������
        if (deliveredPlanks == 1)
        {
            ShowProgressBar();
        }

        UpdateConstructionSprite();
        UpdateProgressBar();

        if (buildEffect != null)
        {
            buildEffect.Play();
        }

        if (deliveredPlanks >= requiredPlanks)
        {
            CompleteConstruction();
        }
    }

    private void ShowProgressBar()
    {
        Transform progressBarTransform = transform.Find("ConstructionProgressBar");
        if (progressBarTransform != null)
        {
            progressBarTransform.gameObject.SetActive(true);
            isProgressBarActive = true;
        }
    }

    private void UpdateProgressBar()
    {
        float progress = (float)deliveredPlanks / requiredPlanks;
        UpdateFillProgress(progress);

        // �������������� ������� ��� ��������� ���������
        StartCoroutine(FillAnimation());
    }

    private void UpdateFillProgress(float progress)
    {
        if (fillTransform != null)
        {
            // ������������ ���������� �� X �� 0 �� 1
            Vector3 newScale = fillTransform.localScale;
            newScale.x = progress;
            fillTransform.localScale = newScale;

            // �������� ���� ����������
            if (fillRenderer != null)
            {
                fillRenderer.color = Color.Lerp(startColor, endColor, progress);
            }
        }
    }

    private IEnumerator FillAnimation()
    {
        if (fillTransform == null) yield break;

        // ��������� �������� "������" ��� ����������
        Vector3 originalScale = fillTransform.localScale;
        Vector3 pulseScale = originalScale * 1.1f;

        float duration = 0.15f;
        float timer = 0f;

        // �����������
        while (timer < duration)
        {
            timer += Time.deltaTime;
            fillTransform.localScale = Vector3.Lerp(originalScale, pulseScale, timer / duration);
            yield return null;
        }

        // ����������
        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            fillTransform.localScale = Vector3.Lerp(pulseScale, originalScale, timer / duration);
            yield return null;
        }

        fillTransform.localScale = originalScale;
    }

    private void UpdateConstructionSprite()
    {
        if (constructionSprites == null || constructionSprites.Length != 5)
        {
            Debug.LogWarning("�� ��������� ������� �������������!");
            return;
        }

        int stage = Mathf.FloorToInt((float)deliveredPlanks / requiredPlanks * 4);
        stage = Mathf.Clamp(stage, 0, 4);
        spriteRenderer.sprite = constructionSprites[stage];
    }

    private void CompleteConstruction()
    {
        isComplete = true;

        // ������������� ��������� ������
        if (constructionSprites != null && constructionSprites.Length >= 5)
        {
            spriteRenderer.sprite = constructionSprites[4];
        }

        // �������� ��������-��� � ���������
        StartCoroutine(HideProgressBar());

        // ������ ����������
        if (completeEffect != null)
        {
            completeEffect.Play();
        }

        Debug.Log("������ ���������!");
        isProgressBarActive = false;
    }

    private IEnumerator HideProgressBar()
    {
        Transform progressBarTransform = transform.Find("ConstructionProgressBar");
        if (progressBarTransform == null) yield break;

        // ������� ������������
        float fadeDuration = 1f;
        float timer = 0f;

        if (frameRenderer != null)
        {
            Color frameColor = frameRenderer.color;
        }

        if (fillRenderer != null)
        {
            Color fillColor = fillRenderer.color;
        }

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);

            if (frameRenderer != null)
                frameRenderer.color = new Color(1, 1, 1, alpha);

            if (fillRenderer != null)
                fillRenderer.color = new Color(fillRenderer.color.r, fillRenderer.color.g, fillRenderer.color.b, alpha);

            yield return null;
        }

        progressBarTransform.gameObject.SetActive(false);
    }

    // ������������ � ��������� ��� ������ � ���������
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            // ���������� ������� ��������-���� � ���������
            Gizmos.color = Color.yellow;
            Vector3 progressBarPos = transform.position + Vector3.up * 2f;
            Gizmos.DrawWireCube(progressBarPos, new Vector3(1, 0.3f, 0));
            Gizmos.DrawIcon(progressBarPos, "ProgressBar Icon");
        }
    }
}