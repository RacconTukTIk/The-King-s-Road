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

    // НОВОЕ: Система состояний и строительства
    public enum UnitState { Idle, MovingToStorage, MovingToSite, Working }
    public UnitState currentState = UnitState.Idle;

    private Storage storage;
    private ConstructionSite currentSite;
    private bool hasPlank = false;

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

        // Начинаем с поиска работы
        FindJob();
    }

    void Update()
    {
        // Обрабатываем только состояния движения
        if (currentState == UnitState.MovingToStorage || currentState == UnitState.MovingToSite)
        {
            MoveToTarget();
        }
    }

    void MoveToTarget()
    {
        // Двигаемся к цели
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, movementSpeed * Time.deltaTime);

        // Определяем направление с порогом для избежания дрожания
        float xDifference = targetPosition.x - transform.position.x;

        // Поворачиваем спрайт только если разница значительная
        if (Mathf.Abs(xDifference) > directionThreshold)
        {
            if (xDifference > 0)
            {
                // Движемся вправо
                transform.localScale = new Vector3(1, 1, 1);
            }
            else
            {
                // Движемся влево
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }

        // Управляем анимацией
        bool isMoving = (Vector3.Distance(transform.position, targetPosition) > 0.1f);
        animator.SetBool("IsWalking", isMoving);

        // Если дошли до цели - обрабатываем событие
        if (!isMoving)
        {
            OnReachedDestination();
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
        Debug.Log("Достигнут склад");

        if (storage.TakePlank())
        {
            hasPlank = true;
            currentState = UnitState.MovingToSite;
            targetPosition = currentSite.transform.position;
            Debug.Log("Взял доску, иду на стройку");
        }
        else
        {
            Debug.Log("На складе нет досок, жду...");
            // Ждем и пробуем снова
            StartCoroutine(WaitAndRetryStorage());
        }
    }

    void OnReachedSite()
    {
        Debug.Log("Достигнута стройка");

        if (hasPlank && currentSite != null)
        {
            currentSite.DeliverPlank();
            hasPlank = false;

            // Проверяем, нужно ли еще доски
            if (currentSite.NeedsPlanks())
            {
                // Идем за следующей доской
                currentState = UnitState.MovingToStorage;
                targetPosition = storage.transform.position;
                Debug.Log("Иду за следующей доской");
            }
            else
            {
                // Строительство завершено
                Debug.Log("Строительство завершено! Ищу новую работу");
                currentSite = null;
                FindJob();
            }
        }
    }

    IEnumerator WaitAndRetryStorage()
    {
        // Ждем 3 секунды и пробуем снова
        yield return new WaitForSeconds(3f);

        if (storage != null)
        {
            currentState = UnitState.MovingToStorage;
            targetPosition = storage.transform.position;
        }
        else
        {
            FindJob();
        }
    }

    void FindJob()
    {
        // Ищем все строящиеся здания на сцене
        ConstructionSite[] sites = FindObjectsOfType<ConstructionSite>();

        foreach (ConstructionSite site in sites)
        {
            if (site.NeedsPlanks())
            {
                currentSite = site;
                currentState = UnitState.MovingToStorage;
                targetPosition = storage.transform.position;
                Debug.Log("Нашел работу! Иду на склад");
                return;
            }
        }

        // Если работы нет - переходим в режим блуждания
        Debug.Log("Работы нет, перехожу в режим ожидания");
        StartWandering();
    }

    void StartWandering()
    {
        currentState = UnitState.Idle;
        GetNewTarget();
    }

    void GetNewTarget()
    {
        // Находим новую случайную точку в пределах видимости камеры
        Vector3 screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.nearClipPlane));

        // Вычисляем границы с учетом размеров самого юнита
        float minX = mainCamera.transform.position.x - screenBounds.x + objectWidth;
        float maxX = mainCamera.transform.position.x + screenBounds.x - objectWidth;
        float minY = mainCamera.transform.position.y - screenBounds.y + objectHeight;
        float maxY = mainCamera.transform.position.y + screenBounds.y - objectHeight;

        // Пытаемся найти валидную позицию
        int attempts = 0;
        do
        {
            targetPosition = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0);
            attempts++;
        }
        while (IsPositionBlocked(targetPosition) && attempts < 10);

        // Запускаем движение к цели
        currentState = UnitState.Idle;
        StartCoroutine(MoveToWanderTarget());
    }

    IEnumerator MoveToWanderTarget()
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            // Если появилась работа - прерываем блуждание
            if (currentState != UnitState.Idle)
                yield break;

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, movementSpeed * Time.deltaTime);

            // Анимация и поворот
            float xDifference = targetPosition.x - transform.position.x;
            if (Mathf.Abs(xDifference) > directionThreshold)
            {
                transform.localScale = new Vector3(xDifference > 0 ? 1 : -1, 1, 1);
            }
            animator.SetBool("IsWalking", true);

            yield return null;
        }

        // Достигли цели - ждем и ищем новую
        animator.SetBool("IsWalking", false);

        // Периодически проверяем, нет ли работы
        yield return new WaitForSeconds(Random.Range(2f, 5f));

        // Проверяем работу перед следующим блужданием
        ConstructionSite[] sites = FindObjectsOfType<ConstructionSite>();
        foreach (ConstructionSite site in sites)
        {
            if (site.NeedsPlanks())
            {
                FindJob();
                yield break;
            }
        }

        // Если работы нет - продолжаем блуждать
        GetNewTarget();
    }

    bool IsPositionBlocked(Vector3 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, 0.5f);
        return hit != null && hit.CompareTag("TownHall");
    }
}