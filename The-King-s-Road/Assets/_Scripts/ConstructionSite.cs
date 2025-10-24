using UnityEngine;
using System.Collections;

public class ConstructionSite : MonoBehaviour
{
    public int requiredPlanks = 10;
    public int deliveredPlanks = 0;
    public bool isComplete = false;

    [Header("Construction Sprites")]
    public Sprite[] constructionSprites; // 5 спрайтов для разных стадий строительства
    private SpriteRenderer spriteRenderer;

    [Header("Construction Effects")]
    public ParticleSystem buildEffect;
    public ParticleSystem completeEffect;

    [Header("Position Settings")]
    public Vector3[] spriteOffsets; // Смещения для каждого спрайта (если нужно)
    public bool useCustomOffsets = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Проверяем массив смещений
        if (useCustomOffsets && (spriteOffsets == null || spriteOffsets.Length != 5))
        {
            Debug.LogWarning("Не настроены смещения для спрайтов! Отключаем useCustomOffsets.");
            useCustomOffsets = false;
        }

        // Устанавливаем начальный спрайт (первую стадию)
        UpdateConstructionSprite();
    }

    public bool NeedsPlanks()
    {
        return !isComplete && deliveredPlanks < requiredPlanks;
    }

    public void DeliverPlank()
    {
        deliveredPlanks++;
        Debug.Log($"Доставлена доска на стройку: {deliveredPlanks}/{requiredPlanks}");

        // Обновляем спрайт при доставке каждой доски
        UpdateConstructionSprite();

        // Воспроизводим эффект строительства
        if (buildEffect != null)
        {
            buildEffect.Play();
        }

        if (deliveredPlanks >= requiredPlanks)
        {
            CompleteConstruction();
        }
    }

    private void UpdateConstructionSprite()
    {
        if (constructionSprites == null || constructionSprites.Length != 5)
        {
            Debug.LogWarning("Не настроены спрайты строительства! Нужно 5 спрайтов.");
            return;
        }

        // Определяем текущую стадию строительства (0-4)
        int stage = Mathf.FloorToInt((float)deliveredPlanks / requiredPlanks * 4);
        stage = Mathf.Clamp(stage, 0, 4);

        spriteRenderer.sprite = constructionSprites[stage];

        // Применяем смещение позиции если нужно
        if (useCustomOffsets)
        {
            ApplySpriteOffset(stage);
        }

        // Если это последняя стадия, но здание еще не завершено
        if (stage == 4 && !isComplete)
        {
            // Можно добавить временный спрайт "почти готово"
            // или оставить как есть - последний спрайт будет отображаться при завершении
        }
    }

    private void ApplySpriteOffset(int stage)
    {
        // Сохраняем оригинальную позицию и применяем смещение
        Vector3 originalPosition = transform.position;

        // Если это первый спрайт, запоминаем оригинальную позицию как базовую
        if (stage == 0 && !isComplete)
        {
            // Можно сохранить базовую позицию если нужно
        }

        // Применяем смещение для текущей стадии
        transform.position = originalPosition + spriteOffsets[stage];
    }

    private void CompleteConstruction()
    {
        isComplete = true;

        // Устанавливаем финальный спрайт (последний в массиве)
        if (constructionSprites != null && constructionSprites.Length >= 5)
        {
            spriteRenderer.sprite = constructionSprites[4];

            // Применяем финальное смещение если используется
            if (useCustomOffsets)
            {
                ApplySpriteOffset(4);
            }
        }

        // Эффект завершения строительства
        if (completeEffect != null)
        {
            completeEffect.Play();
        }

        Debug.Log("Здание построено!");

        // Отключаем или удаляем компонент стройки, если больше не нужен
        // StartCoroutine(RemoveConstructionComponent());
    }

    // Опционально: удаляем компонент строительства через некоторое время
    private System.Collections.IEnumerator RemoveConstructionComponent()
    {
        yield return new WaitForSeconds(2f);

        // Переименовываем объект (убираем "строящийся")
        gameObject.name = gameObject.name.Replace("(Строится)", "");

        // Удаляем этот скрипт, так как строительство завершено
        Destroy(this);
    }

    // Визуализация прогресса в редакторе (опционально)
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Рисуем прогресс-бар над зданием
        Vector3 startPos = transform.position + Vector3.up * 1.5f;
        float progress = (float)deliveredPlanks / requiredPlanks;

        // Фон прогресс-бара
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(startPos + Vector3.right * 0.5f, new Vector3(1, 0.2f, 0));

        // Заполнение прогресс-бара
        Gizmos.color = Color.Lerp(Color.red, Color.green, progress);
        Gizmos.DrawCube(startPos + Vector3.right * (progress * 0.5f),
                       new Vector3(progress, 0.15f, 0));
    }
}