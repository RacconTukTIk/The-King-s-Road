    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

    public class Sawmill : FunctionalBuilding
    {
        [Header("Sawmill Settings")]
        public float productionTime = 3f;
        public int maxProductionPerVisit = 5;

        [Header("Door Settings")]
        public Transform doorPoint;
        public Transform exitPoint;
        public float enterExitTime = 0.5f;

        [Header("Audio")]
        public AudioClip workSound;
        [Range(0f, 1f)]
        public float workSoundVolume = 0.5f;

        private bool isWorking = false;
        private EntryPoint usedEntryPoint;
        private AudioSource audioSource;

        void Start()
        {
            // Сначала создаём точки входа (если их нет в инспекторе)
            if (entryPoints == null || entryPoints.Count == 0)
            {
                entryPoints = new List<EntryPoint>();
                CreateDefaultEntryPoints();

                // Обновляем parentBuilding для новых точек
                RefreshEntryPoints();
            }

            // Вызываем базовый Start (он найдёт точки, но они уже есть)
            base.Start();

            // AudioSource
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.loop = true;
                audioSource.playOnAwake = false;
                audioSource.volume = workSoundVolume;
            }

            if (doorPoint == null) CreateDefaultDoorPoints();

            Debug.Log($"Лесопилка инициализирована. Точек входа: {entryPoints.Count}");
        }

        void CreateDefaultEntryPoints()
        {
            // Удаляем старые точки, если есть
            foreach (var oldPoint in entryPoints)
            {
                if (oldPoint != null)
                    Destroy(oldPoint.gameObject);
            }
            entryPoints.Clear();

            // Точка входа спереди
            GameObject entryObj = new GameObject("EntryPoint_Front");
            entryObj.transform.SetParent(transform);
            entryObj.transform.localPosition = new Vector3(1.5f, 0, 0);
            EntryPoint frontPoint = entryObj.AddComponent<EntryPoint>();
            entryPoints.Add(frontPoint);
            Debug.Log($"Создана точка входа: {entryObj.name} на позиции {entryObj.transform.position}");

            // Точка входа сбоку
            GameObject entryObj2 = new GameObject("EntryPoint_Side");
            entryObj2.transform.SetParent(transform);
            entryObj2.transform.localPosition = new Vector3(0, 1.5f, 0);
            EntryPoint sidePoint = entryObj2.AddComponent<EntryPoint>();
            entryPoints.Add(sidePoint);
            Debug.Log($"Создана точка входа: {entryObj2.name} на позиции {entryObj2.transform.position}");
        }

        void CreateDefaultDoorPoints()
        {
            GameObject doorObj = new GameObject("DoorPoint");
            doorObj.transform.SetParent(transform);
            doorObj.transform.localPosition = new Vector3(1.5f, 0, 0);
            doorPoint = doorObj.transform;

            GameObject exitObj = new GameObject("ExitPoint");
            exitObj.transform.SetParent(transform);
            exitObj.transform.localPosition = new Vector3(1.5f, -0.5f, 0);
            exitPoint = exitObj.transform;
        }

        public override void OnConstructionComplete()
        {
            base.OnConstructionComplete();
            Debug.Log("Лесопилка построена!");
        }

        public Vector3 GetDoorPosition() => doorPoint != null ? doorPoint.position : transform.position;
        public Vector3 GetExitPosition() => exitPoint != null ? exitPoint.position : GetDoorPosition();

        public override EntryPoint GetNearestFreeEntryPoint(Vector3 unitPosition)
        {
            if (entryPoints == null || entryPoints.Count == 0)
            {
                Debug.LogWarning("У лесопилки нет точек входа!");
                return null;
            }

            EntryPoint nearest = null;
            float minDistance = float.MaxValue;

            foreach (var point in entryPoints)
            {
                if (point != null && !point.isOccupied)
                {
                    float distance = Vector3.Distance(unitPosition, point.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = point;
                    }
                }
            }

            if (nearest == null)
                Debug.Log("Все точки входа лесопилки заняты");
            else
                Debug.Log($"Найдена свободная точка входа: {nearest.name}");

            return nearest;
        }

        public override void Interact(UnitAI unit, EntryPoint entryPoint)
        {
            Debug.Log($"Лесопилка: юнит {unit.name} пытается войти через {entryPoint?.name}");

            if (isWorking)
            {
                Debug.Log("Лесопилка занята");
                entryPoint?.Vacate();
                unit.FindJob();
                return;
            }

            usedEntryPoint = entryPoint;
            StartCoroutine(EnterAndWork(unit));
        }

        private IEnumerator EnterAndWork(UnitAI unit)
        {
            isWorking = true;
            Debug.Log($"{unit.name} начал вход в лесопилку");

            // Отключаем коллайдер здания
            if (buildingCollider != null)
            {
                buildingCollider.enabled = false;
                Debug.Log("Коллайдер лесопилки отключен");
            }

            // Скрываем юнита
            SpriteRenderer unitRenderer = unit.GetComponent<SpriteRenderer>();
            Collider2D unitCollider = unit.GetComponent<Collider2D>();
            if (unitRenderer != null) unitRenderer.enabled = false;
            if (unitCollider != null) unitCollider.enabled = false;
            if (unit.plankVisual != null) unit.plankVisual.SetActive(false);

            yield return new WaitForSeconds(enterExitTime);

            // Включаем звук работы
            if (workSound != null && audioSource != null)
            {
                audioSource.clip = workSound;
                audioSource.Play();
            }

            // Производство досок
            for (int i = 0; i < maxProductionPerVisit; i++)
            {
                Debug.Log($"Производство доски {i + 1}/{maxProductionPerVisit}");
                yield return new WaitForSeconds(productionTime);
            }

            // Выключаем звук
            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

            // Юнит забирает доски
            unit.SetHasPlank(true);
            Debug.Log($"Произведено {maxProductionPerVisit} досок");

            // Выход
            if (exitPoint != null)
            {
                unit.transform.position = exitPoint.position;
                Debug.Log($"Юнит перемещен на выход: {exitPoint.position}");
            }

            // Показываем юнита
            if (unitRenderer != null) unitRenderer.enabled = true;
            if (unitCollider != null) unitCollider.enabled = true;
            if (unit.plankVisual != null && unit.HasPlank) unit.plankVisual.SetActive(true);

            yield return new WaitForSeconds(enterExitTime);

            // Включаем коллайдер обратно
            if (buildingCollider != null)
            {
                buildingCollider.enabled = true;
                Debug.Log("Коллайдер лесопилки включен");
            }

            usedEntryPoint?.Vacate();
            isWorking = false;

            Debug.Log($"{unit.name} вышел из лесопилки с досками");
            unit.FindJob();
        }

        private void OnDrawGizmos()
        {
            if (doorPoint != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(doorPoint.position, 0.2f);
            }
            if (exitPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(exitPoint.position, 0.2f);
            }

            if (entryPoints != null)
            {
                foreach (var point in entryPoints)
                {
                    if (point != null)
                    {
                        Gizmos.color = point.isOccupied ? Color.red : Color.cyan;
                        Gizmos.DrawSphere(point.transform.position, 0.25f);
                    }
                }
            }
        }
    }