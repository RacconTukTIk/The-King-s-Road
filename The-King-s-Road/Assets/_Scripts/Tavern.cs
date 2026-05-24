using UnityEngine;
using System.Collections;

public class Tavern : FunctionalBuilding
{
    [Header("Tavern Settings")]
    public int maxCustomers = 5;
    public float restTime = 3f;              // Время отдыха в таверне
    public float staminaRestorePerSecond = 50f; // Восстановление выносливости в секунду

    private bool isOccupied = false;

    void Start()
    {
        Debug.Log($"Таверна {buildingName} открыта!");
    }

    public Vector3 GetDoorPosition()
    {
        EntryPoint entry = GetNearestFreeEntryPoint(transform.position);
        if (entry != null)
            return entry.transform.position;

        if (entryPoints != null && entryPoints.Count > 0 && entryPoints[0] != null)
            return entryPoints[0].transform.position;

        return transform.position;
    }

    public override void OnConstructionComplete()
    {
        base.OnConstructionComplete();
        Debug.Log("Таверна построена! Теперь юниты могут отдыхать здесь.");
    }

    public override void Interact(UnitAI unit, EntryPoint usedEntryPoint)
    {
        Debug.Log($"{unit.name} зашел в таверну");

        if (isOccupied)
        {
            Debug.Log("Таверна занята другим посетителем, подождите...");
            usedEntryPoint?.Vacate();
            if (unit.IsExhausted)
                unit.RequestRestAtPlace();
            else
                unit.FindJob();
            return;
        }

        StartCoroutine(RestInTavern(unit, usedEntryPoint));
    }

    private IEnumerator RestInTavern(UnitAI unit, EntryPoint usedEntryPoint)
    {
        isOccupied = true;
        unit.BeginTavernRest(this);

        SetCollisionIgnoredForUnit(unit, true);

        SpriteRenderer unitRenderer = unit.GetComponent<SpriteRenderer>();
        Collider2D unitCollider = unit.GetComponent<Collider2D>();
        if (unitRenderer != null) unitRenderer.enabled = false;
        if (unitCollider != null) unitCollider.enabled = false;
        if (unit.plankVisual != null) unit.plankVisual.SetActive(false);

        Debug.Log($"{unit.name} отдыхает в таверне");

        float elapsed = 0f;
        while (elapsed < restTime && unit.currentStamina < unit.maxStamina)
        {
            elapsed += Time.deltaTime;
            unit.RestoreStamina(staminaRestorePerSecond * Time.deltaTime);
            yield return null;
        }

        if (unitRenderer != null) unitRenderer.enabled = true;
        if (unitCollider != null) unitCollider.enabled = true;

        SetCollisionIgnoredForUnit(unit, false);

        usedEntryPoint?.Vacate();
        isOccupied = false;

        Debug.Log($"{unit.name} отдохнул в таверне и готов работать");
        unit.CompleteRest();
    }
}