using UnityEngine;
using System.Collections;

public class Storage : MonoBehaviour
{
    public int planks = 5;

    [Header("Door Settings")]
    public Transform doorPoint; // Точка у двери (куда подходить)
    public Transform exitPoint; // Точка выхода (откуда появляться)
    public float interactionTime = 1f; // Время "внутри" здания

    [Header("Collision Settings")]
    public Collider2D buildingCollider; // Коллайдер здания

    private bool isUnitInside = false;

    public bool TakePlank()
    {
        if (planks > 0)
        {
            planks--;
            Debug.Log($"Взята доска со склада. Осталось: {planks}");
            return true;
        }
        return false;
    }

    public void ReturnPlank()
    {
        planks++;
        Debug.Log($"Доска возвращена на склад. Всего досок: {planks}");
    }

    public Vector3 GetDoorPosition()
    {
        return doorPoint != null ? doorPoint.position : transform.position;
    }

    public Vector3 GetExitPosition()
    {
        return exitPoint != null ? exitPoint.position : doorPoint != null ? doorPoint.position : transform.position;
    }

    // Юнит входит, берет доску и выходит
    public IEnumerator EnterAndTakePlank(UnitAI unit)
    {
        isUnitInside = true;

        // Отключаем коллайдер для входа
        if (buildingCollider != null)
            buildingCollider.enabled = false;

        // Юнит "заходит" в здание
        unit.gameObject.SetActive(false);
        Debug.Log("Юнит зашел в склад");

        

        // ВНУТРИ берем доску (пока юнит невидим)
        bool plankTaken = TakePlank();

        if (plankTaken)
        {
            unit.SetHasPlank(true); // Новый метод в UnitAI
            Debug.Log("Юнит взял доску внутри склада");
        }

        // Выходим из здания
        if (exitPoint != null)
        {
            unit.transform.position = exitPoint.position;
        }

        // Юнит появляется
        unit.gameObject.SetActive(true);

        // Включаем коллайдер обратно
        if (buildingCollider != null)
        {
            yield return new WaitForSeconds(0.2f);
            buildingCollider.enabled = true;
        }

        Debug.Log("Юнит вышел из склада" + (plankTaken ? " с доской" : " без доски"));
        isUnitInside = false;
    }
}