using UnityEngine;
using System.Collections;

public class Storage : Building
{
    public int planks = 5;
    public int maxPlanks = 5;

    [Header("Door Settings")]
    public Transform doorPoint; // ˜˜˜˜˜ ˜ ˜˜˜˜˜ (˜˜˜˜ ˜˜˜˜˜˜˜˜˜)
    public Transform exitPoint; // ˜˜˜˜˜ ˜˜˜˜˜˜ (˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜˜)
    public float interactionTime = 1f; // ˜˜˜˜˜ "˜˜˜˜˜˜" ˜˜˜˜˜˜

    // buildingCollider ˜˜˜ ˜˜˜˜ ˜ ˜˜˜˜˜˜˜ ˜˜˜˜˜˜ Building!
    // public Collider2D buildingCollider; // ˜˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜!

    private bool isUnitInside = false;

    void Start()
    {
        base.Start(); // ˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜ ˜˜˜˜˜ Building.Start()
        planks = Mathf.Clamp(planks, 0, maxPlanks);
    }

    public bool TakePlank()
    {
        if (planks > 0)
        {
            planks--;
            Debug.Log($"˜˜˜˜˜ ˜˜˜˜˜ ˜˜ ˜˜˜˜˜˜. ˜˜˜˜˜˜˜˜: {planks}");
            return true;
        }
        return false;
    }

