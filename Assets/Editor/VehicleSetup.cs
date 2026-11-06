using System;
using System.Linq;
using LabGame.Vehicles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LabGame.EditorTools
{
    /// <summary>
    /// Настройка техники в сцене: добавляет контроллеры на модель машины
    /// и самолёта, создаёт маршруты и коллайдер по габаритам модели.
    ///
    /// Повторный запуск безопасен: объекты и маршруты пересоздаются.
    /// </summary>
    public static class VehicleSetup
    {
        private const string DemoScene = "Assets/Scenes/Lab7_8_Demo.unity";

        private const string OkMarker = "VEHICLE_OK";
        private const string FailMarker = "VEHICLE_FAIL";

        [MenuItem("Lab 7-8/5. Настроить технику", priority = 5)]
        public static void Setup()
        {
            try
            {
                Run();
                Debug.Log(OkMarker);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{FailMarker}: {exception}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void Run()
        {
            if (!System.IO.File.Exists(DemoScene))
                throw new Exception($"нет сцены {DemoScene}");

            var scene = EditorSceneManager.OpenScene(DemoScene, OpenSceneMode.Single);

            var car = GameObject.Find("Car") ?? throw new Exception("в сцене нет объекта Car");
            var plane = GameObject.Find("Plane") ?? throw new Exception("в сцене нет объекта Plane");

            // ── Машина ───────────────────────────────────────────────
            var carController = EnsureComponent<GroundVehicleController>(car);

            var carBounds = CombineBounds(car);

            if (car.GetComponentInChildren<Collider>() == null)
            {
                var box = car.AddComponent<BoxCollider>();
                box.center = car.transform.InverseTransformPoint(carBounds.center);
                box.size = carBounds.size;
            }

            AlignCarModel(car, carBounds);

            carController.patrolSpeed = 7f;
            carController.chaseSpeed = 12f;
            carController.detectRadius = 16f;
            carController.waypoints = CreateRoute("CarRoute", car.transform.position, 5, 10f, null);
            carController.autoRideHeight = true;

            // ── Самолёт ──────────────────────────────────────────────
            var planeController = EnsureComponent<AircraftController>(plane);

            Vector3 planePosition = plane.transform.position;

            if (planePosition.y < 10f)
                plane.transform.position = new Vector3(planePosition.x, 16f, planePosition.z);

            planeController.cruiseAltitude = 16f;
            planeController.patrolSpeed = 16f;
            planeController.chaseSpeed = 24f;
            planeController.detectRadius = 45f;
            planeController.waypoints = CreateRoute("PlaneRoute", Vector3.zero, 4, 30f, 16f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[VehicleSetup] машина: {carController.waypoints.Length} точек, " +
                      $"самолёт: {planeController.waypoints.Length} точек, " +
                      $"сцена={SceneManager.GetActiveScene().name}");
        }

        // ─────────────────────────────────────────────────────────────
        //  Маршрут
        // ─────────────────────────────────────────────────────────────

        private static Transform[] CreateRoute(string name, Vector3 center, int count, float radius, float? altitude)
        {
            var existing = GameObject.Find(name);
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject(name);
            var points = new Transform[count];

            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;

                Vector3 position = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

                if (altitude.HasValue)
                {
                    position.y = altitude.Value;
                }
                else
                {
                    // Машина едет по земле — ставим точки на поверхность.
                    if (Physics.Raycast(position + Vector3.up * 30f, Vector3.down, out RaycastHit hit, 80f))
                        position.y = hit.point.y;
                    else
                        position.y = center.y;
                }

                var point = new GameObject($"WP_{i:00}");
                point.transform.SetParent(root.transform, false);
                point.transform.position = position;

                points[i] = point.transform;
            }

            return points;
        }

        // ─────────────────────────────────────────────────────────────
        //  Вспомогательное
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Если модель машины вытянута вдоль X (а не Z), поворачивает её,
        /// чтобы она ехала вдоль своего кузова, а не боком.
        /// </summary>
        private static void AlignCarModel(GameObject car, Bounds bounds)
        {
            if (car.transform.childCount == 0) return;

            bool longAlongX = bounds.size.x > bounds.size.z * 1.15f;
            if (!longAlongX) return;

            var model = car.transform.GetChild(0);

            // Поворачиваем один раз: если уже повёрнуто — пропускаем.
            if (Mathf.Abs(Mathf.DeltaAngle(model.localEulerAngles.y, 0f)) > 1f) return;

            model.localRotation = Quaternion.Euler(0f, -90f, 0f) * model.localRotation;
            Debug.Log("[VehicleSetup] модель машины вытянута вдоль X — повернул на −90°");
        }

        private static Bounds CombineBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            if (component != null) return component;

            component = target.AddComponent<T>();
            Debug.Log($"[VehicleSetup] на {target.name} добавлен {typeof(T).Name}");
            return component;
        }
    }
}
