using UnityEngine;

public class SortingOrderByY : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // ��� ���� ������ �� Y, ��� ������ �� �������� (������� sortingOrder)
        // ��� ���� �� Y, ��� ����� �������� (������� sortingOrder)
        spriteRenderer.sortingOrder = Mathf.RoundToInt(transform.position.y * 100f) * -1;
    }
}