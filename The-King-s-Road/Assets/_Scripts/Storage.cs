using UnityEngine;
using System.Collections;

public class Storage : Building
{
    public int planks = 5;
    public int maxPlanks = 5;

    [Header("Door Settings")]
    public Transform doorPoint; // Точка входа на склад, если задана.
    public Transform exitPoint; // Точка выхода со склада, если задана.
    public float interactionTime = 1f; // Время взаимодействия со складом.

    private bool isUnitInside = false;

    void Start()
    {
        base.Start();
        planks = Mathf.Clamp(planks, 0, maxPlanks);
    }

    public bool TakePlank()
    {
        if (planks > 0)
        {
            planks--;
            Debug.Log($"Доска взята со склада. Осталось: {planks}");
            return true;
        }
        return false;
    }

    public void ReturnPlank()
    {
        planks = Mathf.Min(planks + 1, maxPlanks);
        Debug.Log($"Доска возвращена на склад. Теперь досок: {planks}/{maxPlanks}");
    }

    public bool IsFull()
    {
        return planks >= maxPlanks;
    }

    public Vector3 GetDoorPosition()
    {
        return doorPoint != null ? doorPoint.position : transform.position;
    }

    public Vector3 GetExitPosition()
    {
        return exitPoint != null ? exitPoint.position : doorPoint != null ? doorPoint.position : transform.position;
    }

    public override void Interact(UnitAI unit, EntryPoint usedEntryPoint)
    {
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

    public IEnumerator EnterAndTakePlank(UnitAI unit, EntryPoint usedEntryPoint)
    {
        Debug.Log("=== Юнит входит на склад ===");

        isUnitInside = true;
        SetCollisionIgnoredForUnit(unit, true);

        Vector3 posBefore = unit.transform.position;
        Debug.Log($"Позиция юнита до входа: {posBefore}");

        SpriteRenderer unitRenderer = unit.GetComponent<SpriteRenderer>();
        Collider2D unitCollider = unit.GetComponent<Collider2D>();

        if (unitRenderer != null) unitRenderer.enabled = false;
        if (unitCollider != null) unitCollider.enabled = false;

        if (unit.plankVisual != null)
            unit.plankVisual.SetActive(false);

        Debug.Log($"Юнит скрыт внутри склада. Renderer: {unitRenderer?.enabled}, Collider: {unitCollider?.enabled}");

        Debug.Log($"Ожидание {interactionTime} сек...");
        unit.BeginWork();

        yield return new WaitForSeconds(interactionTime);

        unit.EndWork();

        if (!unit.CanWork)
        {
            if (unitRenderer != null) unitRenderer.enabled = true;
            if (unitCollider != null) unitCollider.enabled = true;
            SetCollisionIgnoredForUnit(unit, false);
            usedEntryPoint?.Vacate();
            isUnitInside = false;
            yield break;
        }

        Debug.Log("Ожидание завершено — пробуем взять доску");

        bool plankTaken = TakePlank();
        Debug.Log($"Доска взята: {plankTaken}");

        if (plankTaken)
        {
            unit.SetHasPlank(true);
        }

        if (exitPoint != null)
        {
            unit.transform.position = exitPoint.position;
            Debug.Log($"Юнит перемещен к выходу склада: {exitPoint.position}");
        }
        else
        {
            Debug.LogWarning("exitPoint не назначен, используем doorPoint");
            unit.transform.position = doorPoint.position;
        }

        if (unitRenderer != null) unitRenderer.enabled = true;
        if (unitCollider != null) unitCollider.enabled = true;

        if (unit.plankVisual != null && plankTaken)
            unit.plankVisual.SetActive(true);

        Debug.Log($"Юнит вышел со склада в позиции: {unit.transform.position}");

        usedEntryPoint?.Vacate();

        yield return new WaitForSeconds(0.3f);

        SetCollisionIgnoredForUnit(unit, false);

        Debug.Log($"=== Выход со склада завершен. Доска взята: {plankTaken} ===");
        isUnitInside = false;
    }

    public IEnumerator EnterAndStorePlank(UnitAI unit, EntryPoint usedEntryPoint)
    {
        Debug.Log("=== Юнит заносит доску на склад ===");

        isUnitInside = true;
        SetCollisionIgnoredForUnit(unit, true);

        SpriteRenderer unitRenderer = unit.GetComponent<SpriteRenderer>();
        Collider2D unitCollider = unit.GetComponent<Collider2D>();

        if (unitRenderer != null) unitRenderer.enabled = false;
        if (unitCollider != null) unitCollider.enabled = false;
        if (unit.plankVisual != null) unit.plankVisual.SetActive(false);

        unit.BeginWork();

        yield return new WaitForSeconds(interactionTime);

        unit.EndWork();

        if (!unit.CanWork)
        {
            if (unitRenderer != null) unitRenderer.enabled = true;
            if (unitCollider != null) unitCollider.enabled = true;
            SetCollisionIgnoredForUnit(unit, false);
            usedEntryPoint?.Vacate();
            isUnitInside = false;
            yield break;
        }

        ReturnPlank();
        unit.SetHasPlank(false);

        if (exitPoint != null)
        {
            unit.transform.position = exitPoint.position;
            Debug.Log($"Юнит перемещен к выходу склада: {exitPoint.position}");
        }
        else
        {
            unit.transform.position = GetDoorPosition();
        }

        if (unitRenderer != null) unitRenderer.enabled = true;
        if (unitCollider != null) unitCollider.enabled = true;

        usedEntryPoint?.Vacate();

        yield return new WaitForSeconds(0.3f);

        SetCollisionIgnoredForUnit(unit, false);

        Debug.Log("=== Доска успешно занесена на склад ===");
        isUnitInside = false;
    }
}
