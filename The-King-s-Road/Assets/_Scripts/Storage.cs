using UnityEngine;

public class Storage : MonoBehaviour
{
    public int planks = 10;

    public bool TakePlank()
    {
        if (planks > 0)
        {
            planks--;
            Debug.Log($"����� ����� �� ������. ��������: {planks}");
            return true;
        }
        return false;
    } 
    
    public void ReturnPlank()
    {
        planks++;
        Debug.Log($"����� ���������� �� �����. ����� �����: {planks}");
    }

}
