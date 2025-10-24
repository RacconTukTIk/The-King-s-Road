using UnityEngine;
using System.Collections;

public class ProgressBarFill : MonoBehaviour
{
    [Header("Fill Settings")]
    public SpriteRenderer fillRenderer; // �������� ��� ����������
    public Color startColor = Color.red;
    public Color endColor = Color.green;
    public AnimationCurve fillCurve = AnimationCurve.Linear(0, 0, 1, 1);

    private Material fillMaterial;
    private float currentProgress = 0f;

    void Start()
    {
        if (fillRenderer != null)
        {
            // ������� �������� ��� ����������
            fillMaterial = fillRenderer.material;
            UpdateFillVisual();
        }
    }

    public void SetProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);
        UpdateFillVisual();
    }

    private void UpdateFillVisual()
    {
        if (fillMaterial == null) return;

        // ������������� ������� ���������� ����� ������ (���� ������������ ��������� ������)
        fillMaterial.SetFloat("_FillAmount", currentProgress);

        // ��� �������� scale ��� �������� ����������
        float curvedProgress = fillCurve.Evaluate(currentProgress);
        fillRenderer.transform.localScale = new Vector3(curvedProgress, 1f, 1f);

        // �������� ����
        fillRenderer.color = Color.Lerp(startColor, endColor, currentProgress);
    }

    // �������� ����������
    public IEnumerator AnimateFill(float targetProgress, float duration)
    {
        float startProgress = currentProgress;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            currentProgress = Mathf.Lerp(startProgress, targetProgress, timer / duration);
            UpdateFillVisual();
            yield return null;
        }

        currentProgress = targetProgress;
        UpdateFillVisual();
    }
}