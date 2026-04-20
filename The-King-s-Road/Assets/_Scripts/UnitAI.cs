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
    public enum UnitState { Idle, MovingToStorage, MovingToSite, Working, EnteringBuilding }
    public UnitState currentState = UnitState.Idle;

    private Storage storage;
    private ConstructionSite currentSite;
    private bool hasPlank = false;

    // Переменная для точки входа
    private EntryPoint targetEntryPoint;

    // Для управления анимациями
    private float idleTimer = 0f;
    public float idleSwitchTime = 3f;

    [Header("Collision Avoidance")]
    public LayerMask obstacleLayer;
    public float raycastDistance = 1.5f;
    public float stopDistance = 0.3f;

    [Header("Footstep Audio")]
    public float footstepInterval = 0.5f;
    private float footstepTimer = 0f;
    public float footstepVolume = 0.3f;

    [Header("Plank Visual")]
    public GameObject plankVisual;

    private bool hasWood = false;
    private int woodAmount = 0;

    // Rigidbody2D компонент
    private Rigidbody2D rb;

    // Public property для доступа к hasPlank
    public bool HasPlank
    {
        get { return hasPlank; }
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

        SetIdleAnimation();

        // Скрываем доску в начале
        if (plankVisual != null)
            plankVisual.SetActive(false);

        FindJob();
    }

    void Update()
    {
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

        if (currentState == UnitState.MovingToStorage || currentState == UnitState.MovingToSite)
        {
            MoveToTarget();
        }
    }

    void HandleFootstepAudio()
    {
        if (currentState == UnitState.MovingToStorage || currentState == UnitState.MovingToSite)
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
                ignoreStorageCollider = true;
            }
        }

        // Игнорируем коллайдер стройки, если идем к точке входа
        bool ignoreSiteCollider = false;
        if (currentState == UnitState.MovingToSite && currentSite != null && targetEntryPoint != null)
        {
            if (Vector3.Distance(targetPosition, targetEntryPoint.transform.position) < 0.1f)
            {
                ignoreSiteCollider = true;
            }
        }

        // ========== НОВЫЙ КОД: Игнорируем коллайдер лесопилки ==========
        bool ignoreSawmillCollider = false;
        Sawmill targetSawmill = null;

        // Проверяем, идем ли мы на лесопилку
        if (currentState == UnitState.MovingToSite && targetEntryPoint != null)
        {
            targetSawmill = targetEntryPoint.parentBuilding as Sawmill;
            if (targetSawmill != null)
            {
                if (Vector3.Distance(targetPosition, targetEntryPoint.transform.position) < 0.1f)
                {
                    ignoreSawmillCollider = true;
                }
            }
        }

        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToTarget, raycastDistance, obstacleLayer);

        // Игнорируем коллайдер склада
        if (ignoreStorageCollider && hit.collider != null && hit.collider.gameObject == storage.gameObject)
        {
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), hit.collider, true);
            hit = new RaycastHit2D();
        }

        // Игнорируем коллайдер стройки
        if (ignoreSiteCollider && hit.collider != null && hit.collider.gameObject == currentSite.gameObject)
        {
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), hit.collider, true);
            hit = new RaycastHit2D();
        }

        // ========== НОВЫЙ КОД: Игнорируем коллайдер лесопилки ==========
        if (ignoreSawmillCollider && hit.collider != null && targetSawmill != null && hit.collider.gameObject == targetSawmill.gameObject)
        {
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), hit.collider, true);
            hit = new RaycastHit2D();
        }

        Debug.DrawRay(transform.position, directionToTarget * raycastDistance, Color.blue);

        Vector3 finalDirection = directionToTarget;

        if (hit.collider != null)
        {
            finalDirection = GetAvoidanceDirection(directionToTarget, hit);

            if (finalDirection == Vector3.zero)
            {
                StopMoving();
                StartCoroutine(WaitAndRetry());
                return;
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

    Vector3 GetAvoidanceDirection(Vector3 originalDirection, RaycastHit2D obstacleHit)
    {
        Vector3 bestDirection = Vector3.zero;
        float bestScore = -Mathf.Infinity;

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 testDirection = Quaternion.Euler(0, 0, angle) * Vector3.right;

            if (!Physics2D.Raycast(transform.position, testDirection, raycastDistance, obstacleLayer))
            {
                float dotProduct = Vector3.Dot(testDirection, originalDirection);
                float distanceToTarget = Vector3.Distance(transform.position + testDirection * raycastDistance, targetPosition);
                float score = dotProduct * 2f - distanceToTarget;

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
        if (!Physics2D.Raycast(transform.position, retreatDirection, raycastDistance, obstacleLayer))
        {
            Debug.DrawRay(transform.position, retreatDirection * raycastDistance, Color.yellow);
            return retreatDirection;
        }

        return Vector3.zero;
    }

    IEnumerator WaitAndRetry()
    {
        StopMoving();

        isWaiting = true;
        yield return new WaitForSeconds(2f);
        isWaiting = false;

        if (currentState == UnitState.MovingToStorage || currentState == UnitState.MovingToSite)
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

        yield return StartCoroutine(currentStorage.EnterAndTakePlank(this, currentEntryPoint));

        Debug.Log($"Корутина склада завершена. hasPlank = {hasPlank}");

        if (hasPlank)
        {
            Debug.Log("Вышел со склада с доской, иду к стройке");
            currentState = UnitState.MovingToSite;

            if (currentSiteRef != null)
            {
                EntryPoint nearestPoint = currentSiteRef.GetNearestFreeEntryPoint(transform.position);

                if (nearestPoint != null && nearestPoint.TryOccupy())
                {
                    targetEntryPoint = nearestPoint;
                    targetPosition = nearestPoint.transform.position;
                    Debug.Log($"Иду к точке входа: {nearestPoint.name}");
                }
                else
                {
                    targetEntryPoint = null;
                    targetPosition = currentSiteRef.transform.position;
                    Debug.Log("Нет свободных точек входа, иду к центру стройки");
                }

                SetWalkAnimation();
            }
            else
            {
                Debug.LogError("currentSiteRef = null после выхода из склада!");
                FindJob();
            }
        }
        else
        {
            Debug.Log("На складе нет досок");
            currentState = UnitState.Idle;
            SetIdleAnimation();
            StartCoroutine(WaitAndRetryStorage());
        }
    }

    void OnReachedSite()
    {
        StopMoving();
        Debug.Log($"OnReachedSite: hasPlank={hasPlank}, currentSite={currentSite?.name}, targetEntryPoint={targetEntryPoint?.name}");

        // Проверяем, не идём ли мы на лесопилку
        if (targetEntryPoint != null && targetEntryPoint.parentBuilding is Sawmill)
        {
            Sawmill sawmill = targetEntryPoint.parentBuilding as Sawmill;
            Debug.Log($"ДОШЕЛ ДО ЛЕСОПИЛКИ! Иду на лесопилку {sawmill.name}");
            sawmill.Interact(this, targetEntryPoint);
            targetEntryPoint = null;
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
                EntryPoint nearestPoint = site.GetNearestFreeEntryPoint(transform.position);
                if (nearestPoint != null && nearestPoint.TryOccupy())
                {
                    Debug.Log($"Нашел ближайшую точку {nearestPoint.name}");
                    site.Interact(this, nearestPoint);
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

        OnReachedSite();
    }

    IEnumerator WaitAndRetryStorage()
    {
        StopMoving();
        yield return new WaitForSeconds(3f);

        if (storage != null)
        {
            currentState = UnitState.MovingToStorage;
            targetPosition = storage.GetDoorPosition();
            targetEntryPoint = null;
            SetWalkAnimation();
        }
        else
        {
            currentState = UnitState.Idle;
            SetIdleAnimation();
            FindJob();
        }
    }

    public void FindJob()
    {
        StopMoving();

        // Если у юнита есть доски - ищем стройку
        if (HasPlank)
        {
            ConstructionSite[] sites = FindObjectsOfType<ConstructionSite>();
            foreach (ConstructionSite site in sites)
            {
                if (site.NeedsPlanks())
                {
                    currentSite = site;
                    currentState = UnitState.MovingToSite;

                    EntryPoint nearestPoint = site.GetNearestFreeEntryPoint(transform.position);
                    if (nearestPoint != null && nearestPoint.TryOccupy())
                    {
                        targetEntryPoint = nearestPoint;
                        targetPosition = nearestPoint.transform.position;
                    }
                    else
                    {
                        targetEntryPoint = null;
                        targetPosition = site.transform.position;
                    }

                    SetWalkAnimation();
                    Debug.Log("Есть доски, иду на стройку");
                    return;
                }
            }
        }

        // Если нет досок, проверяем склад
        if (storage != null && storage.planks > 0)
        {
            ConstructionSite[] sites = FindObjectsOfType<ConstructionSite>();
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

        // Если на складе нет досок - идём на лесопилку
        Sawmill sawmill = FindObjectOfType<Sawmill>();
        if (sawmill != null)
        {
            currentState = UnitState.MovingToSite;
            currentSite = null;

            // ОБЯЗАТЕЛЬНО находим точку входа и занимаем её
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
                    Debug.Log("Точка входа занята, жду...");
                    targetEntryPoint = null;
                    targetPosition = sawmill.GetDoorPosition();
                }
            }
            else
            {
                Debug.LogWarning("У лесопилки нет свободных точек входа!");
                targetEntryPoint = null;
                targetPosition = sawmill.GetDoorPosition();
            }

            SetWalkAnimation();
            Debug.Log("На складе нет досок, иду на лесопилку");
            return;
        }

        StartWandering();
    }

    public void GoToSawmill(Sawmill targetSawmill)
    {
        currentState = UnitState.MovingToSite;

        EntryPoint nearestPoint = targetSawmill.GetNearestFreeEntryPoint(transform.position);
        if (nearestPoint != null && nearestPoint.TryOccupy())
        {
            targetEntryPoint = nearestPoint;
            targetPosition = nearestPoint.transform.position;
        }
        else
        {
            targetEntryPoint = null;
            targetPosition = targetSawmill.GetDoorPosition();
        }

        SetWalkAnimation();
        Debug.Log("Иду на лесопилку");
    }

    public void GoToStorage(Storage targetStorage)
    {
        storage = targetStorage;
        currentState = UnitState.MovingToStorage;
        targetPosition = storage.GetDoorPosition();
        targetEntryPoint = null;
        SetWalkAnimation();
        Debug.Log("Иду к складу за доской");
    }

    void StartWandering()
    {
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

        StartCoroutine(MoveToWanderTarget());
    }

    IEnumerator MoveToWanderTarget()
    {
        currentState = UnitState.MovingToSite;
        SetWalkAnimation();

        while (Vector3.Distance(transform.position, targetPosition) > stopDistance)
        {
            if (currentState != UnitState.MovingToSite)
                yield break;

            Vector3 direction = (targetPosition - transform.position).normalized;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, raycastDistance, obstacleLayer);

            if (hit.collider != null)
            {
                Vector3 avoidDirection = GetAvoidanceDirection(direction, hit);
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

        yield return new WaitForSeconds(Random.Range(2f, 5f));

        ConstructionSite[] sites = FindObjectsOfType<ConstructionSite>();
        foreach (ConstructionSite site in sites)
        {
            if (site.NeedsPlanks())
            {
                FindJob();
                yield break;
            }
        }

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

        if (plankVisual != null)
            plankVisual.SetActive(value);

        Debug.Log($"UnitAI.SetHasPlank: hasPlank = {value} для юнита {gameObject.name}");
    }

    void OnDestroy()
    {
        if (storage != null)
        {
            Collider2D unitCollider = GetComponent<Collider2D>();
            Collider2D storageCollider = storage.GetComponent<Collider2D>();
            if (unitCollider != null && storageCollider != null)
            {
                Physics2D.IgnoreCollision(unitCollider, storageCollider, false);
            }
        }

        // Восстанавливаем коллизию с лесопилкой
        Sawmill sawmill = FindObjectOfType<Sawmill>();
        if (sawmill != null)
        {
            Collider2D unitCollider = GetComponent<Collider2D>();
            Collider2D sawmillCollider = sawmill.GetComponent<Collider2D>();
            if (unitCollider != null && sawmillCollider != null)
            {
                Physics2D.IgnoreCollision(unitCollider, sawmillCollider, false);
            }
        }

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

}