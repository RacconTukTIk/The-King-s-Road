using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ConstructionSite : Building
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

    [Header("Progress Bar Settings")]
    public Transform progressBarParent;
    public Sprite[] progressBarFrames;
    public float frameRate = 10f;
    public Color startColor = Color.red;
    public Color endColor = Color.green;

    private SpriteRenderer frameRenderer;
    private SpriteRenderer fillRenderer;
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private bool isProgressBarActive = false;

    void Start()
    {
        // Вызываем базовый метод Building.Start()
        base.Start();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Автоматически находим все точки входа среди дочерних объектов (если не заданы)
        if (entryPoints == null || entryPoints.Count == 0)
        {
            entryPoints = new List<EntryPoint>(GetComponentsInChildren<EntryPoint>());
        }

        // Связываем точки с этим зданием
        foreach (var point in entryPoints)
        {
            if (point != null)
                point.parentBuilding = this;
        }

        InitializeProgressBar();
        UpdateConstructionSprite();
    }

    void Update()
    {
        if (isProgressBarActive && progressBarFrames != null && progressBarFrames.Length > 0)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / frameRate)
            {
                frameTimer = 0f;
                currentFrame = (currentFrame + 1) % progressBarFrames.Length;
                if (frameRenderer != null)
                    frameRenderer.sprite = progressBarFrames[currentFrame];
            }
        }
    }

    private void InitializeProgressBar()
    {
        if (progressBarParent == null)
        {
            Transform barTransform = transform.Find("ConstructionProgressBar");
            if (barTransform != null)
                progressBarParent = barTransform;
        }

        if (progressBarParent != null)
        {
            Transform frameTransform = progressBarParent.Find("Frame");
            if (frameTransform != null)
                frameRenderer = frameTransform.GetComponent<SpriteRenderer>();

            Transform fillTransform = progressBarParent.Find("Fill");
            if (fillTransform != null)
            {
                fillRenderer = fillTransform.GetComponent<SpriteRenderer>();
                UpdateFillProgress(0f);
            }

            progressBarParent.gameObject.SetActive(false);
        }
    }

    public bool NeedsPlanks()
    {
        return !isComplete && deliveredPlanks < requiredPlanks;
    }

    // Переопределяем метод из Building для поиска ближайшей свободной точки входа
    public override EntryPoint GetNearestFreeEntryPoint(Vector3 unitPosition)
    {
        if (entryPoints == null || entryPoints.Count == 0)
            return null;

        // Сортируем точки по расстоянию и выбираем первую свободную
        var freePoints = entryPoints.Where(p => p != null && !p.isOccupied).ToList();

        if (freePoints.Count == 0)
            return null;

        // Находим ближайшую
        EntryPoint nearest = null;
        float minDistance = float.MaxValue;

        foreach (var point in freePoints)
        {
            float distance = Vector3.Distance(unitPosition, point.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = point;
            }
        }

        return nearest;
    }

    // Основной метод для взаимодействия (вызывается из UnitAI)
    public override void Interact(UnitAI unit, EntryPoint usedEntryPoint)
    {
        if (unit == null)
        {
            usedEntryPoint?.Vacate();
            return;
        }

        // Сначала проверяем, есть ли доска у юнита
        if (!unit.HasPlank)
        {
            Debug.LogWarning($"Юнит {unit.name} пришёл на стройку без доски!");
            usedEntryPoint?.Vacate();
            unit.FindJob();
            return;
        }

        if (isComplete)
        {
            Debug.Log("Стройка уже завершена");
            usedEntryPoint?.Vacate();
            unit.FindJob();
            return;
        }

        // Если все проверки пройдены - принимаем доску
        StartCoroutine(DeliverPlankRoutine(unit, usedEntryPoint));
    }

    private IEnumerator DeliverPlankRoutine(UnitAI unit, EntryPoint usedEntryPoint)
    {
        // Защита от повторных вызовов
        if (unit == null || usedEntryPoint == null)
        {
            Debug.LogError("DeliverPlankRoutine: unit или usedEntryPoint null");
            yield break;
        }

        // Дополнительная проверка наличия доски
        if (!unit.HasPlank)
        {
            Debug.LogError($"DeliverPlankRoutine: У юнита {unit.name} нет доски в начале корутины!");
            usedEntryPoint?.Vacate();
            unit.FindJob();
            yield break;
        }

        Debug.Log($"DeliverPlankRoutine: Юнит {unit.name} начал сдачу доски. Доска есть: {unit.HasPlank}");

        // Небольшая пауза для анимации сдачи доски
        yield return new WaitForSeconds(0.3f);

        // Проверяем, не завершена ли стройка
        if (deliveredPlanks >= requiredPlanks)
        {
            Debug.Log("Стройка уже завершена, доска не принята");
            usedEntryPoint?.Vacate();
            unit.FindJob();
            yield break;
        }

        // Проверяем, есть ли доска после паузы
        if (!unit.HasPlank)
        {
            Debug.LogError($"DeliverPlankRoutine: У юнита {unit.name} пропала доска во время паузы!");
            usedEntryPoint?.Vacate();
            unit.FindJob();
            yield break;
        }

        // Принимаем доску
        deliveredPlanks++;
        unit.SetHasPlank(false); // Убираем доску у юнита
        Debug.Log($"Доставлена доска: {deliveredPlanks}/{requiredPlanks} от юнита {unit.name}");

        // Визуальные эффекты
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

        // Проверка завершения стройки
        bool completedNow = false;
        if (deliveredPlanks >= requiredPlanks)
        {
            completedNow = true;
            CompleteConstruction();
        }

        // Освобождаем точку входа
        usedEntryPoint?.Vacate();

        // Определяем дальнейшие действия юнита
        if (!completedNow && NeedsPlanks())
        {
            Debug.Log($"Юнит {unit.name} идет за следующей доской");
            Storage storage = FindObjectOfType<Storage>();
            if (storage != null)
            {
                unit.GoToStorage(storage);
            }
            else
            {
                unit.FindJob();
            }
        }
        else
        {
            Debug.Log($"Юнит {unit.name} ищет новую работу");
            unit.FindJob();
        }
    }

    private IEnumerator CompleteAndRelease(UnitAI unit, EntryPoint usedEntryPoint)
    {
        usedEntryPoint?.Vacate();
        if (unit != null)
            unit.FindJob();
        yield return null;
    }

    private void ShowProgressBar()
    {
        if (progressBarParent != null)
        {
            progressBarParent.gameObject.SetActive(true);
            isProgressBarActive = true;
        }
    }

    private void UpdateProgressBar()
    {
        float progress = (float)deliveredPlanks / requiredPlanks;
        Debug.Log($"Progress: {deliveredPlanks}/{requiredPlanks} = {progress}");
        UpdateFillProgress(progress);
        StartCoroutine(FillAnimation());
    }

    private void UpdateFillProgress(float progress)
    {
        if (fillRenderer != null)
        {
            Vector3 newScale = fillRenderer.transform.localScale;
            newScale.x = Mathf.Clamp01(progress);
            fillRenderer.transform.localScale = newScale;

            fillRenderer.color = Color.Lerp(startColor, endColor, progress);
        }
    }

    private IEnumerator FillAnimation()
    {
        if (fillRenderer == null) yield break;

        Vector3 originalScale = fillRenderer.transform.localScale;
        Vector3 pulseScale = originalScale * 1.1f;

        float duration = 0.15f;
        float timer = 0f;

        // Увеличиваем
        while (timer < duration)
        {
            timer += Time.deltaTime;
            fillRenderer.transform.localScale = Vector3.Lerp(originalScale, pulseScale, timer / duration);
            yield return null;
        }

        // Возвращаем
        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            fillRenderer.transform.localScale = Vector3.Lerp(pulseScale, originalScale, timer / duration);
            yield return null;
        }

        fillRenderer.transform.localScale = originalScale;
    }

    private void UpdateConstructionSprite()
    {
        if (constructionSprites == null || constructionSprites.Length == 0)
        {
            return;
        }

        int stage = Mathf.FloorToInt((float)deliveredPlanks / requiredPlanks * (constructionSprites.Length - 1));
        stage = Mathf.Clamp(stage, 0, constructionSprites.Length - 1);

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = constructionSprites[stage];
        }
    }

    private void CompleteConstruction()
    {
        isComplete = true;

        if (constructionSprites != null && constructionSprites.Length > 0)
        {
            spriteRenderer.sprite = constructionSprites[constructionSprites.Length - 1];
        }

        StartCoroutine(HideProgressBar());

        if (completeEffect != null)
        {
            completeEffect.Play();
        }

        Debug.Log("Здание построено!");
        isProgressBarActive = false;
    }

    private IEnumerator HideProgressBar()
    {
        if (progressBarParent == null) yield break;

        float fadeDuration = 1f;
        float timer = 0f;

        Color frameStartColor = frameRenderer != null ? frameRenderer.color : Color.white;
        Color fillStartColor = fillRenderer != null ? fillRenderer.color : Color.white;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);

            if (frameRenderer != null)
                frameRenderer.color = new Color(frameStartColor.r, frameStartColor.g, frameStartColor.b, alpha);

            if (fillRenderer != null)
                fillRenderer.color = new Color(fillStartColor.r, fillStartColor.g, fillStartColor.b, alpha);

            yield return null;
        }

        progressBarParent.gameObject.SetActive(false);
    }

    // Визуализация в редакторе
    private void OnDrawGizmos()
    {
        // Рисуем точки входа
        if (entryPoints != null)
        {
            foreach (var point in entryPoints)
            {
                if (point != null)
                {
                    Gizmos.color = point.isOccupied ? Color.red : Color.green;
                    Gizmos.DrawSphere(point.transform.position, 0.2f);
                }
            }
        }

        // Рисуем позицию прогресс-бара
        if (progressBarParent != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 barPos = progressBarParent.position;
            Gizmos.DrawWireCube(barPos, new Vector3(1f, 0.3f, 0f));
        }
        else
        {
            Gizmos.color = Color.yellow;
            Vector3 barPos = transform.position + Vector3.up * 2f;
            Gizmos.DrawWireCube(barPos, new Vector3(1f, 0.3f, 0f));
            Gizmos.DrawIcon(barPos, "ProgressBar Icon", true);
        }
    }
}