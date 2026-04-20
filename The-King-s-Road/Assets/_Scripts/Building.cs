using UnityEngine;
using System.Collections.Generic;

public abstract class Building : MonoBehaviour
{
    [Header("Building Settings")]
    public List<EntryPoint> entryPoints = new List<EntryPoint>();
    public Collider2D buildingCollider;

    protected virtual void Start()
    {
        // Автоматически находим все точки входа среди дочерних объектов
        if (entryPoints == null || entryPoints.Count == 0)
        {
            entryPoints = new List<EntryPoint>(GetComponentsInChildren<EntryPoint>());
        }

        // Связываем точки с этим зданием (с проверкой на null)
        foreach (var point in entryPoints)
        {
            if (point != null)  // <-- ВАЖНО: проверяем на null
            {
                point.parentBuilding = this;
            }
        }
    }

    // Найти ближайшую свободную точку входа
    public virtual EntryPoint GetNearestFreeEntryPoint(Vector3 unitPosition)
    {
        if (entryPoints == null || entryPoints.Count == 0)
            return null;

        EntryPoint nearest = null;
        float minDistance = float.MaxValue;

        foreach (var point in entryPoints)
        {
            if (point != null && !point.isOccupied)  // <-- проверка на null
            {
                float distance = Vector3.Distance(unitPosition, point.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = point;
                }
            }
        }

        return nearest;
    }

    // Освободить точку входа
    public void VacateEntryPoint(EntryPoint point)
    {
        if (point != null)
        {
            point.Vacate();
        }
    }

    // Абстрактный метод для взаимодействия
    public abstract void Interact(UnitAI unit, EntryPoint usedEntryPoint);

    public virtual void RefreshEntryPoints()
    {
        foreach (var point in entryPoints)
        {
            if (point != null)
                point.parentBuilding = this;
        }
    }
}