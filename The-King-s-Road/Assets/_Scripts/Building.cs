using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public abstract class Building : MonoBehaviour
{
    [Header("Building Settings")]
    public List<EntryPoint> entryPoints = new List<EntryPoint>();
    public Collider2D buildingCollider;

    Collider2D[] cachedBuildingColliders;

    protected virtual void Awake()
    {
        CacheBuildingColliders();
    }

    protected virtual void Start()
    {
        // Инициализируем точки входа, если они не заданы в инспекторе.
        if (entryPoints == null || entryPoints.Count == 0)
        {
            entryPoints = new List<EntryPoint>(GetComponentsInChildren<EntryPoint>());
        }

        // Привязываем точки входа к этому зданию, пропуская null-объекты.
        foreach (var point in entryPoints)
        {
            if (point != null)
            {
                point.parentBuilding = this;
            }
        }
    }

    // Возвращает ближайшую свободную точку входа.
    public virtual EntryPoint GetNearestFreeEntryPoint(Vector3 unitPosition)
    {
        if (entryPoints == null || entryPoints.Count == 0)
            return null;

        EntryPoint nearest = null;
        float minDistance = float.MaxValue;

        foreach (var point in entryPoints)
        {
            if (point != null && !point.isOccupied)
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

    // Освобождает точку входа.
    public void VacateEntryPoint(EntryPoint point)
    {
        if (point != null)
        {
            point.Vacate();
        }
    }

    // Метод взаимодействия юнита со зданием.
    public abstract void Interact(UnitAI unit, EntryPoint usedEntryPoint);

    public virtual void RefreshEntryPoints()
    {
        foreach (var point in entryPoints)
        {
            if (point != null)
                point.parentBuilding = this;
        }
    }

    protected void CacheBuildingColliders()
    {
        cachedBuildingColliders = GetComponentsInChildren<Collider2D>(true)
            .Where(col => col != null)
            .ToArray();
    }

    /// <summary>
    /// Разрешает конкретному юниту проходить сквозь коллайдеры здания.
    /// Коллайдеры здания остаются активными для остальных объектов.
    /// </summary>
    public void SetCollisionIgnoredForUnit(Collider2D unitCollider, bool ignore)
    {
        if (unitCollider == null)
            return;

        if (cachedBuildingColliders == null || cachedBuildingColliders.Length == 0)
            CacheBuildingColliders();

        foreach (Collider2D buildingCol in cachedBuildingColliders)
        {
            if (buildingCol == null || buildingCol == unitCollider)
                continue;

            Physics2D.IgnoreCollision(unitCollider, buildingCol, ignore);
        }
    }

    public void SetCollisionIgnoredForUnit(UnitAI unit, bool ignore)
    {
        if (unit == null)
            return;

        SetCollisionIgnoredForUnit(unit.GetComponent<Collider2D>(), ignore);
    }
}
