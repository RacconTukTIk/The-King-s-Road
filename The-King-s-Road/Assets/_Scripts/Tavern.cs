using UnityEngine;

public class Tavern : FunctionalBuilding
{
    [Header("Tavern Settings")]
    public int maxCustomers = 5;
    public float restSpeed = 1f;
    public float hungerRestore = 50f;

    void Start()
    {
        Debug.Log($"Таверна {buildingName} открыта!");
    }

    public override void OnConstructionComplete()
    {
        base.OnConstructionComplete();
        Debug.Log("Таверна построена! Теперь юниты могут отдыхать здесь.");
    }

    public override void Interact(UnitAI unit, EntryPoint usedEntryPoint)
    {
        Debug.Log($"{unit.name} зашел в таверну");

        // Здесь будет логика:
        // - Восстановление усталости
        // - Утоление голода
        // - Повышение настроения

        // Пока просто логируем
        Debug.Log($"{unit.name} отдыхает в таверне");

        usedEntryPoint?.Vacate();
        unit.FindJob();
    }
}