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
        Debug.Log("Столкновение с: " + collision.gameObject.name);

        if (collision.gameObject.CompareTag("TownHall"))
        {
            Debug.Log("Столкнулись с ратушей!");

            currentBuildingCollider = collision.collider;

            // Определяем направление от здания к юниту
            Vector3 direction = transform.position - collision.transform.position;
            Debug.Log("Направление: " + direction);

            // Вычисляем угол между направлением и осью X (справа)
            float angle = Mathf.Abs(Vector3.Angle(direction, Vector3.right));
            Debug.Log("Угол подхода: " + angle);

            // Если подходим сбоку (справа или слева)
            if (angle < 45f || angle > 135f)
            {
                Debug.Log("Подход СБОКУ - разрешаем проход");

                // Разрешаем проход сквозь здание
                Physics2D.IgnoreCollision(GetComponent<Collider2D>(), collision.collider, true);

                // Меняем порядок отрисовки - юнит позади здания
                SpriteRenderer buildingRenderer = collision.gameObject.GetComponent<SpriteRenderer>();
                SpriteRenderer unitRenderer = GetComponent<SpriteRenderer>();

                if (buildingRenderer != null && unitRenderer != null)
                {
                    unitRenderer.sortingOrder = buildingRenderer.sortingOrder - 1;
                }

                // Восстанавливаем коллайдер через 2 секунды
                Invoke("ReenableCollision", 2f);
            }
            else
            {
                Debug.Log("Подход СВЕРХУ/СНИЗУ - блокируем движение");

                // Подходим сверху/снизу - блокируем движение
                if (unitAI != null)
                {
                    Debug.Log("Вызываем StopAndFindNewTarget");
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
        Debug.Log("Восстанавливаем столкновение");

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