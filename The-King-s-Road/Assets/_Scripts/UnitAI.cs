using UnityEngine;
using System.Collections;

public class UnitAI : MonoBehaviour
{
    private Animator animator;
    private Vector3 targetPosition;
    public float movementSpeed = 2f;

    // Для ограничения движения
    private Camera mainCamera;
    private float objectWidth;
    private float objectHeight;

    // Для предотвращения дрожания
    private float directionThreshold = 0.5f;
    private bool isWaiting = false;

    // Система состояний
    public enum UnitState { Idle, MovingToStorage, MovingToSite, Working, EnteringBuilding, Resting }
    public UnitState currentState = UnitState.Idle;

    private Storage storage;
    private ConstructionSite currentSite;
    private bool hasPlank = false;
    private EntryPoint preferredSiteEntryPoint;

    // Переменная для точки входа
    private EntryPoint targetEntryPoint;

    // Для управления анимациями
    private float idleTimer = 0f;
    public float idleSwitchTime = 3f;

    [Header("Collision Avoidance")]
    public LayerMask obstacleLayer;
    public float raycastDistance = 1.5f;
    public float stopDistance = 0.3f;
    [Tooltip("Игнорировать коллайдер лесопилки только у двери/выхода на этом расстоянии.")]
    public float sawmillDoorPassDistance = 0.75f;

    [Header("Footstep Audio")]
    public float footstepInterval = 0.5f;
    private float footstepTimer = 0f;
    public float footstepVolume = 0.3f;

    [Header("Plank Visual")]
    public GameObject plankVisual;

    [Header("Stamina System")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;
    public float staminaDrainRate = 15f;      // Расход в секунду при работе
    public float staminaRestoreRate = 25f;     // Восстановление в секунду
    public float staminaThreshold = 30f;       // Ниже этого значения юнит устаёт

    [Header("Status Icons")]
    public GameObject tiredIcon;
    public Sprite tiredIconSprite;
    public Vector2 tiredIconOffset = new Vector2(0.55f, 0.55f);
    public float tiredIconScale = 0.65f;

    public GameObject workIcon;
    public Sprite workIconSprite;
    public Vector2 workIconOffset = new Vector2(0.55f, 0.55f);
    public float workIconScale = 0.65f;

    private bool isTired = false;
    private bool isShowingWorkIcon = false;
    private bool isResting = false;
    private bool isSeekingRest = false;
    private bool pendingRestAfterWork = false;
    private int activeWorkCount = 0;
    private Tavern targetTavern;

    private WorkCoordinator.WorkRole assignedWorkRole = WorkCoordinator.WorkRole.Unassigned;
    private Sawmill pickupSawmillTarget;
    private bool headingToSawmillPickup;
    // true = доска с лесопилки нужна сразу на стройку, а не на склад
    private bool directSawmillPickupToConstruction;
    private bool isFreeRoaming;
    private Coroutine waitForSawmillPlanksRoutine;
    private bool waitingForSawmillPlanks;
    private const float sawmillExitReachDistance = 0.65f;
    private float sawmillCooldownUntil;
    private Coroutine wanderRoutine;

    private bool hasWood = false;
    private int woodAmount = 0;

    // Rigidbody2D компонент
    private Rigidbody2D rb;



    // Public property для доступа к hasPlank
    public bool HasPlank => hasPlank;
    public bool IsExhausted => currentStamina <= 0f;
    public bool IsTiredNow => currentStamina <= staminaThreshold && !isResting;
    public bool CanWork => !isSeekingRest && !isResting && currentStamina > 0f;
    public bool IsPerformingWork => activeWorkCount > 0;
    public bool HasActiveWork =>
        !isResting && !isSeekingRest && !isFreeRoaming && (
            IsPerformingWork ||
            currentState == UnitState.EnteringBuilding ||
            currentState == UnitState.Working ||
            currentState == UnitState.MovingToStorage ||
            assignedWorkRole != WorkCoordinator.WorkRole.Unassigned ||
            (currentState == UnitState.MovingToSite && (hasPlank || currentSite != null || IsHeadingToSawmill() || IsHeadingToSawmillPickup())));

    public bool IsHeadingToSawmill()
    {
        return targetEntryPoint != null && targetEntryPoint.parentBuilding is Sawmill;
    }

    public bool IsHeadingToSawmillPickup()
    {
        return headingToSawmillPickup && pickupSawmillTarget != null;
    }

    public bool IsRestingForWork()
    {
        return isResting || isSeekingRest;
    }

    public void BeginSawmillCooldown(float seconds = 5f)
    {
        sawmillCooldownUntil = Time.time + seconds;
    }

    public bool IsOnSawmillCooldown()
    {
        return Time.time < sawmillCooldownUntil;
    }

    public WorkCoordinator.WorkRole AssignedWorkRole => assignedWorkRole;

    public bool IsWaitingForSawmillPlanks() => waitingForSawmillPlanks;

    public bool IsFreeForJobReassignment()
    {
        if (isFreeRoaming)
            return true;

        if (waitingForSawmillPlanks)
            return false;

        if (WorkCoordinator.ShouldCoordinate() && WorkCoordinator.Instance != null)
        {
            if (WorkCoordinator.Instance.IsActiveDeliveryCourier(this))
                return false;

            WorkCoordinator.WorkRole committed = WorkCoordinator.Instance.GetCommittedRole(this);
            if (committed == WorkCoordinator.WorkRole.DeliverToConstruction)
                return false;

            if (committed == WorkCoordinator.WorkRole.SawmillWorker
                || committed == WorkCoordinator.WorkRole.SawmillToStorage)
            {
                return false;
            }

            if (committed != WorkCoordinator.WorkRole.Unassigned
                && assignedWorkRole != WorkCoordinator.WorkRole.SawmillToStorage)
            {
                return false;
            }

            if (assignedWorkRole == WorkCoordinator.WorkRole.DeliverToConstruction)
                return false;
        }

        return currentState == UnitState.Idle;
    }

    bool ShouldPreserveCurrentTask()
    {
        if (currentState == UnitState.EnteringBuilding || IsPerformingWork)
            return true;

        if (isFreeRoaming)
            return false;

        if (waitingForSawmillPlanks)
            return true;

        if (currentState == UnitState.MovingToStorage)
            return true;

        if (currentState == UnitState.MovingToSite
            && (HasPlank || headingToSawmillPickup || targetEntryPoint != null))
            return true;

        if (currentState == UnitState.Resting)
            return true;

        return false;
    }



    void Start()
    {
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        // Получаем Rigidbody2D
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D не найден на юните! Добавьте компонент Rigidbody2D.");
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            objectWidth = sr.bounds.extents.x;
            objectHeight = sr.bounds.extents.y;
        }

        storage = FindObjectOfType<Storage>();
        WorkCoordinator.EnsureExists();
        currentStamina = maxStamina;

        SetIdleAnimation();

        // Скрываем доску в начале
        if (plankVisual != null)
            plankVisual.SetActive(false);

        EnsureTiredIcon();
        EnsureWorkIcon();
        if (tiredIcon != null)
            tiredIcon.SetActive(false);
        if (workIcon != null)
            workIcon.SetActive(false);

        WorkCoordinator.ScheduleSpawnJobAssignment();
    }

    public void BeginWork()
    {
        activeWorkCount++;
        currentState = UnitState.Working;
    }

    public void EndWork()
    {
        activeWorkCount = Mathf.Max(0, activeWorkCount - 1);
        if (activeWorkCount == 0 && !isResting && !isSeekingRest && currentState == UnitState.Working)
            currentState = UnitState.Idle;
    }

