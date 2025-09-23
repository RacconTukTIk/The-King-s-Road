using UnityEngine;

public class TownHallScript : MonoBehaviour
{
    // ������ ������ �����, ������� ����� ����������
    public GameObject unitPrefab;
    // �����, ������ ����� ���������� ����� (�����������)
    public Transform spawnPoint;

    void Update()
    {
        // ���������, ���� �� ������ ����� ������ ���� � ������� ����� :cite[3]
        if (Input.GetMouseButtonDown(0))
        {
            // ������� ��� �� ������ � ����������� ������� ����
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            // ���� ��� ����� � ���������
            if (hit.collider != null)
            {
                // ���������, ��� ������? (��������������, ��� � ������ ���� ��� "TownHall")
                if (hit.collider.name == "TownHall")
                {
                    SpawnUnit();
                }
            }
        }
    }

    void SpawnUnit()
    {
        // ���������� ������� ��� ������
        Vector3 spawnPosition = (spawnPoint != null) ? spawnPoint.position : transform.position;

        // ������� ��������� ������� ����� �� �����
        Instantiate(unitPrefab, spawnPosition, Quaternion.identity);
        Debug.Log("���� ������!");
    }
}