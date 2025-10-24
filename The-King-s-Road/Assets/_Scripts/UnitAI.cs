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
    public enum UnitState { Idle, MovingToStorage, MovingToSite, Working }
    public UnitState currentState = UnitState.Idle;

    private Storage storage;
    private ConstructionSite currentSite;
    private bool hasPlank = false;

    // Для управления анимациями
    private float idleTimer = 0f;
    public float idleSwitchTime = 3f;

    [Header("Collision Avoidance")]
    public LayerMask obstacleLayer;
    public float raycastDistance = 1.5f;
    public float avoidanceForce = 5f;
    public float stopDistance = 0.3f;

    void Start()
    {
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        // Получаем размеры коллайдера юнита для границ
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        objectWidth = sr.bounds.extents.x;
        objectHeight = sr.bounds.extents.y;

        // Находим склад на сцене
        storage = FindObjectOfType<Storage>();

        // Устанавливаем начальную idle анимацию
        SetIdleAnimation();

        // Начинаем с поиска работы
        FindJob();
    }

    void Update()
    {
        // Управление сменой idle анимаций
        if (currentState == UnitState.Idle && !isWaiting)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleSwitchTime)
            {
                idleTimer = 0f;
                SwitchIdleVariant();
            }
        }

        // Обрабатываем только состояния движения
        if (currentState == UnitState.MovingToStorage || currentState == UnitState.MovingToSite)
        {
            MoveToTargetWithAvoidance();
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

    void MoveToTargetWithAvoidance()
    {
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        // Проверяем, есть ли препятствие прямо на пути
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToTarget, raycastDistance, obstacleLayer);
        Debug.DrawRay(transform.position, directionToTarget * raycastDistance, Color.blue);

        Vector3 finalDirection = directionToTarget;

        // Если на пути препятствие - избегаем его
        if (hit.collider != null)
        {
            finalDirection = GetAvoidanceDirection(directionToTarget, hit);

            // Если не можем найти безопасный путь - останавливаемся
            if (finalDirection == Vector3.zero)
            {
                SetIdleAnimation();
                StartCoroutine(WaitAndRetry());
                return;
            }
        }

        // Двигаемся только если достаточно далеко от цели
        if (distanceToTarget > stopDistance)
        {
            transform.position += finalDirection * movementSpeed * Time.deltaTime;
            SetWalkAnimation();
        }
        else
        {
            SetIdleAnimation();
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

        // Проверяем 8 направлений вокруг юнита
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 testDirection = Quaternion.Euler(0, 0, angle) * Vector3.right;

            // Проверяем, свободно ли это направление
            if (!Physics2D.Raycast(transform.position, testDirection, raycastDistance, obstacleLayer))
            {
                // Оцениваем направление: чем ближе к оригинальному направлению, тем лучше
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

        // Если нашли безопасное направление
        if (bestDirection != Vector3.zero)
        {
            return bestDirection;
        }

        // Если все направления заблокированы, пробуем отступить
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
        yield return new WaitForSeconds(2f);

        // После ожидания снова пытаемся двигаться к цели
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
        if (storage.TakePlank())
        {
            hasPlank = true;
            currentState = UnitState.MovingToSite;
            targetPosition = currentSite.transform.position;
            SetWalkAnimation();
        }
        else
        {
            currentState = UnitState.Idle;
            SetIdleAnimation();
            StartCoroutine(WaitAndRetryStorage());
        }
    }

    void OnReachedSite()
    {
        if (hasPlank && currentSite != null)
        {
            currentSite.DeliverPlank();
            hasPlank = false;

            if (currentSite.NeedsPlanks())
            {
                currentState = UnitState.MovingToStorage;
                targetPosition = storage.transform.position;
                SetWalkAnimation();
            }
            else
            {
                currentSite = null;
                currentState = UnitState.Idle;
                SetIdleAnimation();
                FindJob();
            }
        }
    }

    IEnumerator WaitAndRetryStorage()
    {
        yield return new WaitForSeconds(3f);

        if (storage != null)
        {
            currentState = UnitState.MovingToStorage;
            targetPosition = storage.transform.position;
            SetWalkAnimation();
        }
        else
        {
            currentState = UnitState.Idle;
            SetIdleAnimation();
            FindJob();
        }
    }

    void FindJob()
    {
        ConstructionSite[] sites = FindObjectsOfType<ConstructionSite>();

        foreach (ConstructionSite site in sites)
        {
            if (site.NeedsPlanks())
            {
                currentSite = site;
                currentState = UnitState.MovingToStorage;
                targetPosition = storage.transform.position;
                SetWalkAnimation();
                return;
            }
        }

        StartWandering();
    }

    void StartWandering()
    {
        currentState = UnitState.Idle;
        SetIdleAnimation();
        GetNewTarget();
    }

    void GetNewTarget()
    {
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
        SetWalkAnimation();

        while (Vector3.Distance(transform.position, targetPosition) > stopDistance)
        {
            if (currentState != UnitState.Idle)
                yield break;

            Vector3 direction = (targetPosition - transform.position).normalized;

            // Используем ту же логику избегания при блуждании
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, raycastDistance, obstacleLayer);

            if (hit.collider != null)
            {
                Vector3 avoidDirection = GetAvoidanceDirection(direction, hit);
                if (avoidDirection != Vector3.zero)
                {
                    transform.position += avoidDirection * movementSpeed * Time.deltaTime;
                }
                else
                {
                    // Если путь заблокирован, ищем новую цель
                    GetNewTarget();
                    yield break;
                }
            }
            else
            {
                transform.position += direction * movementSpeed * Time.deltaTime;
            }

            float xDifference = targetPosition.x - transform.position.x;
            if (Mathf.Abs(xDifference) > directionThreshold)
            {
                transform.localScale = new Vector3(xDifference > 0 ? 1 : -1, 1, 1);
            }

            yield return null;
        }

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
}