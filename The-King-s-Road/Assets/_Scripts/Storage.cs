using UnityEngine;
using System.Collections;

public class Storage : Building  // Добавили : Building
{
    public int planks = 5;

    [Header("Door Settings")]
    public Transform doorPoint; // Точка у двери (куда подходить)
    public Transform exitPoint; // Точка выхода (откуда появляться)
    public float interactionTime = 1f; // Время "внутри" здания

    // buildingCollider уже есть в базовом классе Building!
    // public Collider2D buildingCollider; // Эту строку УДАЛИ!

    private bool isUnitInside = false;

    void Start()
    {
        base.Start(); // Вызываем базовый метод Building.Start()
    }

    public bool TakePlank()
    {
        if (planks > 0)
        {
            planks--;
            Debug.Log($"Взята доска со склада. Осталось: {planks}");
            return true;
        }
        return false;
    }

    public void ReturnPlank()
    {
        planks++;
        Debug.Log($"Доска возвращена на склад. Всего досок: {planks}");
    }

    public Vector3 GetDoorPosition()
    {
        return doorPoint != null ? doorPoint.position : transform.position;
    }

    public Vector3 GetExitPosition()
    {
        return exitPoint != null ? exitPoint.position : doorPoint != null ? doorPoint.position : transform.position;
    }

    // Реализуем абстрактный метод из Building
    public override void Interact(UnitAI unit, EntryPoint usedEntryPoint)
    {
        // Для склада используем старую логику, но с проверкой точки входа
        Debug.Log($"Storage.Interact вызван с точкой входа: {usedEntryPoint?.name}");

        if (usedEntryPoint != null)
        {
            StartCoroutine(EnterAndTakePlank(unit, usedEntryPoint));
        }
        else
        {
            Debug.LogError("Storage.Interact: usedEntryPoint = null!");
            unit.FindJob();
        }
    }

    // Юнит входит, берет доску и выходит
    public IEnumerator EnterAndTakePlank(UnitAI unit, EntryPoint usedEntryPoint)
    {
        Debug.Log("=== НАЧАЛО ВХОДА В СКЛАД ===");

        isUnitInside = true;

        // Отключаем коллайдер здания для входа
        if (buildingCollider != null)
        {
            buildingCollider.enabled = false;
            Debug.Log("Коллайдер склада ОТКЛЮЧЕН");
        }

        Vector3 posBefore = unit.transform.position;
        Debug.Log($"Позиция юнита ДО входа: {posBefore}");

        // ВАЖНО: Не отключаем GameObject, только рендерер и коллайдер
        SpriteRenderer unitRenderer = unit.GetComponent<SpriteRenderer>();
        Collider2D unitCollider = unit.GetComponent<Collider2D>();

        // Отключаем визуал и коллизию юнита
        if (unitRenderer != null) unitRenderer.enabled = false;
        if (unitCollider != null) unitCollider.enabled = false;

        // Прячем доску
        if (unit.plankVisual != null)
            unit.plankVisual.SetActive(false);

        Debug.Log($"Юнит НЕВИДИМ (рендерер: {unitRenderer?.enabled}, коллайдер: {unitCollider?.enabled})");

        // Ждем время нахождения "внутри"
        Debug.Log($"Ожидание {interactionTime} секунд...");
        yield return new WaitForSeconds(interactionTime);

        Debug.Log("ПРОШЛО ВРЕМЯ - ПРОДОЛЖАЕМ");

        // ВНУТРИ берем доску (пока юнит невидим)
        bool plankTaken = TakePlank();
        Debug.Log($"Доска взята: {plankTaken}");

        if (plankTaken)
        {
            unit.SetHasPlank(true);
        }

        // Выходим из здания - перемещаем к выходу
        if (exitPoint != null)
        {
            unit.transform.position = exitPoint.position;
            Debug.Log($"Юнит перемещен на выход: {exitPoint.position}");
        }
        else
        {
            Debug.LogWarning("exitPoint не назначен, использую doorPoint");
            unit.transform.position = doorPoint.position;
        }

        // Возвращаем видимость юниту
        if (unitRenderer != null) unitRenderer.enabled = true;
        if (unitCollider != null) unitCollider.enabled = true;

        // Показываем доску, если она есть
        if (unit.plankVisual != null && plankTaken)
            unit.plankVisual.SetActive(true);

        Debug.Log($"Юнит СНОВА ВИДИМ на позиции: {unit.transform.position}");

        // Освобождаем точку входа
        usedEntryPoint?.Vacate();

        // Небольшая задержка перед включением коллайдера здания
        yield return new WaitForSeconds(0.3f);

        // Включаем коллайдер здания обратно
        if (buildingCollider != null)
        {
            buildingCollider.enabled = true;
            Debug.Log("Коллайдер склада ВКЛЮЧЕН");
        }

        Debug.Log($"=== ВЫХОД ИЗ СКЛАДА ЗАВЕРШЕН. Доска взята: {plankTaken} ===");
        isUnitInside = false;
    }
}