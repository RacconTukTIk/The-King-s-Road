using UnityEngine;
using System.Collections;

public class ConstructionSite : MonoBehaviour
{
    public int requiredPlanks = 10;
    public int deliveredPlanks = 0;
    public bool isComplete = false;

    [Header("Construction Sprites")]
    public Sprite[] constructionSprites; // 5 �������� ��� ������ ������ �������������
    private SpriteRenderer spriteRenderer;

    [Header("Construction Effects")]
    public ParticleSystem buildEffect;
    public ParticleSystem completeEffect;

    [Header("Position Settings")]
    public Vector3[] spriteOffsets; // �������� ��� ������� ������� (���� �����)
    public bool useCustomOffsets = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // ��������� ������ ��������
        if (useCustomOffsets && (spriteOffsets == null || spriteOffsets.Length != 5))
        {
            Debug.LogWarning("�� ��������� �������� ��� ��������! ��������� useCustomOffsets.");
            useCustomOffsets = false;
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
        if (constructionSprites == null || constructionSprites.Length != 5)
        {
            Debug.LogWarning("�� ��������� ������� �������������! ����� 5 ��������.");
            return;
        }

        // ���������� ������� ������ ������������� (0-4)
        int stage = Mathf.FloorToInt((float)deliveredPlanks / requiredPlanks * 4);
        stage = Mathf.Clamp(stage, 0, 4);

        spriteRenderer.sprite = constructionSprites[stage];

        // ��������� �������� ������� ���� �����
        if (useCustomOffsets)
        {
            ApplySpriteOffset(stage);
        }

        // ���� ��� ��������� ������, �� ������ ��� �� ���������
        if (stage == 4 && !isComplete)
        {
            // ����� �������� ��������� ������ "����� ������"
            // ��� �������� ��� ���� - ��������� ������ ����� ������������ ��� ����������
        }
    }

    private void ApplySpriteOffset(int stage)
    {
        // ��������� ������������ ������� � ��������� ��������
        Vector3 originalPosition = transform.position;

        // ���� ��� ������ ������, ���������� ������������ ������� ��� �������
        if (stage == 0 && !isComplete)
        {
            // ����� ��������� ������� ������� ���� �����
        }

        // ��������� �������� ��� ������� ������
        transform.position = originalPosition + spriteOffsets[stage];
    }

    private void CompleteConstruction()
    {
        isComplete = true;

        // ������������� ��������� ������ (��������� � �������)
        if (constructionSprites != null && constructionSprites.Length >= 5)
        {
            spriteRenderer.sprite = constructionSprites[4];

            // ��������� ��������� �������� ���� ������������
            if (useCustomOffsets)
            {
                ApplySpriteOffset(4);
            }
        }

        // ������ ���������� �������������
        if (completeEffect != null)
        {
            completeEffect.Play();
        }

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