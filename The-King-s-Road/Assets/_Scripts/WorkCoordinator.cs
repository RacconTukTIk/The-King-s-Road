using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorkCoordinator : MonoBehaviour
{
    public static WorkCoordinator Instance { get; private set; }

    public enum WorkRole
    {
        Unassigned,
        DeliverToConstruction,
        SawmillWorker,
        SawmillToStorage
    }

    [Header("Coordination")]
    public int minUnitsForRoleSplit = 2;

    private readonly Dictionary<UnitAI, WorkRole> rolesByUnit = new Dictionary<UnitAI, WorkRole>();
    private readonly Dictionary<UnitAI, WorkRole> committedRoles = new Dictionary<UnitAI, WorkRole>();
    private UnitAI activeDeliveryCourier;
    private static readonly object deliveryLock = new object();
    private int lastRefreshFrame = -1;
    private Coroutine spawnAssignmentRoutine;

    static readonly WorkRole[] RoleAssignPriority =
    {
        WorkRole.SawmillToStorage,
        WorkRole.DeliverToConstruction,
        WorkRole.SawmillWorker
    };

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        WorkCoordinator existing = FindObjectOfType<WorkCoordinator>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        GameObject coordinatorObject = new GameObject("WorkCoordinator");
        Instance = coordinatorObject.AddComponent<WorkCoordinator>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static bool ShouldCoordinate()
    {
        EnsureExists();
        return Instance != null && Instance.CountAvailableUnits() >= Instance.minUnitsForRoleSplit;
    }

    public static void ScheduleSpawnJobAssignment()
    {
        EnsureExists();
        if (Instance == null)
            return;

        if (Instance.spawnAssignmentRoutine != null)
            Instance.StopCoroutine(Instance.spawnAssignmentRoutine);

        Instance.spawnAssignmentRoutine = Instance.StartCoroutine(Instance.AssignJobsWhenAllSpawned());
    }

    IEnumerator AssignJobsWhenAllSpawned()
    {
        yield return null;

        float deadline = Time.realtimeSinceStartup + 2.5f;
        int lastCount = 0;
        int stableChecks = 0;

        while (Time.realtimeSinceStartup < deadline)
        {
            int count = CountAvailableUnits();
            if (count == lastCount && count >= minUnitsForRoleSplit)
                stableChecks++;
            else
                stableChecks = 0;

            lastCount = count;

            if (stableChecks >= 2)
                break;

            yield return new WaitForSeconds(0.15f);
        }

        AssignRolesAndStartWork();
        spawnAssignmentRoutine = null;
    }

    void AssignRolesAndStartWork()
    {
        RefreshRoleAssignments();

        List<UnitAI> units = GetAvailableUnits().OrderBy(u => u.GetInstanceID()).ToList();
        foreach (UnitAI unit in units)
        {
            if (unit == null)
                continue;

            if (rolesByUnit.TryGetValue(unit, out WorkRole role))
                unit.BeginCoordinatedRole(role);
            else
                unit.FindJob();
        }
    }

    public WorkRole GetRole(UnitAI unit)
    {
        if (unit == null || !ShouldCoordinate())
            return WorkRole.Unassigned;

        if (committedRoles.TryGetValue(unit, out WorkRole committed) && committed != WorkRole.Unassigned)
            return committed;

        RefreshRoleAssignments();
        return rolesByUnit.TryGetValue(unit, out WorkRole role) ? role : WorkRole.Unassigned;
    }

    public void ReleaseUnit(UnitAI unit)
    {
        if (unit == null)
            return;

        rolesByUnit.Remove(unit);
        committedRoles.Remove(unit);
        ReleaseDelivery(unit);
    }

    public void CommitRole(UnitAI unit, WorkRole role)
    {
        if (unit == null || role == WorkRole.Unassigned)
            return;

        committedRoles[unit] = role;
        rolesByUnit[unit] = role;
    }

    public void ReleaseCommittedRole(UnitAI unit)
    {
        if (unit == null)
            return;

        committedRoles.Remove(unit);
    }

    public WorkRole GetCommittedRole(UnitAI unit)
    {
        if (unit == null)
            return WorkRole.Unassigned;

        return committedRoles.TryGetValue(unit, out WorkRole role) ? role : WorkRole.Unassigned;
    }

    public bool TryClaimDelivery(UnitAI unit)
    {
        if (unit == null)
            return false;

        lock (deliveryLock)
        {
            if (activeDeliveryCourier != null && activeDeliveryCourier != unit)
                return false;

            activeDeliveryCourier = unit;
            return true;
        }
    }

    public void ReleaseDelivery(UnitAI unit)
    {
        lock (deliveryLock)
        {
            if (unit != null && activeDeliveryCourier == unit)
                activeDeliveryCourier = null;
        }
    }

    public bool IsActiveDeliveryCourier(UnitAI unit)
    {
        return unit != null && activeDeliveryCourier == unit;
    }

    public bool ShouldDepositAtSawmill(UnitAI unit)
    {
        return ShouldCoordinate()
               && CountAvailableUnits() >= 3
               && GetRole(unit) == WorkRole.SawmillWorker;
    }

    void RefreshRoleAssignments()
    {
        if (Time.frameCount == lastRefreshFrame)
            return;

        lastRefreshFrame = Time.frameCount;

        List<UnitAI> units = GetAvailableUnits();
        if (units.Count < minUnitsForRoleSplit)
        {
            rolesByUnit.Clear();
            return;
        }

        List<WorkRole> neededRoles = ComputeNeededRoles(units.Count);
        HashSet<WorkRole> neededSet = new HashSet<WorkRole>(neededRoles);

        List<UnitAI> toRemove = rolesByUnit
            .Where(pair =>
                pair.Key == null ||
                !units.Contains(pair.Key) ||
                (!committedRoles.ContainsKey(pair.Key) &&
                 !neededSet.Contains(pair.Value) &&
                 !IsUnitBusyWithRole(pair.Key, pair.Value)))
            .Select(pair => pair.Key)
            .ToList();

        foreach (UnitAI unit in toRemove)
        {
            if (rolesByUnit.TryGetValue(unit, out WorkRole role) && role == WorkRole.DeliverToConstruction)
                ReleaseDelivery(unit);

            rolesByUnit.Remove(unit);
        }

        EnforceDeliveryCourierRole(neededSet);

        foreach (WorkRole role in RoleAssignPriority)
        {
            if (!neededSet.Contains(role) || IsRoleTaken(role))
                continue;

            UnitAI candidate = PickUnitForRole(units, role);

            if (candidate != null)
                rolesByUnit[candidate] = role;
        }

        EnforceDeliveryCourierRole(neededSet);

        if (activeDeliveryCourier != null && !rolesByUnit.ContainsValue(WorkRole.DeliverToConstruction))
            activeDeliveryCourier = null;
    }

    UnitAI PickUnitForRole(List<UnitAI> units, WorkRole role)
    {
        if (role == WorkRole.DeliverToConstruction)
        {
            lock (deliveryLock)
            {
                if (activeDeliveryCourier != null && units.Contains(activeDeliveryCourier))
                    return activeDeliveryCourier;
            }
        }

        return units
            .Where(u => !rolesByUnit.ContainsKey(u))
            .Where(u => !IsDedicatedDeliveryCourier(u))
            .OrderBy(u => u.GetInstanceID())
            .FirstOrDefault();
    }

    bool IsDedicatedDeliveryCourier(UnitAI unit)
    {
        if (unit == null)
            return false;

        lock (deliveryLock)
        {
            if (activeDeliveryCourier == unit)
                return true;
        }

        return committedRoles.TryGetValue(unit, out WorkRole role)
               && role == WorkRole.DeliverToConstruction;
    }

    void EnforceDeliveryCourierRole(HashSet<WorkRole> neededSet)
    {
        if (!neededSet.Contains(WorkRole.DeliverToConstruction))
            return;

        lock (deliveryLock)
        {
            if (activeDeliveryCourier == null)
                return;

            List<UnitAI> othersWithDeliver = rolesByUnit
                .Where(pair => pair.Value == WorkRole.DeliverToConstruction && pair.Key != activeDeliveryCourier)
                .Select(pair => pair.Key)
                .ToList();

            foreach (UnitAI unit in othersWithDeliver)
                rolesByUnit.Remove(unit);

            rolesByUnit[activeDeliveryCourier] = WorkRole.DeliverToConstruction;
        }
    }

    bool IsUnitBusyWithRole(UnitAI unit, WorkRole role)
    {
        if (unit == null)
            return false;

        switch (role)
        {
            case WorkRole.DeliverToConstruction:
                return IsDedicatedDeliveryCourier(unit)
                       || unit.HasPlank
                       || unit.currentState == UnitAI.UnitState.MovingToStorage
                       || unit.currentState == UnitAI.UnitState.EnteringBuilding
                       || (unit.currentState == UnitAI.UnitState.MovingToSite && unit.HasPlank);

            case WorkRole.SawmillWorker:
                return (committedRoles.TryGetValue(unit, out WorkRole sawmillCommitted)
                        && sawmillCommitted == WorkRole.SawmillWorker)
                       || unit.IsHeadingToSawmill()
                       || unit.currentState == UnitAI.UnitState.EnteringBuilding
                       || unit.IsPerformingWork;

            case WorkRole.SawmillToStorage:
                return unit.IsWaitingForSawmillPlanks()
                       || (committedRoles.TryGetValue(unit, out WorkRole committed)
                           && committed == WorkRole.SawmillToStorage)
                       || unit.HasPlank
                       || unit.IsHeadingToSawmillPickup()
                       || unit.currentState == UnitAI.UnitState.MovingToStorage
                       || (unit.currentState == UnitAI.UnitState.MovingToSite && unit.IsHeadingToSawmillPickup());

            default:
                return false;
        }
    }

    List<WorkRole> ComputeNeededRoles(int unitCount)
    {
        List<WorkRole> roles = new List<WorkRole>();

        Storage storage = FindObjectOfType<Storage>();
        Sawmill sawmill = FindObjectOfType<Sawmill>();

        bool constructionNeeds = HasConstructionDemand();
        bool storageNotFull = storage != null && !storage.IsFull();
        bool storageHasPlanks = storage != null && storage.planks > 0;
        int pickupWaiting = sawmill != null ? sawmill.PlanksWaitingPickup : 0;

        // Сначала забираем готовые доски с выхода лесопилки.
        if (pickupWaiting > 0 && storageNotFull)
            roles.Add(WorkRole.SawmillToStorage);

        // Курьер на стройку — только если на складе уже есть доски.
        if (constructionNeeds && storageHasPlanks)
            roles.Add(WorkRole.DeliverToConstruction);

        // Пильщик пополняет склад, пока стройка активна и склад не полон (даже если там уже есть доски).
        if (sawmill != null && storageNotFull && constructionNeeds && unitCount >= 2)
        {
            if (unitCount >= 3 || !storageHasPlanks)
                roles.Add(WorkRole.SawmillWorker);
        }

        // При 3 юнитах — отдельный курьер «лесопилка → склад» (идёт только когда появятся доски).
        if (unitCount >= 3 && constructionNeeds && sawmill != null && storageNotFull
            && !roles.Contains(WorkRole.SawmillToStorage))
        {
            roles.Add(WorkRole.SawmillToStorage);
        }
        else if (sawmill != null && storageNotFull && !constructionNeeds)
        {
            roles.Add(WorkRole.SawmillWorker);
        }

        return roles;
    }

    public static void NotifyJobsChanged()
    {
        if (!ShouldCoordinate())
            return;

        Instance.RefreshRoleAssignments();
        Instance.NotifySawmillPickupUnits();

        foreach (UnitAI unit in Instance.GetAvailableUnits())
        {
            if (unit == null)
                continue;

            WorkRole committed = Instance.GetCommittedRole(unit);
            if (committed == WorkRole.SawmillToStorage || committed == WorkRole.SawmillWorker)
            {
                unit.ContinueAssignedWork();
                continue;
            }

            if (unit.AssignedWorkRole == WorkRole.SawmillToStorage
                || unit.AssignedWorkRole == WorkRole.SawmillWorker)
            {
                unit.ContinueAssignedWork();
                continue;
            }

            if (!unit.IsFreeForJobReassignment())
                continue;

            unit.FindJob();
        }
    }

    void NotifySawmillPickupUnits()
    {
        Sawmill sawmill = FindObjectOfType<Sawmill>();
        if (sawmill == null || sawmill.PlanksWaitingPickup <= 0)
            return;

        foreach (UnitAI unit in GetAvailableUnits())
        {
            if (unit == null)
                continue;

            if (GetCommittedRole(unit) == WorkRole.SawmillToStorage
                || unit.AssignedWorkRole == WorkRole.SawmillToStorage)
            {
                unit.WakeForSawmillPickup(sawmill);
            }
        }
    }

    bool IsRoleTaken(WorkRole role)
    {
        return rolesByUnit.ContainsValue(role);
    }

    public int CountAvailableUnits()
    {
        return GetAvailableUnits().Count;
    }

    public void EnsureDeliveryCourier(UnitAI unit)
    {
        if (unit == null)
            return;

        lock (deliveryLock)
            activeDeliveryCourier = unit;

        CommitRole(unit, WorkRole.DeliverToConstruction);
    }

    List<UnitAI> GetAvailableUnits()
    {
        return FindObjectsOfType<UnitAI>()
            .Where(u => u != null && u.isActiveAndEnabled && u.CanWork && !u.IsRestingForWork())
            .ToList();
    }

    static bool HasConstructionDemand()
    {
        ConstructionSite[] sites = FindObjectsOfType<ConstructionSite>();
        foreach (ConstructionSite site in sites)
        {
            if (site != null && site.NeedsPlanks())
                return true;
        }

        return false;
    }
}