    public void RestoreStamina(float amount)
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
        UpdateStatusIcons();
    }

    void Update()
    {
        UpdateStamina();
        UpdateStatusIcons();

        if (currentState == UnitState.Idle && !isWaiting)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleSwitchTime)
            {
                idleTimer = 0f;
                SwitchIdleVariant();
            }
        }

        HandleFootstepAudio();

        if (currentState == UnitState.MovingToStorage || currentState == UnitState.MovingToSite || currentState == UnitState.Resting)
        {
            MoveToTarget();
        }
    }

    void UpdateStamina()
    {
        if (isResting)
        {
            currentStamina += staminaRestoreRate * Time.deltaTime;
            if (currentStamina >= maxStamina)
                CompleteRest();
            return;
        }

        bool isBusy = IsPerformingWork || currentState == UnitState.EnteringBuilding;
        if (isBusy)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                TriggerRest();
            }
        }
    }

    void UpdateStatusIcons()
    {
        SpriteRenderer unitRenderer = GetComponent<SpriteRenderer>();
        bool visible = unitRenderer == null || unitRenderer.enabled;

        bool shouldBeTired = IsTiredNow && visible;
        if (shouldBeTired != isTired)
        {
            isTired = shouldBeTired;
            if (tiredIcon != null)
                tiredIcon.SetActive(isTired);
        }

        if (isTired && tiredIcon != null)
            ApplyIconTransform(tiredIcon, tiredIconOffset, tiredIconScale);

        bool shouldShowWork = HasActiveWork && visible && !shouldBeTired;
        if (shouldShowWork != isShowingWorkIcon)
        {
            isShowingWorkIcon = shouldShowWork;
            if (workIcon != null)
                workIcon.SetActive(isShowingWorkIcon);
        }

        if (isShowingWorkIcon && workIcon != null)
            ApplyIconTransform(workIcon, workIconOffset, workIconScale);
    }

    static void ApplyIconTransform(GameObject icon, Vector2 offset, float scale)
    {
        Transform iconTransform = icon.transform;
        Transform parent = iconTransform.parent;
        float facing = parent != null ? parent.localScale.x : 1f;
        float side = facing >= 0f ? offset.x : -offset.x;
        iconTransform.localPosition = new Vector3(side, offset.y, 0f);
        iconTransform.localScale = Vector3.one * scale;
    }

    void TriggerRest()
    {
        if (isSeekingRest || isResting)
            return;

        SpriteRenderer unitRenderer = GetComponent<SpriteRenderer>();
        if (unitRenderer != null && !unitRenderer.enabled)
        {
            pendingRestAfterWork = true;
            return;
        }

        isSeekingRest = true;
        pendingRestAfterWork = false;
        AbortCurrentTask();

        Debug.Log($"{gameObject.name} выдохся и прекращает работу.");

        Tavern tavern = FindAvailableTavern();
        if (tavern != null)
        {
            targetTavern = tavern;
            currentState = UnitState.Resting;

            EntryPoint nearestPoint = tavern.GetNearestFreeEntryPoint(transform.position);
            if (nearestPoint != null && nearestPoint.TryOccupy())
            {
                targetEntryPoint = nearestPoint;
                targetPosition = nearestPoint.transform.position;
            }
            else
            {
                targetEntryPoint = null;
                targetPosition = tavern.GetDoorPosition();
            }

            SetWalkAnimation();
            return;
        }

        StartRestingAtPlace();
    }

    void AbortCurrentTask()
    {
        StopAllCoroutines();
        ReleaseReservedEntryPoint();
        StopMoving();
        isWaiting = false;
        activeWorkCount = 0;
        headingToSawmillPickup = false;
        directSawmillPickupToConstruction = false;
        pickupSawmillTarget = null;
    }

    Tavern FindAvailableTavern()
    {
        Tavern[] taverns = FindObjectsOfType<Tavern>();
        Tavern nearest = null;
        float minDistance = float.MaxValue;

        foreach (Tavern tavern in taverns)
        {
            if (tavern == null || !tavern.isActiveAndEnabled)
                continue;

            float distance = Vector3.Distance(transform.position, tavern.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = tavern;
            }
        }

        return nearest;
    }

    public void RequestRestAtPlace()
    {
        if (isResting)
            return;

        isSeekingRest = false;
        AbortCurrentTask();
        StartRestingAtPlace();
    }

    void StartRestingAtPlace()
    {
        Debug.Log($"{gameObject.name} отдыхает на месте (таверны нет)");
        isSeekingRest = false;
        isResting = true;
        currentState = UnitState.Idle;
        StopMoving();
        SetIdleAnimation();
    }

    public void BeginTavernRest(Tavern tavern)
    {
        isSeekingRest = false;
        isResting = true;
        targetTavern = tavern;
        currentState = UnitState.Idle;
        StopMoving();

        if (tiredIcon != null)
            tiredIcon.SetActive(false);
        if (workIcon != null)
            workIcon.SetActive(false);
        isShowingWorkIcon = false;
    }

    public void CompleteRest()
    {
        isResting = false;
        isSeekingRest = false;
        currentStamina = maxStamina;
        currentState = UnitState.Idle;
        targetTavern = null;
        activeWorkCount = 0;

        if (targetEntryPoint != null)
        {
            targetEntryPoint.Vacate();
            targetEntryPoint = null;
        }

        UpdateStatusIcons();
        FindJob();
    }

    void EnsureTiredIcon()
    {
        if (tiredIcon != null)
            return;

        tiredIcon = new GameObject("TiredIcon");
        tiredIcon.transform.SetParent(transform, false);
        tiredIcon.transform.localPosition = new Vector3(tiredIconOffset.x, tiredIconOffset.y, 0f);

        SpriteRenderer iconRenderer = tiredIcon.AddComponent<SpriteRenderer>();
        SpriteRenderer unitRenderer = GetComponent<SpriteRenderer>();
        iconRenderer.sprite = tiredIconSprite != null ? tiredIconSprite : CreateDefaultTiredSprite();
        iconRenderer.sortingLayerID = unitRenderer != null ? unitRenderer.sortingLayerID : 0;
        iconRenderer.sortingOrder = unitRenderer != null ? unitRenderer.sortingOrder + 2 : 2;
        iconRenderer.color = new Color(1f, 0.85f, 0.2f, 1f);
        tiredIcon.transform.localScale = Vector3.one * tiredIconScale;
        tiredIcon.SetActive(false);
    }

    static Sprite defaultTiredSprite;

    static Sprite CreateDefaultTiredSprite()
    {
        if (defaultTiredSprite != null)
            return defaultTiredSprite;

        const int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color fill = new Color(1f, 0.75f, 0.1f, 1f);
        Color empty = new Color(0f, 0f, 0f, 0f);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.42f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                texture.SetPixel(x, y, distance <= radius ? fill : empty);
            }
        }

        texture.Apply();
        defaultTiredSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return defaultTiredSprite;
    }

    void EnsureWorkIcon()
    {
        if (workIcon != null)
            return;

        workIcon = new GameObject("WorkIcon");
        workIcon.transform.SetParent(transform, false);
        workIcon.transform.localPosition = new Vector3(workIconOffset.x, workIconOffset.y, 0f);

        SpriteRenderer iconRenderer = workIcon.AddComponent<SpriteRenderer>();
        SpriteRenderer unitRenderer = GetComponent<SpriteRenderer>();
        iconRenderer.sprite = workIconSprite != null ? workIconSprite : CreateDefaultWorkSprite();
        iconRenderer.sortingLayerID = unitRenderer != null ? unitRenderer.sortingLayerID : 0;
        iconRenderer.sortingOrder = unitRenderer != null ? unitRenderer.sortingOrder + 2 : 2;
        iconRenderer.color = new Color(0.35f, 0.75f, 1f, 1f);
        workIcon.transform.localScale = Vector3.one * workIconScale;
        workIcon.SetActive(false);
    }

    static Sprite defaultWorkSprite;

    static Sprite CreateDefaultWorkSprite()
    {
        if (defaultWorkSprite != null)
            return defaultWorkSprite;

        const int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color fill = new Color(0.35f, 0.75f, 1f, 1f);
        Color empty = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = x >= 3 && x <= 12 && y >= 3 && y <= 12;
                texture.SetPixel(x, y, inside ? fill : empty);
            }
        }

        texture.Apply();
        defaultWorkSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return defaultWorkSprite;
    }

    void HandleFootstepAudio()
    {
        if (currentState == UnitState.MovingToStorage || currentState == UnitState.MovingToSite || currentState == UnitState.Resting)
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= footstepInterval)
            {
                PlayFootstepSound();
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    void PlayFootstepSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFXAtPosition("footstep", transform.position, footstepVolume);
        }
    }

    void SetIdleAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
        }
    }

    void SetWalkAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
        }
    }

    void SwitchIdleVariant()
    {
        if (animator != null)
        {
            int randomIdle = Random.Range(0, 2);
            animator.SetInteger("IdleVariant", randomIdle);
        }
    }

    // Метод для остановки движения
    void StopMoving()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;
        SetIdleAnimation();
    }

    static bool IsColliderPartOfBuilding(Collider2D collider, Building building)
    {
        if (collider == null || building == null)
            return false;

        Transform colliderTransform = collider.transform;
        Transform buildingTransform = building.transform;
        return colliderTransform == buildingTransform || colliderTransform.IsChildOf(buildingTransform);
    }

    bool ShouldIgnoreSawmillCollider(Collider2D collider, float distanceToTarget, Sawmill knownSawmill)
    {
        if (collider == null)
            return false;

        Sawmill sawmill = knownSawmill ?? collider.GetComponentInParent<Sawmill>();
        if (sawmill == null || !IsColliderPartOfBuilding(collider, sawmill))
            return false;

        // Забор досок у выхода: идём к exitPoint, коллайдер здания не должен блокировать путь.
        if (headingToSawmillPickup && pickupSawmillTarget != null
            && (pickupSawmillTarget == sawmill || pickupSawmillTarget == knownSawmill))
        {
            return true;
        }

        if (distanceToTarget > sawmillDoorPassDistance)
            return false;

        if (IsHeadingToSawmill() && !HasPlank)
            return true;

        return false;
    }

    bool IsRaycastBlocked(RaycastHit2D hit, float distanceToTarget, Sawmill knownSawmill)
    {
        if (hit.collider == null)
            return false;

        if (ShouldIgnoreSawmillCollider(hit.collider, distanceToTarget, knownSawmill))
            return false;

        return true;
    }

    void MoveToTarget()
    {
        if (currentState == UnitState.MovingToSite && targetEntryPoint != null)
        {
            Debug.Log($"MovingToTarget: targetEntryPoint = {targetEntryPoint.name}, parentBuilding = {targetEntryPoint.parentBuilding?.name}");
        }

        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        // Игнорируем коллайдер склада
        bool ignoreStorageCollider = false;
        if (currentState == UnitState.MovingToStorage && storage != null)
        {
            if (Vector3.Distance(targetPosition, storage.GetDoorPosition()) < 0.1f)
            {
                // Игнорируем коллайдер только вблизи двери, а не по всему маршруту.
                ignoreStorageCollider = distanceToTarget <= 0.6f;
            }
        }

        // Игнорируем коллайдер стройки, если идем к точке входа
        bool ignoreSiteCollider = false;
        if (currentState == UnitState.MovingToSite && currentSite != null && targetEntryPoint != null)
        {
            if (Vector3.Distance(targetPosition, targetEntryPoint.transform.position) < 0.1f)
            {
                // Пропускаем коллайдер стройки только на последнем отрезке к точке входа.
                ignoreSiteCollider = distanceToTarget <= 0.6f;
            }
        }

        Sawmill targetSawmill = null;
        if (currentState == UnitState.MovingToSite && targetEntryPoint != null)
            targetSawmill = targetEntryPoint.parentBuilding as Sawmill;
        if (targetSawmill == null && headingToSawmillPickup && pickupSawmillTarget != null)
            targetSawmill = pickupSawmillTarget;

        // Игнорируем коллайдер таверны
        bool ignoreTavernCollider = false;
        Tavern targetTavernBuilding = null;
        if (currentState == UnitState.Resting && targetEntryPoint != null)
        {
            targetTavernBuilding = targetEntryPoint.parentBuilding as Tavern;
            if (targetTavernBuilding != null)
            {
                if (Vector3.Distance(targetPosition, targetEntryPoint.transform.position) < 0.1f)
                {
                    ignoreTavernCollider = distanceToTarget <= 0.6f;
                }
            }
        }

        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToTarget, raycastDistance, obstacleLayer);

        // Игнорируем коллайдер склада
        if (ignoreStorageCollider && hit.collider != null && storage != null
            && IsColliderPartOfBuilding(hit.collider, storage))
        {
            hit = new RaycastHit2D();
        }

        // Игнорируем коллайдер стройки
        if (ignoreSiteCollider && hit.collider != null && currentSite != null
            && IsColliderPartOfBuilding(hit.collider, currentSite))
        {
            hit = new RaycastHit2D();
        }

        if (hit.collider != null && ShouldIgnoreSawmillCollider(hit.collider, distanceToTarget, targetSawmill))
            hit = new RaycastHit2D();

        // Игнорируем коллайдер таверны
        if (ignoreTavernCollider && hit.collider != null && targetTavernBuilding != null && hit.collider.gameObject == targetTavernBuilding.gameObject)
        {
            hit = new RaycastHit2D();
        }

        Debug.DrawRay(transform.position, directionToTarget * raycastDistance, Color.blue);

        Vector3 finalDirection = directionToTarget;

        if (hit.collider != null)
        {
            finalDirection = GetAvoidanceDirection(directionToTarget, hit, distanceToTarget, targetSawmill);

            if (finalDirection == Vector3.zero)
            {
                if (headingToSawmillPickup && pickupSawmillTarget != null)
                    finalDirection = directionToTarget;
                else
                {
                    StopMoving();
                    StartCoroutine(WaitAndRetry());
                    return;
                }
            }
        }

        // Движение с использованием Rigidbody2D
        if (distanceToTarget > stopDistance)
        {
            if (rb != null)
            {
                rb.linearVelocity = finalDirection * movementSpeed;
            }
            else
            {
                transform.position += finalDirection * movementSpeed * Time.deltaTime;
            }
            SetWalkAnimation();
        }
        else
        {
            StopMoving();
            OnReachedDestination();
        }

        // Поворот спрайта
        float xDifference = targetPosition.x - transform.position.x;
        if (Mathf.Abs(xDifference) > directionThreshold)
        {
            transform.localScale = new Vector3(xDifference > 0 ? 1 : -1, 1, 1);
        }
    }

    Vector3 GetAvoidanceDirection(Vector3 originalDirection, RaycastHit2D obstacleHit, float distanceToTarget, Sawmill knownSawmill)
    {
        Vector3 bestDirection = Vector3.zero;
        float bestScore = -Mathf.Infinity;

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 testDirection = Quaternion.Euler(0, 0, angle) * Vector3.right;

            RaycastHit2D testHit = Physics2D.Raycast(transform.position, testDirection, raycastDistance, obstacleLayer);
            if (!IsRaycastBlocked(testHit, distanceToTarget, knownSawmill))
            {
                float dotProduct = Vector3.Dot(testDirection, originalDirection);
                float pathDistanceToTarget = Vector3.Distance(transform.position + testDirection * raycastDistance, targetPosition);
                float score = dotProduct * 2f - pathDistanceToTarget;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = testDirection;
                }

                Debug.DrawRay(transform.position, testDirection * raycastDistance, Color.green);
            }
            else
            {
                Debug.DrawRay(transform.position, testDirection * raycastDistance, Color.red);
            }
        }

        if (bestDirection != Vector3.zero)
        {
            return bestDirection;
        }

        Vector3 retreatDirection = -originalDirection;
        RaycastHit2D retreatHit = Physics2D.Raycast(transform.position, retreatDirection, raycastDistance, obstacleLayer);
        if (!IsRaycastBlocked(retreatHit, distanceToTarget, knownSawmill))
        {
            Debug.DrawRay(transform.position, retreatDirection * raycastDistance, Color.yellow);
            return retreatDirection;
        }

        return Vector3.zero;
    }

    IEnumerator WaitAndRetry()
    {
        Sawmill sawmillForPickup = headingToSawmillPickup ? pickupSawmillTarget : null;
        bool deliverPickupToConstruction = directSawmillPickupToConstruction;
        StopMoving();

        isWaiting = true;
        yield return new WaitForSeconds(2f);
        isWaiting = false;

        if (sawmillForPickup != null
            && (assignedWorkRole == WorkCoordinator.WorkRole.SawmillToStorage || deliverPickupToConstruction))
        {
            ResumeMovingToSawmillExit(sawmillForPickup, deliverPickupToConstruction);
            yield break;
        }

        if (currentState == UnitState.MovingToStorage || currentState == UnitState.MovingToSite || currentState == UnitState.Resting)
        {
            SetWalkAnimation();
        }
    }

    void OnReachedDestination()
    {
        switch (currentState)
        {
            case UnitState.MovingToStorage:
                OnReachedStorage();
                break;

            case UnitState.MovingToSite:
                OnReachedSite();
                break;

            case UnitState.Resting:
                OnReachedTavern();
                break;
        }
    }

    void OnReachedTavern()
    {
        StopMoving();

        if (targetEntryPoint != null && targetEntryPoint.parentBuilding is Tavern)
        {
            Tavern tavern = targetEntryPoint.parentBuilding as Tavern;
            Debug.Log($"ДОШЕЛ ДО ТАВЕРНЫ! Отдыхаю в {tavern.name}");
            tavern.Interact(this, targetEntryPoint);
            targetEntryPoint = null;
        }
        else
        {
            // Если не дошли до таверны, отдыхаем на месте
            RequestRestAtPlace();
        }
    }

    void OnReachedStorage()
    {
        StartCoroutine(EnterStorage());
    }

    IEnumerator EnterStorage()
    {
        StopMoving();
        currentState = UnitState.EnteringBuilding;
        SetIdleAnimation();

        Debug.Log("Подошел к двери, захожу внутрь...");

        var currentStorage = storage;
        var currentSiteRef = currentSite;
        var currentEntryPoint = targetEntryPoint;

        Debug.Log($"Запускаю корутину склада. Склад: {currentStorage}, Стройка: {currentSiteRef}, Точка входа: {currentEntryPoint?.name}");

        // Если юнит уже несет доску, сначала разгружаем ее на склад.
        if (hasPlank)
        {
            yield return StartCoroutine(currentStorage.EnterAndStorePlank(this, currentEntryPoint));
            currentState = UnitState.Idle;
            SetIdleAnimation();
            FindJob();
            yield break;
        }

        yield return StartCoroutine(currentStorage.EnterAndTakePlank(this, currentEntryPoint));

        Debug.Log($"Корутина склада завершена. hasPlank = {hasPlank}");

        if (hasPlank)
        {
            StopWandering();
            isFreeRoaming = false;

            if (!ResumeDeliveryToConstruction(currentSiteRef))
                FindJob();
        }
        else
        {
            Debug.Log("На складе нет досок");

            Sawmill pickupSawmill = FindObjectOfType<Sawmill>();
            if (currentSiteRef != null && currentSiteRef.NeedsPlanks()
                && pickupSawmill != null && pickupSawmill.PlanksWaitingPickup > 0)
            {
                currentSite = currentSiteRef;
                GoToSawmillPickup(pickupSawmill, true, currentSiteRef);
                Debug.Log("[Стройка] Склад пустой — беру готовую доску прямо с лесопилки");
                yield break;
            }

            if (WorkCoordinator.ShouldCoordinate())
                WorkCoordinator.Instance.ReleaseDelivery(this);

            currentState = UnitState.Idle;
            SetIdleAnimation();
            FindJob();
        }
    }

    void OnReachedSite()
    {
        if (headingToSawmillPickup && pickupSawmillTarget != null)
        {
            HandleSawmillPickupArrival();
            return;
        }

        StopMoving();
        Debug.Log($"OnReachedSite: hasPlank={hasPlank}, currentSite={currentSite?.name}, targetEntryPoint={targetEntryPoint?.name}");

        // Проверяем, не идём ли мы на лесопилку
        if (targetEntryPoint != null && targetEntryPoint.parentBuilding is Sawmill)
        {
            Sawmill sawmill = targetEntryPoint.parentBuilding as Sawmill;
            EntryPoint entry = targetEntryPoint;
            targetEntryPoint = null;

            if (sawmill.IsWorking)
            {
                entry.Vacate();
                EnterIdleWaitingForCoordinator();
                return;
            }

            if (IsOnSawmillCooldown())
            {
                entry.Vacate();
                if (sawmill.PlanksWaitingPickup > 0 && sawmill.TryTakePickupPlank(this))
                {
                    GoToStorage(storage);
                    return;
                }

                FindJob();
                return;
            }

            Debug.Log($"ДОШЕЛ ДО ЛЕСОПИЛКИ! Иду на лесопилку {sawmill.name}");
            sawmill.Interact(this, entry);
            return;
        }

        // Если targetEntryPoint не null, но это не лесопилка
        if (targetEntryPoint != null)
        {
            Debug.Log($"targetEntryPoint.parentBuilding = {targetEntryPoint.parentBuilding?.GetType().Name}");
        }

        // Обычная стройка
        if (hasPlank && currentSite != null)
        {
            var site = currentSite;
            var entryPoint = targetEntryPoint;
            targetEntryPoint = null;

            if (entryPoint != null)
            {
                Debug.Log($"Иду к точке {entryPoint.name} для сдачи доски");
                site.Interact(this, entryPoint);
            }
            else
            {
                if (TryAssignSiteEntryPoint(site))
                {
                    site.Interact(this, targetEntryPoint);
                }
                else
                {
                    Debug.Log("Все точки входа заняты, жду...");
                    StartCoroutine(WaitAndRetrySite());
                }
            }
        }
        else
        {
            Debug.LogWarning($"Юнит {gameObject.name} пришел на стройку без доски или стройка не найдена");
            FindJob();
        }
    }

    IEnumerator WaitAndRetrySite()
    {
        currentState = UnitState.Idle;
        SetIdleAnimation();

        yield return new WaitForSeconds(2f);

        // Если за время ожидания юнит получил новую задачу, не форсим повторный вход.
        if (currentState != UnitState.Idle || !hasPlank || currentSite == null)
        {
            yield break;
        }

        OnReachedSite();
    }

    IEnumerator WaitAndRetryStorage()
    {
        StopMoving();
        currentState = UnitState.Idle;
        SetIdleAnimation();
        yield return new WaitForSeconds(2f);
        FindJob();
    }

    void HandleSawmillPickupArrival()
    {
        Sawmill sawmill = pickupSawmillTarget;
        bool deliverDirectlyToConstruction = directSawmillPickupToConstruction;
        if (sawmill == null)
        {
            headingToSawmillPickup = false;
            directSawmillPickupToConstruction = false;
            pickupSawmillTarget = null;
            FindJob();
            return;
        }

        if (TryTakeSawmillPickupPlank(sawmill))
            return;

        if (sawmill.PlanksWaitingPickup <= 0)
        {
            if (deliverDirectlyToConstruction)
            {
                headingToSawmillPickup = false;
                directSawmillPickupToConstruction = false;
                pickupSawmillTarget = null;
                StartCoroutine(WaitAndRetryRole(1f));
                return;
            }

            EnterWaitingForSawmillPlanks(sawmill);
            return;
        }

        if (!IsNearSawmillExit(sawmill, sawmillExitReachDistance))
        {
            ResumeMovingToSawmillExit(sawmill, deliverDirectlyToConstruction);
            return;
        }

        if (deliverDirectlyToConstruction)
        {
            StartCoroutine(WaitAndRetryRole(1f));
            return;
        }

        EnterWaitingForSawmillPlanks(sawmill);
    }

    bool IsWithinSawmillPickupRange(Sawmill sawmill)
    {
        if (sawmill == null)
            return false;

        float exitDistance = Vector3.Distance(transform.position, sawmill.GetExitPosition());
        float doorDistance = Vector3.Distance(transform.position, sawmill.GetDoorPosition());
        return exitDistance <= 1.4f || doorDistance <= 1.2f;
    }

    bool TryTakeSawmillPickupPlank(Sawmill sawmill)
    {
        if (sawmill == null || sawmill.PlanksWaitingPickup <= 0)
            return false;

        if (!IsWithinSawmillPickupRange(sawmill) && !IsNearSawmillExit(sawmill, sawmillExitReachDistance))
            return false;

        if (!sawmill.TryTakePickupPlank(this))
            return false;

        bool deliverDirectlyToConstruction = directSawmillPickupToConstruction;

        headingToSawmillPickup = false;
        directSawmillPickupToConstruction = false;
        pickupSawmillTarget = null;
        StopMoving();

        if (deliverDirectlyToConstruction)
        {
            Debug.Log("[Стройка] Забрал доску у лесопилки — несу сразу на стройку");
            if (!ResumeDeliveryToConstruction(currentSite))
                FindJob();
            return true;
        }

        Debug.Log("[Склад] Забрал доску у лесопилки");
        GoToStorage(storage);
        return true;
    }

    void ResumeMovingToSawmillExit(Sawmill sawmill, bool deliverDirectlyToConstruction = false)
    {
        pickupSawmillTarget = sawmill;
        headingToSawmillPickup = true;
        directSawmillPickupToConstruction = deliverDirectlyToConstruction;
        currentState = UnitState.MovingToSite;
        targetPosition = sawmill.GetExitPosition();
        SetWalkAnimation();
        Debug.Log(deliverDirectlyToConstruction
            ? "[Стройка] Иду к выходу лесопилки за доской для стройки"
            : "[Склад] Иду к выходу лесопилки за доской");
    }

    public void WakeForSawmillPickup(Sawmill sawmill)
    {
        if (assignedWorkRole != WorkCoordinator.WorkRole.SawmillToStorage || HasPlank || sawmill == null)
            return;

        StopWaitingForSawmillPlanks();
        StopWandering();
        isFreeRoaming = false;

        if (TryTakeSawmillPickupPlank(sawmill))
            return;

        if (sawmill.PlanksWaitingPickup > 0)
            GoToSawmillPickup(sawmill);
    }

    void StopWaitingForSawmillPlanks()
    {
        waitingForSawmillPlanks = false;
        if (waitForSawmillPlanksRoutine != null)
        {
            StopCoroutine(waitForSawmillPlanksRoutine);
            waitForSawmillPlanksRoutine = null;
        }
    }

    void EnterWaitingForSawmillPlanks(Sawmill sawmill)
    {
        if (sawmill == null)
            return;

        StopWandering();
        isFreeRoaming = false;
        headingToSawmillPickup = false;
        directSawmillPickupToConstruction = false;
        pickupSawmillTarget = sawmill;
        StopMoving();
        currentState = UnitState.Idle;
        SetIdleAnimation();

        if (storage != null && storage.IsFull())
        {
            Debug.Log("[Склад] Склад полон — жду места для досок с лесопилки");
        }
        else
        {
            Debug.Log("[Склад] Жду появления досок на лесопилке (не иду заранее)");
        }

        waitingForSawmillPlanks = true;
        if (waitForSawmillPlanksRoutine != null)
            StopCoroutine(waitForSawmillPlanksRoutine);

        waitForSawmillPlanksRoutine = StartCoroutine(WaitForSawmillPlanksCoroutine(sawmill));
    }

    IEnumerator WaitForSawmillPlanksCoroutine(Sawmill sawmill)
    {
        while (waitingForSawmillPlanks
               && CanWork
               && assignedWorkRole == WorkCoordinator.WorkRole.SawmillToStorage
               && sawmill != null
               && !HasPlank)
        {
            if (storage != null && storage.IsFull())
            {
                yield return new WaitForSeconds(1.5f);
                continue;
            }

            if (sawmill.PlanksWaitingPickup > 0)
            {
                StopWaitingForSawmillPlanks();
                GoToSawmillPickup(sawmill);
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }

        StopWaitingForSawmillPlanks();
    }

    IEnumerator WaitAndRetrySawmill()
    {
        currentState = UnitState.Idle;
        SetIdleAnimation();
        yield return new WaitForSeconds(2f);

        // Если за время ожидания задача изменилась, не перезаписываем её.
        if (currentState != UnitState.Idle || hasPlank || currentSite != null)
        {
            yield break;
        }

        FindJob();
    }

    public void BeginCoordinatedRole(WorkCoordinator.WorkRole role)
    {
        if (isResting || isSeekingRest || role == WorkCoordinator.WorkRole.Unassigned)
            return;

        StopWandering();
        isFreeRoaming = false;
        assignedWorkRole = role;

        if (WorkCoordinator.ShouldCoordinate())
            WorkCoordinator.Instance.CommitRole(this, role);

        switch (role)
        {
            case WorkCoordinator.WorkRole.DeliverToConstruction:
                if (WorkCoordinator.ShouldCoordinate())
                    WorkCoordinator.Instance.EnsureDeliveryCourier(this);
                if (FindJobDeliverToConstruction())
                    return;
                StartCoroutine(WaitAndRetryRole(1f));
                return;

            case WorkCoordinator.WorkRole.SawmillWorker:
                if (FindJobSawmillWorker())
                    return;
                StartCoroutine(WaitAndRetryRole(1.5f));
                return;

            case WorkCoordinator.WorkRole.SawmillToStorage:
                if (FindJobSawmillToStorage())
                    return;
                StartCoroutine(WaitAndRetryRole(1.5f));
                return;
        }
    }

    bool IsNearSawmillExit(Sawmill sawmill, float maxDistance)
    {
        if (sawmill == null)
            return false;

        return Vector3.Distance(transform.position, sawmill.GetExitPosition()) <= maxDistance;
    }

    public void ContinueDeliveryWork()
    {
        if (isResting || isSeekingRest)
            return;

        StopWandering();
        isFreeRoaming = false;
        assignedWorkRole = WorkCoordinator.WorkRole.DeliverToConstruction;

        if (WorkCoordinator.ShouldCoordinate())
        {
            WorkCoordinator.Instance.EnsureDeliveryCourier(this);
            WorkCoordinator.Instance.CommitRole(this, WorkCoordinator.WorkRole.DeliverToConstruction);
        }

        if (FindJobDeliverToConstruction())
            return;

        StartCoroutine(WaitAndRetryRole(1f));
    }

    public void ContinueAssignedWork()
    {
        if (isResting || isSeekingRest)
            return;

        StopWandering();
        isFreeRoaming = false;

        if (WorkCoordinator.ShouldCoordinate())
        {
            WorkCoordinator.WorkRole role = WorkCoordinator.Instance.GetCommittedRole(this);
            if (role == WorkCoordinator.WorkRole.Unassigned)
                role = assignedWorkRole;

            if (role == WorkCoordinator.WorkRole.DeliverToConstruction
                || WorkCoordinator.Instance.IsActiveDeliveryCourier(this))
            {
                ContinueDeliveryWork();
                return;
            }

            if (role == WorkCoordinator.WorkRole.Unassigned)
            {
                FindJob();
                return;
            }

            assignedWorkRole = role;
            WorkCoordinator.Instance.CommitRole(this, role);

            switch (role)
            {
                case WorkCoordinator.WorkRole.SawmillWorker:
                    if (FindJobSawmillWorker())
                        return;
                    break;
                case WorkCoordinator.WorkRole.SawmillToStorage:
                    if (FindJobSawmillToStorage())
                        return;
                    break;
            }
        }

        FindJob();
    }

    public void FindJob()
    {
        if (isResting || isSeekingRest)
            return;

        if (ShouldPreserveCurrentTask())
            return;

        StopWandering();
        isFreeRoaming = false;
        StopMoving();
        ReleaseReservedEntryPoint();
        headingToSawmillPickup = false;
        directSawmillPickupToConstruction = false;
        pickupSawmillTarget = null;

        if (pendingRestAfterWork || IsExhausted)
        {
            pendingRestAfterWork = false;
            TriggerRest();
            return;
        }

        if (WorkCoordinator.ShouldCoordinate())
        {
            WorkCoordinator.WorkRole committed = WorkCoordinator.Instance.GetCommittedRole(this);
            assignedWorkRole = committed != WorkCoordinator.WorkRole.Unassigned
                ? committed
                : WorkCoordinator.Instance.GetRole(this);

            if (assignedWorkRole == WorkCoordinator.WorkRole.Unassigned)
            {
                WorkCoordinator.Instance.ReleaseCommittedRole(this);
                EnterIdleNoAssignment();
                return;
            }

            WorkCoordinator.Instance.CommitRole(this, assignedWorkRole);

            switch (assignedWorkRole)
            {
                case WorkCoordinator.WorkRole.DeliverToConstruction:
                    if (FindJobDeliverToConstruction())
                        return;
                    EnterIdleNoAssignment();
                    return;

                case WorkCoordinator.WorkRole.SawmillWorker:
                    if (FindJobSawmillWorker())
                        return;
                    EnterIdleNoAssignment();
                    return;

                case WorkCoordinator.WorkRole.SawmillToStorage:
                    if (FindJobSawmillToStorage())
                        return;
                    EnterIdleNoAssignment();
                    return;
            }

            EnterIdleNoAssignment();
            return;
        }

        assignedWorkRole = WorkCoordinator.WorkRole.Unassigned;
        FindJobLegacy();
    }

    public void EnterIdleWaitingForCoordinator()
    {
        EnterIdleNoAssignment();
    }

    void EnterIdleNoAssignment()
    {
        StopWandering();

        if (HasPlank && TryResumePlankTask())
            return;

        if (WorkCoordinator.ShouldCoordinate())
        {
            WorkCoordinator.Instance.ReleaseDelivery(this);
            WorkCoordinator.Instance.ReleaseCommittedRole(this);
        }

        assignedWorkRole = WorkCoordinator.WorkRole.Unassigned;
        headingToSawmillPickup = false;
        directSawmillPickupToConstruction = false;
        pickupSawmillTarget = null;
        currentSite = null;
        isFreeRoaming = true;
        StopMoving();
        Debug.Log($"{gameObject.name}: нет работы — брожу и жду задачу.");
        StartWandering();
    }

    bool TryResumePlankTask()
    {
        isFreeRoaming = false;

        if (WorkCoordinator.ShouldCoordinate())
        {
            if (WorkCoordinator.Instance.IsActiveDeliveryCourier(this)
                || assignedWorkRole == WorkCoordinator.WorkRole.DeliverToConstruction
                || committedDeliveryRole())
            {
                if (FindJobDeliverToConstruction())
                    return true;
            }

            if (assignedWorkRole == WorkCoordinator.WorkRole.SawmillToStorage && storage != null)
            {
                GoToStorage(storage);
                return true;
            }
        }
        else if (FindJobDeliverToConstruction())
        {
            return true;
        }

        return false;
    }

    bool committedDeliveryRole()
    {
        return WorkCoordinator.Instance != null
               && WorkCoordinator.Instance.GetCommittedRole(this) == WorkCoordinator.WorkRole.DeliverToConstruction;
    }

    bool ResumeDeliveryToConstruction(ConstructionSite siteHint)
    {
        StopWandering();
        isFreeRoaming = false;

        ConstructionSite site = siteHint;
        if (site == null || !site.NeedsPlanks())
        {
            foreach (ConstructionSite candidate in FindObjectsOfType<ConstructionSite>())
            {
                if (candidate != null && candidate.NeedsPlanks())
                {
                    site = candidate;
                    break;
                }
            }
        }

        if (site == null || !site.NeedsPlanks())
        {
            Debug.LogWarning($"{gameObject.name}: несу доску, но стройка не принимает — ищу другую задачу.");
            return false;
        }

        currentSite = site;
        currentState = UnitState.MovingToSite;

        if (!TryAssignSiteEntryPoint(site))
        {
            currentState = UnitState.Idle;
            SetIdleAnimation();
            StartCoroutine(WaitAndRetrySite());
            return true;
        }

        SetWalkAnimation();
        Debug.Log("[Стройка] Вышел со склада — несу доску на стройплощадку");
        return true;
    }

    bool FindJobDeliverToConstruction()
    {
        if (WorkCoordinator.ShouldCoordinate()
            && assignedWorkRole != WorkCoordinator.WorkRole.DeliverToConstruction)
        {
            return false;
        }

        ConstructionSite[] sites = FindObjectsOfType<ConstructionSite>();
        bool constructionNeedsPlanks = false;
        foreach (ConstructionSite site in sites)
        {
            if (site.NeedsPlanks())
            {
                constructionNeedsPlanks = true;
                break;
            }
        }

        if (!constructionNeedsPlanks)
            return false;

        if (HasPlank)
        {
            return ResumeDeliveryToConstruction(currentSite);
        }

        if (storage != null && storage.planks > 0)
        {
            if (WorkCoordinator.ShouldCoordinate())
            {
                if (!WorkCoordinator.Instance.TryClaimDelivery(this))
                {
                    if (assignedWorkRole == WorkCoordinator.WorkRole.DeliverToConstruction)
                    {
                        StartCoroutine(WaitAndRetryRole(1f));
                        return true;
                    }

                    EnterIdleNoAssignment();
                    return true;
                }

                WorkCoordinator.Instance.CommitRole(this, WorkCoordinator.WorkRole.DeliverToConstruction);
            }

            currentSite = null;
            foreach (ConstructionSite site in sites)
            {
                if (site.NeedsPlanks())
                {
                    currentSite = site;
                    break;
                }
            }

            currentState = UnitState.MovingToStorage;
            targetPosition = storage.GetDoorPosition();
            targetEntryPoint = null;
            SetWalkAnimation();
            Debug.Log("[Стройка] Иду на склад за доской");
            return true;
        }

        // Если склад пустой, но у лесопилки уже лежат готовые доски,
        // строительный курьер забирает доску напрямую с лесопилки и несёт её на стройку.
        Sawmill pickupSawmill = FindObjectOfType<Sawmill>();
        if (pickupSawmill != null && pickupSawmill.PlanksWaitingPickup > 0)
        {
            if (WorkCoordinator.ShouldCoordinate())
            {
                if (!WorkCoordinator.Instance.TryClaimDelivery(this))
                {
                    if (assignedWorkRole == WorkCoordinator.WorkRole.DeliverToConstruction)
                    {
                        StartCoroutine(WaitAndRetryRole(1f));
                        return true;
                    }

                    EnterIdleNoAssignment();
                    return true;
                }

                WorkCoordinator.Instance.CommitRole(this, WorkCoordinator.WorkRole.DeliverToConstruction);
            }

            currentSite = null;
            foreach (ConstructionSite site in sites)
            {
                if (site.NeedsPlanks())
                {
                    currentSite = site;
                    break;
                }
            }

            GoToSawmillPickup(pickupSawmill, true, currentSite);
            Debug.Log("[Стройка] На складе досок нет — забираю готовую доску с лесопилки");
            return true;
        }

        if (WorkCoordinator.ShouldCoordinate())
        {
            if (WorkCoordinator.Instance.IsActiveDeliveryCourier(this)
                || assignedWorkRole == WorkCoordinator.WorkRole.DeliverToConstruction)
            {
                StartCoroutine(WaitAndRetryRole(1.5f));
                return true;
            }

            WorkCoordinator.Instance.ReleaseDelivery(this);
        }

        EnterIdleNoAssignment();
        return true;
    }

    bool FindJobSawmillWorker()
    {
        if (WorkCoordinator.ShouldCoordinate()
            && assignedWorkRole != WorkCoordinator.WorkRole.SawmillWorker)
        {
            return false;
        }

        if (IsOnSawmillCooldown())
        {
            EnterIdleNoAssignment();
            return true;
        }

        if (HasPlank)
        {
            if (WorkCoordinator.ShouldCoordinate() && assignedWorkRole != WorkCoordinator.WorkRole.SawmillToStorage)
            {
                EnterIdleNoAssignment();
                return true;
            }

            if (storage != null && !storage.IsFull())
                GoToStorage(storage);
            else
                EnterIdleNoAssignment();
            return true;
        }

        Sawmill sawmill = FindObjectOfType<Sawmill>();
        if (sawmill == null)
            return false;

        if (storage != null && storage.IsFull())
        {
            if (WorkCoordinator.ShouldCoordinate()
                && WorkCoordinator.Instance.CountAvailableUnits() >= 3)
            {
                GoToSawmill(sawmill);
                Debug.Log("[Лесопилка] Склад полон — пилю, доски оставлю у выхода");
                return true;
            }

            EnterIdleNoAssignment();
            return true;
        }

        GoToSawmill(sawmill);
        Debug.Log("[Лесопилка] Иду пилить доски");
        return true;
    }

    bool FindJobSawmillToStorage()
    {
        if (WorkCoordinator.ShouldCoordinate()
            && assignedWorkRole != WorkCoordinator.WorkRole.SawmillToStorage)
        {
            return false;
        }

        if (HasPlank)
        {
            if (storage != null)
            {
                GoToStorage(storage);
                Debug.Log("[Склад] Несу доску с лесопилки на склад");
                return true;
            }

            return false;
        }

        Sawmill sawmill = FindObjectOfType<Sawmill>();
        if (sawmill == null)
            return false;

        if (storage != null && storage.IsFull())
        {
            EnterWaitingForSawmillPlanks(sawmill);
            return true;
        }

        if (sawmill.PlanksWaitingPickup > 0)
        {
            GoToSawmillPickup(sawmill);
            Debug.Log("[Склад] На лесопилке есть доски — иду забирать");
            return true;
        }

        EnterWaitingForSawmillPlanks(sawmill);
        return true;
    }

    void GoToSawmillPickup(Sawmill sawmill, bool deliverDirectlyToConstruction = false, ConstructionSite siteHint = null)
    {
        if (sawmill == null || sawmill.PlanksWaitingPickup <= 0)
        {
            if (deliverDirectlyToConstruction)
            {
                StartCoroutine(WaitAndRetryRole(1f));
                return;
            }

            EnterWaitingForSawmillPlanks(sawmill);
            return;
        }

        StopWaitingForSawmillPlanks();
        directSawmillPickupToConstruction = deliverDirectlyToConstruction;

        if (siteHint != null)
            currentSite = siteHint;

        if (TryTakeSawmillPickupPlank(sawmill))
            return;

        ReleaseReservedEntryPoint();
        pickupSawmillTarget = sawmill;
        headingToSawmillPickup = true;
        directSawmillPickupToConstruction = deliverDirectlyToConstruction;
        currentState = UnitState.MovingToSite;
        if (!deliverDirectlyToConstruction)
            currentSite = null;
        targetEntryPoint = null;
        targetPosition = sawmill.GetExitPosition();

        SetWalkAnimation();
        Debug.Log(deliverDirectlyToConstruction
            ? "[Стройка] Иду к выходу лесопилки за доской для стройки"
            : "[Склад] Иду к выходу лесопилки за доской");
    }

    IEnumerator WaitAndRetryRole(float delay)
    {
        if (isFreeRoaming)
            yield break;

        WorkCoordinator.WorkRole retryRole = assignedWorkRole;
        currentState = UnitState.Idle;
        SetIdleAnimation();
        yield return new WaitForSeconds(delay);

        if (!isResting && !isSeekingRest)
        {
            if (retryRole != WorkCoordinator.WorkRole.Unassigned)
                BeginCoordinatedRole(retryRole);
            else
                ContinueAssignedWork();
        }
    }

    void FindJobLegacy()
    {
        if (WorkCoordinator.ShouldCoordinate())
        {
            EnterIdleNoAssignment();
            return;
        }

        ConstructionSite[] sites = FindObjectsOfType<ConstructionSite>();
        bool hasConstructionDemand = false;
        foreach (ConstructionSite site in sites)
        {
            if (site.NeedsPlanks())
            {
                hasConstructionDemand = true;
                break;
            }
        }

        if (!hasConstructionDemand)
        {
            currentSite = null;
            if (storage == null)
            {
                StartWandering();
                return;
            }

            // Нет активной стройки: если доска на руках, несем ее на склад.
            if (HasPlank)
            {
                currentState = UnitState.MovingToStorage;
                targetPosition = storage.GetDoorPosition();
                targetEntryPoint = null;
                SetWalkAnimation();
                Debug.Log("Строек нет, несу доску на склад");
                return;
            }

            // Если склад не полный, пополняем его через лесопилку.
            if (!storage.IsFull())
            {
                Sawmill sawmillForStorage = FindObjectOfType<Sawmill>();
                if (sawmillForStorage != null)
                {
                    GoToSawmill(sawmillForStorage);
                    return;
                }

                StartWandering();
                return;
            }

            // Склад полный - юнит свободен.
            StartWandering();
            return;
        }

        // Если у юнита есть доски - ищем стройку
        if (HasPlank)
        {
            foreach (ConstructionSite site in sites)
            {
                if (site.NeedsPlanks())
                {
                    currentSite = site;
                    currentState = UnitState.MovingToSite;
                    if (!TryAssignSiteEntryPoint(site))
                    {
                        currentState = UnitState.Idle;
                        SetIdleAnimation();
                        StartCoroutine(WaitAndRetrySite());
                        return;
                    }

                    SetWalkAnimation();
                    Debug.Log("Есть доски, иду на стройку");
                    return;
                }
            }

            // Строек больше нет - относим доску на склад, чтобы не зацикливаться на лесопилке.
            if (storage != null)
            {
                currentSite = null;
                currentState = UnitState.MovingToStorage;
                targetPosition = storage.GetDoorPosition();
                targetEntryPoint = null;
                SetWalkAnimation();
                Debug.Log("Несу доску на склад");
                return;
            }
        }

        // Если нет досок, проверяем склад
        if (storage != null && storage.planks > 0)
        {
            foreach (ConstructionSite site in sites)
            {
                if (site.NeedsPlanks())
                {
                    currentSite = site;
                    currentState = UnitState.MovingToStorage;
                    targetPosition = storage.GetDoorPosition();
                    targetEntryPoint = null;
                    SetWalkAnimation();
                    Debug.Log("На складе есть доски, иду за ними");
                    return;
                }
            }
        }

        // Если склад пустой, но на выходе лесопилки уже есть готовые доски,
        // берём их напрямую для стройки, не гоняя через склад.
        Sawmill sawmill = FindObjectOfType<Sawmill>();
        if (sawmill != null && sawmill.PlanksWaitingPickup > 0)
        {
            ConstructionSite targetSite = null;
            foreach (ConstructionSite site in sites)
            {
                if (site.NeedsPlanks())
                {
                    targetSite = site;
                    break;
                }
            }

            if (targetSite != null)
            {
                currentSite = targetSite;
                GoToSawmillPickup(sawmill, true, targetSite);
                Debug.Log("На складе нет досок — беру готовую доску с лесопилки сразу на стройку");
                return;
            }
        }

        // Если на складе нет досок и готовых досок у выхода тоже нет - идём работать на лесопилку
        if (sawmill != null)
        {
            currentState = UnitState.MovingToSite;
            currentSite = null;

            // Обязательно находим свободную точку входа и занимаем ее.
            // Если точки нет - ждем, а не идем в дверь без EntryPoint.
            EntryPoint nearestPoint = sawmill.GetNearestFreeEntryPoint(transform.position);
            if (nearestPoint != null)
            {
                if (nearestPoint.TryOccupy())
                {
                    targetEntryPoint = nearestPoint;
                    targetPosition = nearestPoint.transform.position;
                    Debug.Log($"Иду к точке входа лесопилки: {nearestPoint.name} на позиции {targetPosition}");
                }
                else
                {
                    Debug.Log("Точка входа лесопилки занята, жду...");
                    currentState = UnitState.Idle;
                    SetIdleAnimation();
                    StartCoroutine(WaitAndRetrySawmill());
                    return;
                }
            }
            else
            {
                Debug.LogWarning("У лесопилки нет свободных точек входа, жду...");
                currentState = UnitState.Idle;
                SetIdleAnimation();
                StartCoroutine(WaitAndRetrySawmill());
                return;
            }

            SetWalkAnimation();
            Debug.Log("На складе нет досок, иду на лесопилку");
            return;
        }

        StartWandering();
    }

    public void GoToSawmill(Sawmill targetSawmill)
    {
        if (IsOnSawmillCooldown())
        {
            FindJob();
            return;
        }

        if (WorkCoordinator.ShouldCoordinate() && assignedWorkRole != WorkCoordinator.WorkRole.SawmillWorker)
        {
            FindJob();
            return;
        }

        ReleaseReservedEntryPoint();
        currentState = UnitState.MovingToSite;
        currentSite = null;

        EntryPoint nearestPoint = targetSawmill.GetNearestFreeEntryPoint(transform.position);
        if (nearestPoint != null && nearestPoint.TryOccupy())
        {
            targetEntryPoint = nearestPoint;
            targetPosition = nearestPoint.transform.position;
        }
        else
        {
            currentState = UnitState.Idle;
            SetIdleAnimation();
            StartCoroutine(WaitAndRetrySawmill());
            return;
        }

        SetWalkAnimation();
        Debug.Log("Иду на лесопилку");
    }

    public void GoToStorage(Storage targetStorage)
    {
        if (WorkCoordinator.ShouldCoordinate())
        {
            bool allowed = assignedWorkRole == WorkCoordinator.WorkRole.SawmillToStorage
                           || assignedWorkRole == WorkCoordinator.WorkRole.DeliverToConstruction
                           || (assignedWorkRole == WorkCoordinator.WorkRole.SawmillWorker && HasPlank);

            if (!allowed)
            {
                FindJob();
                return;
            }

            if (assignedWorkRole == WorkCoordinator.WorkRole.DeliverToConstruction && !HasPlank
                && !WorkCoordinator.Instance.TryClaimDelivery(this))
            {
                FindJob();
                return;
            }
        }

        ReleaseReservedEntryPoint();
        storage = targetStorage;

        if (assignedWorkRole != WorkCoordinator.WorkRole.DeliverToConstruction)
            currentSite = null;

        currentState = UnitState.MovingToStorage;
        targetPosition = storage.GetDoorPosition();
        targetEntryPoint = null;
        SetWalkAnimation();
        Debug.Log("Иду на склад");
    }

    void StopWandering()
    {
        if (wanderRoutine != null)
        {
            StopCoroutine(wanderRoutine);
            wanderRoutine = null;
        }
    }

    void StartWandering()
    {
        StopWandering();
        currentState = UnitState.Idle;
        SetIdleAnimation();
        GetNewTarget();
    }

    void GetNewTarget()
    {
        if (mainCamera == null) return;

        Vector3 screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.nearClipPlane));

        float minX = mainCamera.transform.position.x - screenBounds.x + objectWidth;
        float maxX = mainCamera.transform.position.x + screenBounds.x - objectWidth;
        float minY = mainCamera.transform.position.y - screenBounds.y + objectHeight;
        float maxY = mainCamera.transform.position.y + screenBounds.y - objectHeight;

        int attempts = 0;
        do
        {
            targetPosition = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0);
            attempts++;
        }
        while (IsPositionBlocked(targetPosition) && attempts < 10);

        wanderRoutine = StartCoroutine(MoveToWanderTarget());
    }

    IEnumerator MoveToWanderTarget()
    {
        if (!isFreeRoaming)
            yield break;

        currentState = UnitState.MovingToSite;
        SetWalkAnimation();

        while (isFreeRoaming && Vector3.Distance(transform.position, targetPosition) > stopDistance)
        {
            if (currentState != UnitState.MovingToSite)
                yield break;

            Vector3 direction = (targetPosition - transform.position).normalized;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, raycastDistance, obstacleLayer);
            float wanderDist = Vector3.Distance(transform.position, targetPosition);

            Sawmill wanderSawmill = pickupSawmillTarget;
            if (hit.collider != null && ShouldIgnoreSawmillCollider(hit.collider, wanderDist, wanderSawmill))
                hit = new RaycastHit2D();

            if (hit.collider != null)
            {
                Vector3 avoidDirection = GetAvoidanceDirection(direction, hit, wanderDist, wanderSawmill);
                if (avoidDirection != Vector3.zero)
                {
                    if (rb != null)
                    {
                        rb.linearVelocity = avoidDirection * movementSpeed;
                    }
                    else
                    {
                        transform.position += avoidDirection * movementSpeed * Time.deltaTime;
                    }
                }
                else
                {
                    GetNewTarget();
                    yield break;
                }
            }
            else
            {
                if (rb != null)
                {
                    rb.linearVelocity = direction * movementSpeed;
                }
                else
                {
                    transform.position += direction * movementSpeed * Time.deltaTime;
                }
            }

            float xDifference = targetPosition.x - transform.position.x;
            if (Mathf.Abs(xDifference) > directionThreshold)
            {
                transform.localScale = new Vector3(xDifference > 0 ? 1 : -1, 1, 1);
            }

            yield return null;
        }

        StopMoving();
        SetIdleAnimation();
        SwitchIdleVariant();

        yield return new WaitForSeconds(Random.Range(1.5f, 3f));

        if (!isFreeRoaming)
        {
            GetNewTarget();
            yield break;
        }

        wanderRoutine = null;

        if (!isFreeRoaming)
            yield break;

        FindJob();
        if (!isFreeRoaming)
            yield break;

        GetNewTarget();
    }

    bool IsPositionBlocked(Vector3 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, 0.5f, obstacleLayer);
        return hit != null;
    }

    public void SetHasPlank(bool value)
    {
        hasPlank = value;
        if (!value)
        {
            // Следующая доска должна идти через ту же точку входа стройки (если она валидна).
            targetEntryPoint = null;
        }

        if (plankVisual != null)
            plankVisual.SetActive(value);

        Debug.Log($"UnitAI.SetHasPlank: hasPlank = {value} для юнита {gameObject.name}");
    }

    void OnDestroy()
    {
        if (WorkCoordinator.Instance != null)
            WorkCoordinator.Instance.ReleaseUnit(this);

        if (storage != null)
            storage.SetCollisionIgnoredForUnit(this, false);

        Sawmill sawmill = FindObjectOfType<Sawmill>();
        if (sawmill != null)
            sawmill.SetCollisionIgnoredForUnit(this, false);

        if (targetEntryPoint != null)
        {
            targetEntryPoint.Vacate();
        }
    }

    public bool HasWood
    {
        get { return hasWood; }
    }

    public int WoodAmount
    {
        get { return woodAmount; }
    }

    // Методы для управления бревнами
    public void SetHasWood(bool value)
    {
        hasWood = value;
        Debug.Log($"UnitAI.SetHasWood: hasWood = {value} для юнита {gameObject.name}");
    }

    public void SetWoodAmount(int amount)
    {
        woodAmount = amount;
        Debug.Log($"UnitAI.SetWoodAmount: woodAmount = {amount}");
    }

    private bool TryAssignSiteEntryPoint(ConstructionSite site)
    {
        if (site == null)
            return false;

        // Сначала пытаемся использовать "любимую" точку, чтобы не прыгать между входами.
        if (preferredSiteEntryPoint != null &&
            preferredSiteEntryPoint.parentBuilding == site &&
            !preferredSiteEntryPoint.isOccupied &&
            preferredSiteEntryPoint.TryOccupy())
        {
            targetEntryPoint = preferredSiteEntryPoint;
            targetPosition = preferredSiteEntryPoint.transform.position;
            Debug.Log($"Использую сохраненную точку входа: {preferredSiteEntryPoint.name}");
            return true;
        }

        EntryPoint nearestPoint = site.GetNearestFreeEntryPoint(transform.position);
        if (nearestPoint != null && nearestPoint.TryOccupy())
        {
            preferredSiteEntryPoint = nearestPoint;
            targetEntryPoint = nearestPoint;
            targetPosition = nearestPoint.transform.position;
            Debug.Log($"Выбрана новая точка входа: {nearestPoint.name}");
            return true;
        }

        targetEntryPoint = null;
        return false;
    }

    private void ReleaseReservedEntryPoint()
    {
        if (targetEntryPoint != null)
        {
            targetEntryPoint.Vacate();
            targetEntryPoint = null;
        }
    }
}
