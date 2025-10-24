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
    public Sprite[] progressBarFrames; // Ваши 10 PNG кадров
    public float frameRate = 10f; // Скорость анимации (кадров в секунду)

    [Header("Progress Bar - Fill Settings")]
    public Transform fillTransform; // Transform объекта Fill
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
        // Обновляем анимацию рамки если прогресс-бар активен
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
        // Находим компоненты Frame и Fill среди дочерних объектов
        Transform progressBarTransform = transform.Find("ConstructionProgressBar");

        if (progressBarTransform != null)
        {
            // Ищем Frame
            Transform frameTransform = progressBarTransform.Find("Frame");
            if (frameTransform != null)
            {
                frameRenderer = frameTransform.GetComponent<SpriteRenderer>();
            }

            // Ищем Fill
            Transform fillTransformObj = progressBarTransform.Find("Fill");
            if (fillTransformObj != null)
            {
                fillTransform = fillTransformObj;
                fillRenderer = fillTransform.GetComponent<SpriteRenderer>();

                // Изначально скрываем заполнение
                UpdateFillProgress(0f);
            }

            // Изначально скрываем весь прогресс-бар
            progressBarTransform.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Не найден объект ConstructionProgressBar среди дочерних объектов!");
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
        Debug.Log($"Доставлена доска: {deliveredPlanks}/{requiredPlanks}");

        // Активируем прогресс-бар при первой доставке
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

        // Дополнительные эффекты при изменении прогресса
        StartCoroutine(FillAnimation());
    }

    private void UpdateFillProgress(float progress)
    {
        if (fillTransform != null)
        {
            // Масштабируем заполнение по X от 0 до 1
            Vector3 newScale = fillTransform.localScale;
            newScale.x = progress;
            fillTransform.localScale = newScale;

            // Изменяем цвет заполнения
            if (fillRenderer != null)
            {
                fillRenderer.color = Color.Lerp(startColor, endColor, progress);
            }
        }
    }

    private IEnumerator FillAnimation()
    {
        if (fillTransform == null) yield break;

        // Небольшая анимация "пульса" при заполнении
        Vector3 originalScale = fillTransform.localScale;
        Vector3 pulseScale = originalScale * 1.1f;

        float duration = 0.15f;
        float timer = 0f;

        // Увеличиваем
        while (timer < duration)
        {
            timer += Time.deltaTime;
            fillTransform.localScale = Vector3.Lerp(originalScale, pulseScale, timer / duration);
            yield return null;
        }

        // Возвращаем
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
            Debug.LogWarning("Не настроены спрайты строительства!");
            return;
        }

        int stage = Mathf.FloorToInt((float)deliveredPlanks / requiredPlanks * 4);
        stage = Mathf.Clamp(stage, 0, 4);
        spriteRenderer.sprite = constructionSprites[stage];
    }

    private void CompleteConstruction()
    {
        isComplete = true;

        // Устанавливаем финальный спрайт
        if (constructionSprites != null && constructionSprites.Length >= 5)
        {
            spriteRenderer.sprite = constructionSprites[4];
        }

        // Скрываем прогресс-бар с анимацией
        StartCoroutine(HideProgressBar());

        // Эффект завершения
        if (completeEffect != null)
        {
            completeEffect.Play();
        }

        Debug.Log("Здание построено!");
        isProgressBarActive = false;
    }

    private IEnumerator HideProgressBar()
    {
        Transform progressBarTransform = transform.Find("ConstructionProgressBar");
        if (progressBarTransform == null) yield break;

        // Плавное исчезновение
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

    // Визуализация в редакторе для помощи в настройке
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            // Показываем позицию прогресс-бара в редакторе
            Gizmos.color = Color.yellow;
            Vector3 progressBarPos = transform.position + Vector3.up * 2f;
            Gizmos.DrawWireCube(progressBarPos, new Vector3(1, 0.3f, 0));
            Gizmos.DrawIcon(progressBarPos, "ProgressBar Icon");
        }
    }
}