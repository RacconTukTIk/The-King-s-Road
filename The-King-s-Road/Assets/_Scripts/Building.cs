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
        // ˜˜˜˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜ ˜˜˜ ˜˜˜˜˜ ˜˜˜˜˜ ˜˜˜˜˜ ˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜
        if (entryPoints == null || entryPoints.Count == 0)
        {
            entryPoints = new List<EntryPoint>(GetComponentsInChildren<EntryPoint>());
        }

        // ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜ ˜ ˜˜˜˜ ˜˜˜˜˜˜˜ (˜ ˜˜˜˜˜˜˜˜˜ ˜˜ null)
        foreach (var point in entryPoints)
        {
            if (point != null)  // <-- ˜˜˜˜˜: ˜˜˜˜˜˜˜˜˜ ˜˜ null
            {
                point.parentBuilding = this;
            }
        }
    }

    // ˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜ ˜˜˜˜˜
    public virtual EntryPoint GetNearestFreeEntryPoint(Vector3 unitPosition)
    {
        if (entryPoints == null || entryPoints.Count == 0)
            return null;

        EntryPoint nearest = null;
        float minDistance = float.MaxValue;

        foreach (var point in entryPoints)
        {
            if (point != null && !point.isOccupied)  // <-- ˜˜˜˜˜˜˜˜ ˜˜ null
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

    // ˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜ ˜˜˜˜˜
    public void VacateEntryPoint(EntryPoint point)
    {
        if (point != null)
        {
            point.Vacate();
        }
    }

    // ˜˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜ ˜˜˜ ˜˜˜˜˜˜˜˜˜˜˜˜˜˜
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
    /// Ïğîïóñê òîëüêî ıòîãî şíèòà ñêâîçü êîëëàéäåğû çäàíèÿ (êîëëàéäåğ çäàíèÿ îñòà¸òñÿ äëÿ îñòàëüíûõ).
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