using UnityEngine;
using System.Collections;

// Базовый класс для всех функциональных зданий (склады, таверны, дома и т.д.)
public abstract class FunctionalBuilding : Building
{
    [Header("Functional Building Settings")]
    public string buildingName = "Здание";
    public Sprite icon;

    // Этот метод будет вызываться при завершении строительства
    public virtual void OnConstructionComplete()
    {
        Debug.Log($"{buildingName} построен и готов к работе!");
    }

    // Абстрактный метод для взаимодействия с юнитами (должен быть реализован в наследниках)
    public abstract override void Interact(UnitAI unit, EntryPoint usedEntryPoint);
}