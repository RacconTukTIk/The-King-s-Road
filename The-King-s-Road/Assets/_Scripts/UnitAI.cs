using UnityEngine;

public class UnitAI : MonoBehaviour
{
    private Animator animator;
    private Vector3 targetPosition;
    public float movementSpeed = 2f;
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;

    // Для ограничения движения
    private Camera mainCamera;
    private float objectWidth;
    private float objectHeight;

    // Для предотвращения дрожания
    private float directionThreshold = 0.5f;
    private bool isWaiting = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        // Получаем размеры коллайдера юнита для границ
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        objectWidth = sr.bounds.extents.x;
        objectHeight = sr.bounds.extents.y;

        GetNewTarget();
    }

    void Update()
    {
        if (isWaiting) return;

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

        // Если дошли до цели - выбираем новую через случайное время
        if (!isMoving)
        {
            isWaiting = true;
            Invoke("GetNewTarget", Random.Range(minWaitTime, maxWaitTime));
        }
    }

    void GetNewTarget()
    {
        isWaiting = false;

        // Находим новую случайную точку в пределах видимости камеры
        Vector3 screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.nearClipPlane));

        // Вычисляем границы с учетом размеров самого юнита
        float minX = mainCamera.transform.position.x - screenBounds.x + objectWidth;
        float maxX = mainCamera.transform.position.x + screenBounds.x - objectWidth;
        float minY = mainCamera.transform.position.y - screenBounds.y + objectHeight;
        float maxY = mainCamera.transform.position.y + screenBounds.y - objectHeight;

        // Пытаемся найти валидную позицию (не внутри ратуши)
        int attempts = 0;
        do
        {
            targetPosition = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0);
            attempts++;
        }
        while (IsPositionBlocked(targetPosition) && attempts < 10);
    }

    bool IsPositionBlocked(Vector3 position)
    {
        // Проверяем, нет ли в этой позиции коллайдера ратуши (тег TownHall)
        Collider2D hit = Physics2D.OverlapCircle(position, 0.5f);
        return hit != null && hit.CompareTag("TownHall");
    }
    public void StopAndFindNewTarget()
    {
        Debug.Log("StopAndFindNewTarget вызван");

        // Отменяем текущее ожидание если есть
        CancelInvoke("GetNewTarget");

        // Останавливаем движение немедленно
        isWaiting = true;

        // Устанавливаем текущую позицию как целевую (останавливаемся)
        targetPosition = transform.position;

        // Выключаем анимацию ходьбы
        animator.SetBool("IsWalking", false);

        // Ждем немного перед поиском новой цели
        Invoke("DelayedNewTarget", 0.5f);
    }

    void DelayedNewTarget()
    {
        isWaiting = false;
        GetNewTarget();
    }
}