    public void ReturnPlank()
    {
        planks = Mathf.Min(planks + 1, maxPlanks);
        Debug.Log($"˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜˜ ˜˜ ˜˜˜˜˜. ˜˜˜˜˜ ˜˜˜˜˜: {planks}/{maxPlanks}");
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

    // ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜ ˜˜ Building
    public override void Interact(UnitAI unit, EntryPoint usedEntryPoint)
    {
        // ˜˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜˜, ˜˜ ˜ ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜ ˜˜˜˜˜
        Debug.Log($"Storage.Interact ˜˜˜˜˜˜ ˜ ˜˜˜˜˜˜ ˜˜˜˜˜: {usedEntryPoint?.name}");

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

    // ˜˜˜˜ ˜˜˜˜˜˜, ˜˜˜˜˜ ˜˜˜˜˜ ˜ ˜˜˜˜˜˜˜
    public IEnumerator EnterAndTakePlank(UnitAI unit, EntryPoint usedEntryPoint)
    {
        Debug.Log("=== ˜˜˜˜˜˜ ˜˜˜˜˜ ˜ ˜˜˜˜˜ ===");

        isUnitInside = true;

        // ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜˜˜ ˜˜˜˜˜
        if (buildingCollider != null)
        {
            buildingCollider.enabled = false;
            Debug.Log("˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜");
        }

        Vector3 posBefore = unit.transform.position;
        Debug.Log($"˜˜˜˜˜˜˜ ˜˜˜˜˜ ˜˜ ˜˜˜˜˜: {posBefore}");

        // ˜˜˜˜˜: ˜˜ ˜˜˜˜˜˜˜˜˜ GameObject, ˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜ ˜ ˜˜˜˜˜˜˜˜˜
        SpriteRenderer unitRenderer = unit.GetComponent<SpriteRenderer>();
        Collider2D unitCollider = unit.GetComponent<Collider2D>();

        // ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜ ˜˜˜˜˜˜˜˜ ˜˜˜˜˜
        if (unitRenderer != null) unitRenderer.enabled = false;
        if (unitCollider != null) unitCollider.enabled = false;

        // ˜˜˜˜˜˜ ˜˜˜˜˜
        if (unit.plankVisual != null)
            unit.plankVisual.SetActive(false);

        Debug.Log($"˜˜˜˜ ˜˜˜˜˜˜˜ (˜˜˜˜˜˜˜˜: {unitRenderer?.enabled}, ˜˜˜˜˜˜˜˜˜: {unitCollider?.enabled})");

        // ˜˜˜˜ ˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜˜ "˜˜˜˜˜˜"
        Debug.Log($"˜˜˜˜˜˜˜˜ {interactionTime} ˜˜˜˜˜˜...");
        yield return new WaitForSeconds(interactionTime);

        Debug.Log("˜˜˜˜˜˜ ˜˜˜˜˜ - ˜˜˜˜˜˜˜˜˜˜");

        // ˜˜˜˜˜˜ ˜˜˜˜˜ ˜˜˜˜˜ (˜˜˜˜ ˜˜˜˜ ˜˜˜˜˜˜˜)
        bool plankTaken = TakePlank();
        Debug.Log($"˜˜˜˜˜ ˜˜˜˜˜: {plankTaken}");

        if (plankTaken)
        {
            unit.SetHasPlank(true);
        }

        // ˜˜˜˜˜˜˜ ˜˜ ˜˜˜˜˜˜ - ˜˜˜˜˜˜˜˜˜˜ ˜ ˜˜˜˜˜˜
        if (exitPoint != null)
        {
            unit.transform.position = exitPoint.position;
            Debug.Log($"˜˜˜˜ ˜˜˜˜˜˜˜˜˜ ˜˜ ˜˜˜˜˜: {exitPoint.position}");
        }
        else
        {
            Debug.LogWarning("exitPoint ˜˜ ˜˜˜˜˜˜˜˜, ˜˜˜˜˜˜˜˜˜ doorPoint");
            unit.transform.position = doorPoint.position;
        }

        // ˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜
        if (unitRenderer != null) unitRenderer.enabled = true;
        if (unitCollider != null) unitCollider.enabled = true;

        // ˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜, ˜˜˜˜ ˜˜˜ ˜˜˜˜
        if (unit.plankVisual != null && plankTaken)
            unit.plankVisual.SetActive(true);

        Debug.Log($"˜˜˜˜ ˜˜˜˜˜ ˜˜˜˜˜ ˜˜ ˜˜˜˜˜˜˜: {unit.transform.position}");

        // ˜˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜ ˜˜˜˜˜
        usedEntryPoint?.Vacate();

        // ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜ ˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜
        yield return new WaitForSeconds(0.3f);

        // ˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜˜˜
        if (buildingCollider != null)
        {
            buildingCollider.enabled = true;
            Debug.Log("˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜˜˜");
        }

        Debug.Log($"=== ˜˜˜˜˜ ˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜. ˜˜˜˜˜ ˜˜˜˜˜: {plankTaken} ===");
        isUnitInside = false;
    }

    // ˜˜˜˜ ˜˜˜˜˜˜ ˜ ˜˜˜˜˜ ˜˜˜˜˜ ˜˜ ˜˜˜˜˜
    public IEnumerator EnterAndStorePlank(UnitAI unit, EntryPoint usedEntryPoint)
    {
        Debug.Log("=== ˜˜˜˜˜˜ ˜˜˜˜˜ ˜˜˜˜˜ ˜˜ ˜˜˜˜˜ ===");

        isUnitInside = true;

        if (buildingCollider != null)
        {
            buildingCollider.enabled = false;
            Debug.Log("˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜");
        }

        SpriteRenderer unitRenderer = unit.GetComponent<SpriteRenderer>();
        Collider2D unitCollider = unit.GetComponent<Collider2D>();

        if (unitRenderer != null) unitRenderer.enabled = false;
        if (unitCollider != null) unitCollider.enabled = false;
        if (unit.plankVisual != null) unit.plankVisual.SetActive(false);

        yield return new WaitForSeconds(interactionTime);

        ReturnPlank();
        unit.SetHasPlank(false);

        if (exitPoint != null)
        {
            unit.transform.position = exitPoint.position;
            Debug.Log($"˜˜˜˜ ˜˜˜˜˜˜˜˜˜ ˜˜ ˜˜˜˜˜: {exitPoint.position}");
        }
        else
        {
            unit.transform.position = GetDoorPosition();
        }

        if (unitRenderer != null) unitRenderer.enabled = true;
        if (unitCollider != null) unitCollider.enabled = true;

        usedEntryPoint?.Vacate();

        yield return new WaitForSeconds(0.3f);

        if (buildingCollider != null)
        {
            buildingCollider.enabled = true;
            Debug.Log("˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜˜˜");
        }

        Debug.Log("=== ˜˜˜˜˜ ˜˜˜˜˜ ˜˜ ˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜ ===");
        isUnitInside = false;
    }
}