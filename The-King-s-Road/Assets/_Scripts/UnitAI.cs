using UnityEngine;
using System.Collections;

public class UnitAI : MonoBehaviour
{
    private Animator animator;
    private Vector3 targetPosition;
    public float movementSpeed = 2f;

    // ��� ����������� ��������
    private Camera mainCamera;
    private float objectWidth;
    private float objectHeight;

    // ��� �������������� ��������
    private float directionThreshold = 0.5f;

    // �����: ������� ��������� � �������������
    public enum UnitState { Idle, MovingToStorage, MovingToSite, Working }
    public UnitState currentState = UnitState.Idle;

    private Storage storage;
    private ConstructionSite currentSite;
    private bool hasPlank = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        // �������� ������� ���������� ����� ��� ������
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        objectWidth = sr.bounds.extents.x;
        objectHeight = sr.bounds.extents.y;

        // ������� ����� �� �����
        storage = FindObjectOfType<Storage>();

        // �������� � ������ ������
        FindJob();
    }

    void Update()
    {
        // ������������ ������ ��������� ��������
        if (currentState == UnitState.MovingToStorage || currentState == UnitState.MovingToSite)
        {
            MoveToTarget();
        }
    }

    void MoveToTarget()
    {
        // ��������� � ����
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, movementSpeed * Time.deltaTime);

        // ���������� ����������� � ������� ��� ��������� ��������
        float xDifference = targetPosition.x - transform.position.x;

        // ������������ ������ ������ ���� ������� ������������
        if (Mathf.Abs(xDifference) > directionThreshold)
        {
            if (xDifference > 0)
            {
                // �������� ������
                transform.localScale = new Vector3(1, 1, 1);
            }
            else
            {
                // �������� �����
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }

        // ��������� ���������
        bool isMoving = (Vector3.Distance(transform.position, targetPosition) > 0.1f);
        animator.SetBool("IsWalking", isMoving);

        // ���� ����� �� ���� - ������������ �������
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
        Debug.Log("��������� �����");

        if (storage.TakePlank())
        {
            hasPlank = true;
            currentState = UnitState.MovingToSite;
            targetPosition = currentSite.transform.position;
            Debug.Log("���� �����, ��� �� �������");
        }
        else
        {
            Debug.Log("�� ������ ��� �����, ���...");
            // ���� � ������� �����
            StartCoroutine(WaitAndRetryStorage());
        }
    }

    void OnReachedSite()
    {
        Debug.Log("���������� �������");

        if (hasPlank && currentSite != null)
        {
            currentSite.DeliverPlank();
            hasPlank = false;

            // ���������, ����� �� ��� �����
            if (currentSite.NeedsPlanks())
            {
                // ���� �� ��������� ������
                currentState = UnitState.MovingToStorage;
                targetPosition = storage.transform.position;
                Debug.Log("��� �� ��������� ������");
            }
            else
            {
                // ������������� ���������
                Debug.Log("������������� ���������! ��� ����� ������");
                currentSite = null;
                FindJob();
            }
        }
    }

    IEnumerator WaitAndRetryStorage()
    {
        // ���� 3 ������� � ������� �����
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
        // ���� ��� ���������� ������ �� �����
        ConstructionSite[] sites = FindObjectsOfType<ConstructionSite>();

        foreach (ConstructionSite site in sites)
        {
            if (site.NeedsPlanks())
            {
                currentSite = site;
                currentState = UnitState.MovingToStorage;
                targetPosition = storage.transform.position;
                Debug.Log("����� ������! ��� �� �����");
                return;
            }
        }

        // ���� ������ ��� - ��������� � ����� ���������
        Debug.Log("������ ���, �������� � ����� ��������");
        StartWandering();
    }

    void StartWandering()
    {
        currentState = UnitState.Idle;
        GetNewTarget();
    }

    void GetNewTarget()
    {
        // ������� ����� ��������� ����� � �������� ��������� ������
        Vector3 screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.nearClipPlane));

        // ��������� ������� � ������ �������� ������ �����
        float minX = mainCamera.transform.position.x - screenBounds.x + objectWidth;
        float maxX = mainCamera.transform.position.x + screenBounds.x - objectWidth;
        float minY = mainCamera.transform.position.y - screenBounds.y + objectHeight;
        float maxY = mainCamera.transform.position.y + screenBounds.y - objectHeight;

        // �������� ����� �������� �������
        int attempts = 0;
        do
        {
            targetPosition = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0);
            attempts++;
        }
        while (IsPositionBlocked(targetPosition) && attempts < 10);

        // ��������� �������� � ����
        currentState = UnitState.Idle;
        StartCoroutine(MoveToWanderTarget());
    }

    IEnumerator MoveToWanderTarget()
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            // ���� ��������� ������ - ��������� ���������
            if (currentState != UnitState.Idle)
                yield break;

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, movementSpeed * Time.deltaTime);

            // �������� � �������
            float xDifference = targetPosition.x - transform.position.x;
            if (Mathf.Abs(xDifference) > directionThreshold)
            {
                transform.localScale = new Vector3(xDifference > 0 ? 1 : -1, 1, 1);
            }
            animator.SetBool("IsWalking", true);

            yield return null;
        }

        // �������� ���� - ���� � ���� �����
        animator.SetBool("IsWalking", false);

        // ������������ ���������, ��� �� ������
        yield return new WaitForSeconds(Random.Range(2f, 5f));

        // ��������� ������ ����� ��������� ����������
        ConstructionSite[] sites = FindObjectsOfType<ConstructionSite>();
        foreach (ConstructionSite site in sites)
        {
            if (site.NeedsPlanks())
            {
                FindJob();
                yield break;
            }
        }

        // ���� ������ ��� - ���������� ��������
        GetNewTarget();
    }

    bool IsPositionBlocked(Vector3 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, 0.5f);
        return hit != null && hit.CompareTag("TownHall");
    }
}