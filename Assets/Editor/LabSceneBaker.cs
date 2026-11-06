using System.Linq;
using LabGame;
using LabGame.AI;
using LabGame.Controls;
using LabGame.Dialogue;
using LabGame.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LabGame.EditorTools
{
    /// <summary>
    /// Размещает элементы лабораторных работ прямо в сцене, чтобы их было
    /// видно и можно было настраивать в инспекторе без запуска игры:
    ///
    ///   • компоненты игрока (характеристики, ввод, взаимодействие);
    ///   • объект LabEnvironment (время суток и погода);
    ///   • HUD с инвентарём и сенсорным управлением.
    ///
    /// Скрипт можно запускать повторно: уже существующие объекты
    /// переиспользуются, дубликаты не создаются.
    /// </summary>
    public static class LabSceneBaker
    {
        private const string DemoScene = "Assets/Scenes/Lab7_8_Demo.unity";

        private const string OkMarker = "LABSCENE_OK";
        private const string FailMarker = "LABSCENE_FAIL";

        [MenuItem("Lab 7-8/3. Разместить элементы в сцене", priority = 3)]
        public static void BakeScene()
        {
            try
            {
                Run();
                Debug.Log(OkMarker);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"{FailMarker}: {exception}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void Run()
        {
            if (!System.IO.File.Exists(DemoScene))
                throw new System.Exception($"нет сцены {DemoScene}");

            var scene = EditorSceneManager.OpenScene(DemoScene, OpenSceneMode.Single);

            var player = GameObject.FindWithTag("Player");
            if (player == null) throw new System.Exception("в сцене нет объекта с тегом Player");

            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null) throw new System.Exception("у игрока нет PlayerInventory");

            // Компоненты игрока — чтобы их параметры были видны в инспекторе.
            EnsureComponent<CharacterStats>(player);
            EnsureComponent<PlayerSpeedLink>(player);
            EnsureComponent<InputRouter>(player);
            EnsureComponent<PlayerInteractor>(player);

            // HUD со всеми панелями, инвентарём и сенсорным слоем.
            var hud = Object.FindAnyObjectByType<LabHUD>();

            if (hud == null)
            {
                var go = new GameObject("LabHUD", typeof(RectTransform));
                hud = go.AddComponent<LabHUD>();
            }

            hud.Bind(inventory);

            // Время суток и погода (создаются внутри HUD, но проверим, что объект на месте).
            if (Object.FindAnyObjectByType<LabEnvironment>() == null)
                LabEnvironment.Ensure();

            // Толпа NPC (ПЗ №6).
            var crowd = BakeCrowd(player.transform.position);

            // Система диалогов (ПЗ №9).
            BakeDialogue();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            AssetDatabase.SaveAssets();

            var environment = Object.FindAnyObjectByType<LabEnvironment>();

            Debug.Log($"[LabSceneBaker] сцена {SceneManager.GetActiveScene().name}: " +
                      $"HUD={(hud != null)}, инвентарь={(hud != null && hud.Inventory != null)}, " +
                      $"окружение={(environment != null)}, толпа={crowd}, игрок={player.name}");
        }

        /// <summary>
        /// Размещает вокруг площади несколько NPC с компонентом CrowdAgent.
        /// Повторный запуск пересоздаёт толпу, а не плодит дубликаты.
        /// </summary>
        private static int BakeCrowd(Vector3 playerPosition)        {
            var existing = GameObject.Find("Crowd");
            if (existing != null) Object.DestroyImmediate(existing);

            var template = GameObject.Find("NPC");
            if (template == null)
            {
                Debug.LogWarning("[LabSceneBaker] не найден объект NPC — толпа не создана");
                return 0;
            }

            const int count = 8;

            var root = new GameObject("Crowd");
            var manager = root.AddComponent<CrowdManager>();
            manager.pointOfInterest = null;
            manager.panicRadius = 14f;
            manager.panicDuration = 5f;
            manager.interestRadius = 9f;

            // Толпа собирается на площади перед NPC, чуть в стороне от игрока.
            Vector3 center = new Vector3(0f, 0f, 2f);

            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                Vector3 position = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 5f;

                if (NavMesh.SamplePosition(position, out NavMeshHit hit, 6f, NavMesh.AllAreas))
                    position = hit.position;

                var member = Object.Instantiate(template, position,
                    Quaternion.Euler(0f, angle * Mathf.Rad2Deg + 180f, 0f), root.transform);

                member.name = $"CrowdMember_{i:00}";

                // Убираем поведение одиночного противника — здесь работает толпа.
                // Собеседника убираем первым: он требует наличия NPCController.
                foreach (var behaviour in member.GetComponents<MonoBehaviour>().ToList())
                    if (behaviour is DialogueAgent)
                        Object.DestroyImmediate(behaviour);

                foreach (var behaviour in member.GetComponents<MonoBehaviour>().ToList())
                {
                    if (behaviour is NPCController || behaviour is RovingNPC || behaviour is ChaoticNPC)
                        Object.DestroyImmediate(behaviour);
                }

                var agent = member.GetComponent<NavMeshAgent>();

                if (agent != null)
                {
                    agent.speed = 2.2f;
                    agent.angularSpeed = 240f;
                    agent.stoppingDistance = 0.4f;
                    agent.radius = 0.42f;
                    agent.avoidancePriority = 30 + i;
                }

                member.AddComponent<CrowdAgent>();
            }

            Debug.Log($"[LabSceneBaker] создана толпа из {count} NPC вокруг {center}");
            return count;
        }

        /// <summary>
        /// Добавляет в сцену систему диалогов (ПЗ №9):
        /// объект-раннер и компонент-собеседник на NPC.
        /// </summary>
        private static void BakeDialogue()
        {
            if (Object.FindAnyObjectByType<DialogueRunner>() == null)
                new GameObject("DialogueRunner").AddComponent<DialogueRunner>();

            var npc = GameObject.Find("NPC");

            if (npc == null)
            {
                Debug.LogWarning("[LabSceneBaker] не найден объект NPC — диалог не привязан");
                return;
            }

            var agent = EnsureComponent<DialogueAgent>(npc);

            Debug.Log($"[LabSceneBaker] диалог «{agent.DialogueId}» привязан к {npc.name}");
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            if (component != null) return component;

            component = target.AddComponent<T>();
            Debug.Log($"[LabSceneBaker] на {target.name} добавлен {typeof(T).Name}");
            return component;
        }
    }
}
