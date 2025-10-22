using UnityEngine;

public class ConstructionSite : MonoBehaviour
{
    public int requiredPlanks = 10;
    public int deliveredPlanks = 0;
    public bool isComplete = false;

    [Header("Construction Sprites")]
    public Sprite[] constructionSprites; // 4 ������� ��� ������ ������ �������������
    private SpriteRenderer spriteRenderer;

    [Header("Construction Effects")]
    public ParticleSystem buildEffect;
    public ParticleSystem completeEffect;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // ������������� ��������� ������ (������ ������)
        UpdateConstructionSprite();
    }

    public bool NeedsPlanks()
    {
        return !isComplete && deliveredPlanks < requiredPlanks;
    }

    public void DeliverPlank()
    {
        deliveredPlanks++;
        Debug.Log($"���������� ����� �� �������: {deliveredPlanks}/{requiredPlanks}");

        // ��������� ������ ��� �������� ������ �����
        UpdateConstructionSprite();

        // ������������� ������ �������������
        if (buildEffect != null)
        {
            buildEffect.Play();
        }

        if (deliveredPlanks >= requiredPlanks)
        {
            CompleteConstruction();
        }
    }

    private void UpdateConstructionSprite()
    {
        if (constructionSprites == null || constructionSprites.Length != 4)
        {
            Debug.LogWarning("�� ��������� ������� �������������! ����� 4 �������.");
            return;
        }

        // ���������� ������� ������ ������������� (0-3)
        int stage = Mathf.FloorToInt((float)deliveredPlanks / requiredPlanks * 3);
        stage = Mathf.Clamp(stage, 0, 3);

        spriteRenderer.sprite = constructionSprites[stage];

        // ���� ��� ��������� ������, �� ������ ��� �� ���������
        if (stage == 3 && !isComplete)
        {
            // ����� �������� ��������� ������ "����� ������"
            // ��� �������� ��� ���� - ��������� ������ ����� ������������ ��� ����������
        }
    }

    private void CompleteConstruction()
    {
        isComplete = true;

        // ������������� ��������� ������ (��������� � �������)
        if (constructionSprites != null && constructionSprites.Length >= 4)
        {
            spriteRenderer.sprite = constructionSprites[3];
        }

        // ������ ���������� �������������
        if (completeEffect != null)
        {
            completeEffect.Play();
        }

        // ����� �������� �������������� ��������:
        // - �������� ��������� �������� ������
        // - �������� ���������������� (��������, ��� ������� - ����������� ��������)
        // - ������������� ����
        // - ��������� ������� � ���������� �������������

        Debug.Log("������ ���������!");

        // ��������� ��� ������� ��������� �������, ���� ������ �� �����
        // StartCoroutine(RemoveConstructionComponent());
    }

    // �����������: ������� ��������� ������������� ����� ��������� �����
    private System.Collections.IEnumerator RemoveConstructionComponent()
    {
        yield return new WaitForSeconds(2f);

        // ��������������� ������ (������� "����������")
        gameObject.name = gameObject.name.Replace("(��������)", "");

        // ������� ���� ������, ��� ��� ������������� ���������
        Destroy(this);
    }

    // ������������ ��������� � ��������� (�����������)
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // ������ ��������-��� ��� �������
        Vector3 startPos = transform.position + Vector3.up * 1.5f;
        float progress = (float)deliveredPlanks / requiredPlanks;

        // ��� ��������-����
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(startPos + Vector3.right * 0.5f, new Vector3(1, 0.2f, 0));

        // ���������� ��������-����
        Gizmos.color = Color.Lerp(Color.red, Color.green, progress);
        Gizmos.DrawCube(startPos + Vector3.right * (progress * 0.5f),
                       new Vector3(progress, 0.15f, 0));
    }
}