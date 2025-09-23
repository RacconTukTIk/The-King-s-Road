using UnityEngine;

public class UnitAI : MonoBehaviour
{
    private Animator animator;
    private Vector3 targetPosition;
    public float movementSpeed = 2f;
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;

    // ��� ����������� ��������
    private Camera mainCamera;
    private float objectWidth;
    private float objectHeight;

    // ��� �������������� ��������
    private float directionThreshold = 0.5f;
    private bool isWaiting = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        // �������� ������� ���������� ����� ��� ������
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        objectWidth = sr.bounds.extents.x;
        objectHeight = sr.bounds.extents.y;

        GetNewTarget();
    }

    void Update()
    {
        if (isWaiting) return;

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

        // ���� ����� �� ���� - �������� ����� ����� ��������� �����
        if (!isMoving)
        {
            isWaiting = true;
            Invoke("GetNewTarget", Random.Range(minWaitTime, maxWaitTime));
        }
    }

    void GetNewTarget()
    {
        isWaiting = false;

        // ������� ����� ��������� ����� � �������� ��������� ������
        Vector3 screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.nearClipPlane));

        // ��������� ������� � ������ �������� ������ �����
        float minX = mainCamera.transform.position.x - screenBounds.x + objectWidth;
        float maxX = mainCamera.transform.position.x + screenBounds.x - objectWidth;
        float minY = mainCamera.transform.position.y - screenBounds.y + objectHeight;
        float maxY = mainCamera.transform.position.y + screenBounds.y - objectHeight;

        // �������� ����� �������� ������� (�� ������ ������)
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
        // ���������, ��� �� � ���� ������� ���������� ������ (��� TownHall)
        Collider2D hit = Physics2D.OverlapCircle(position, 0.5f);
        return hit != null && hit.CompareTag("TownHall");
    }
    public void StopAndFindNewTarget()
    {
        Debug.Log("StopAndFindNewTarget ������");

        // �������� ������� �������� ���� ����
        CancelInvoke("GetNewTarget");

        // ������������� �������� ����������
        isWaiting = true;

        // ������������� ������� ������� ��� ������� (���������������)
        targetPosition = transform.position;

        // ��������� �������� ������
        animator.SetBool("IsWalking", false);

        // ���� ������� ����� ������� ����� ����
        Invoke("DelayedNewTarget", 0.5f);
    }

    void DelayedNewTarget()
    {
        isWaiting = false;
        GetNewTarget();
    }
}