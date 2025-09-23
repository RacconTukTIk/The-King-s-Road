using UnityEngine;

public class SimpleSmartCollision : MonoBehaviour
{
    private UnitAI unitAI;
    private Collider2D currentBuildingCollider;

    void Start()
    {
        unitAI = GetComponent<UnitAI>();

        if (unitAI == null)
        {
            Debug.LogError("UnitAI component not found on " + gameObject.name);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("������������ �: " + collision.gameObject.name);

        if (collision.gameObject.CompareTag("TownHall"))
        {
            Debug.Log("����������� � �������!");

            currentBuildingCollider = collision.collider;

            // ���������� ����������� �� ������ � �����
            Vector3 direction = transform.position - collision.transform.position;
            Debug.Log("�����������: " + direction);

            // ��������� ���� ����� ������������ � ���� X (������)
            float angle = Mathf.Abs(Vector3.Angle(direction, Vector3.right));
            Debug.Log("���� �������: " + angle);

            // ���� �������� ����� (������ ��� �����)
            if (angle < 45f || angle > 135f)
            {
                Debug.Log("������ ����� - ��������� ������");

                // ��������� ������ ������ ������
                Physics2D.IgnoreCollision(GetComponent<Collider2D>(), collision.collider, true);

                // ������ ������� ��������� - ���� ������ ������
                SpriteRenderer buildingRenderer = collision.gameObject.GetComponent<SpriteRenderer>();
                SpriteRenderer unitRenderer = GetComponent<SpriteRenderer>();

                if (buildingRenderer != null && unitRenderer != null)
                {
                    unitRenderer.sortingOrder = buildingRenderer.sortingOrder - 1;
                }

                // ��������������� ��������� ����� 2 �������
                Invoke("ReenableCollision", 2f);
            }
            else
            {
                Debug.Log("������ ������/����� - ��������� ��������");

                // �������� ������/����� - ��������� ��������
                if (unitAI != null)
                {
                    Debug.Log("�������� StopAndFindNewTarget");
                    unitAI.StopAndFindNewTarget();
                }
                else
                {
                    Debug.LogError("UnitAI is null!");
                }
            }
        }
    }

    void ReenableCollision()
    {
        Debug.Log("��������������� ������������");

        if (currentBuildingCollider != null)
        {
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), currentBuildingCollider, false);
            GetComponent<SpriteRenderer>().sortingOrder = 0;
            currentBuildingCollider = null;
        }
    }

    void OnDestroy()
    {
        CancelInvoke("ReenableCollision");
    }
}