using UnityEngine;

public class TownHallScript : MonoBehaviour
{
    // Префаб твоего юнита, который будет спавниться
    public GameObject unitPrefab;
    // Место, откуда будут появляться юниты (опционально)
    public Transform spawnPoint;

    void Update()
    {
        // Проверяем, была ли нажата левая кнопка мыши в текущем кадре :cite[3]
        if (Input.GetMouseButtonDown(0))
        {
            // Создаем луч из камеры в направлении курсора мыши
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            // Если луч попал в коллайдер
            if (hit.collider != null)
            {
                // Проверяем, это ратуша? (Предполагается, что у ратуши есть тег "TownHall")
                if (hit.collider.name == "TownHall")
                {
                    SpawnUnit();
                }
            }
        }
    }

    void SpawnUnit()
    {
        // Определяем позицию для спавна
        Vector3 spawnPosition = (spawnPoint != null) ? spawnPoint.position : transform.position;

        // Создаем экземпляр префаба юнита на сцене
        Instantiate(unitPrefab, spawnPosition, Quaternion.identity);
        Debug.Log("Юнит создан!");
    }
}