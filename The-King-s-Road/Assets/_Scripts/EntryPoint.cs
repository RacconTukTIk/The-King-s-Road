using UnityEngine;
using UnityEngine.UIElements;   

public class EntryPoint : MonoBehaviour
{
    public Building parentBuilding;
    public bool isOccupied = false;

    // Визуализация в редакторе
    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.red : Color.green;
        Gizmos.DrawSphere(transform.position, 0.2f);

        // Рисуем линию к зданию
        if (parentBuilding != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, parentBuilding.transform.position);
        }
    }

    public bool TryOccupy()
    {
        if (!isOccupied)
        {
            isOccupied = true;
            return true;
        }
        return false;
    }

    public void Vacate()
    {
        isOccupied = false;
    }
}