using System;
using System.IO;
using System.Linq;
using LabGame.AI;
using LabGame.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LabGame.EditorTools
{
    /// <summary>
    /// Сборщик демо-сцены для ПЗ №7 и ПЗ №8.
    /// Создаёт ассеты предметов, копию рабочей сцены, расставляет лут
    /// и добавляет хаотичного NPC. Запускается из меню или в batch-режиме:
    ///
    ///   Unity.exe -batchmode -quit -projectPath &lt;проект&gt;
    ///             -executeMethod LabGame.EditorTools.LabSetup.BuildDemoScene -logFile -
    /// </summary>
    public static class LabSetup
    {
        private const string SourceScene = "Assets/Scenes/SampleScene.unity";
        private const string DemoScene = "Assets/Scenes/Lab7_8_Demo.unity";
        private const string ItemFolder = "Assets/LabItems";

        private const string OkMarker = "LABSETUP_OK";
        private const string FailMarker = "LABSETUP_FAIL";

        [MenuItem("Lab 7-8/1. Создать демо-сцену", priority = 1)]
        public static void BuildDemoScene()
        {
            try
            {
                Build();
                Debug.Log(OkMarker);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{FailMarker}: {exception}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        [MenuItem("Lab 7-8/2. Проверить демо-сцену", priority = 2)]
        public static void ValidateDemoScene()
        {
            try
            {
                if (!File.Exists(DemoScene))
                    throw new Exception($"нет сцены {DemoScene}");

                EditorSceneManager.OpenScene(DemoScene, OpenSceneMode.Single);

                var player = GameObject.FindWithTag("Player");
                var pickups = Object.FindObjectsByType<ItemPickup>();
                var chaosNpcs = Object.FindObjectsByType<ChaoticNPC>();
                var stats = player != null ? player.GetComponent<CharacterStats>() : null;
                var inventory = player != null ? player.GetComponent<PlayerInventory>() : null;

                Debug.Log($"[Validate] player={player != null}, inventory={inventory != null}, " +
                          $"stats={stats != null}, pickups={pickups.Length}, chaosNpc={chaosNpcs.Length}, " +
                          $"scene={SceneManager.GetActiveScene().name}");

                if (player == null) throw new Exception("в сцене нет игрока");
                if (inventory == null) throw new Exception("у игрока нет PlayerInventory");
                if (stats == null) throw new Exception("у игрока нет CharacterStats");
                if (pickups.Length == 0) throw new Exception("в сцене нет предметов для подбора");
                if (chaosNpcs.Length == 0) throw new Exception("в сцене нет хаотичного NPC");

                Debug.Log(OkMarker);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{FailMarker}: {exception}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  Сборка
        // ─────────────────────────────────────────────────────────────

        private static void Build()
        {
            EnsureFolder(ItemFolder);

            // ── 1. Ассеты предметов ──────────────────────────────────
            CreateOrUpdateItems();
            GetOrCreateProfile();

            // Важно: сохранить ассеты на диск ДО открытия сцены.
            // Иначе загрузка сцены выгружает ещё не сохранённые объекты,
            // и ссылки на них становятся «пустыми».
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ── 2. Сцена ─────────────────────────────────────────────
            if (!File.Exists(DemoScene))
            {
                if (!AssetDatabase.CopyAsset(SourceScene, DemoScene))
                    throw new Exception($"не удалось скопировать {SourceScene} в {DemoScene}");
            }

            var scene = EditorSceneManager.OpenScene(DemoScene, OpenSceneMode.Single);

            // Теперь читаем ассеты заново — гарантированно валидные ссылки.
            var sword = LoadItem("Sword_Steel");
            var shield = LoadItem("Shield_Oak");
            var helmet = LoadItem("Helmet_Iron");
            var chest = LoadItem("Chest_Leather");
            var boots = LoadItem("Boots_Swift");
            var amulet = LoadItem("Amulet_Might");
            var potion = LoadItem("Potion_Heal");
            var berry = LoadItem("Berry");
            var profile = LoadProfile();

            var player = GameObject.FindWithTag("Player");
            if (player == null) throw new Exception("в сцене нет объекта с тегом Player");

            EnsureComponent<CharacterStats>(player);
            EnsureComponent<PlayerSpeedLink>(player);

            // ── 3. Лут на земле ──────────────────────────────────────
            var oldLoot = GameObject.Find("LabLoot");
            if (oldLoot != null) Object.DestroyImmediate(oldLoot);

            var lootRoot = new GameObject("LabLoot").transform;

            Vector3 origin = player.transform.position;
            SpawnLoot(lootRoot, sword, 1, origin + new Vector3(2.5f, 0f, 1.5f));
            SpawnLoot(lootRoot, shield, 1, origin + new Vector3(-2.2f, 0f, 2.0f));
            SpawnLoot(lootRoot, helmet, 1, origin + new Vector3(0.5f, 0f, 3.5f));
            SpawnLoot(lootRoot, chest, 1, origin + new Vector3(-3.0f, 0f, 1.5f));
            SpawnLoot(lootRoot, boots, 1, origin + new Vector3(3.5f, 0f, -2.0f));
            SpawnLoot(lootRoot, amulet, 1, origin + new Vector3(0f, 0f, 5.0f));
            SpawnLoot(lootRoot, potion, 3, origin + new Vector3(-1.2f, 0f, 4.5f));
            SpawnLoot(lootRoot, potion, 2, origin + new Vector3(4.2f, 0f, 0.5f));
            SpawnLoot(lootRoot, berry, 5, origin + new Vector3(-4.0f, 0f, 0.8f));

            // ── 4. Хаотичный NPC ─────────────────────────────────────
            var oldChaos = GameObject.Find("ChaosNPC");
            if (oldChaos != null) Object.DestroyImmediate(oldChaos);

            var npc = GameObject.Find("NPC");
            if (npc == null) throw new Exception("в сцене нет объекта NPC");

            var chaos = Object.Instantiate(npc, npc.transform.position + new Vector3(7f, 0f, 4f), npc.transform.rotation);
            chaos.name = "ChaosNPC";

            foreach (var behaviour in chaos.GetComponents<MonoBehaviour>().ToList())
            {
                if (behaviour is NPCController || behaviour is RovingNPC)
                    Object.DestroyImmediate(behaviour);
            }

            EnsureComponent<ChaosAgent>(chaos);
            EnsureComponent<NpcPerception>(chaos);
            var chaotic = EnsureComponent<ChaoticNPC>(chaos);

            chaotic.displayName = "Хаотичный NPC";
            chaotic.profile = profile;
            chaotic.logActions = true;
            chaotic.showHud = true;

            var chaosAgent = chaos.GetComponent<ChaosAgent>();
            chaosAgent.player = player.transform;
            chaosAgent.fakeGiftItems = new[] { potion, berry };
            chaosAgent.walkSpeed = 3.6f;
            chaosAgent.runSpeed = 5.4f;

            var chaosPerception = chaos.GetComponent<NpcPerception>();
            chaosPerception.player = player.transform;
            chaosPerception.sightRange = 26f;

            if (NavMesh.SamplePosition(chaos.transform.position, out var navHit, 12f, NavMesh.AllAreas))
                chaos.transform.position = navHit.position;
            else
                Debug.LogWarning("[LabSetup] не удалось поставить ChaosNPC на NavMesh — проверьте запечённый NavMesh");

            // ── 5. Сохранение ────────────────────────────────────────
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            RegisterInBuildSettings(DemoScene);

            AssetDatabase.SaveAssets();

            Debug.Log($"[LabSetup] сцена собрана: {DemoScene}, лут: {lootRoot.childCount}, " +
                      $"хаотичный NPC: {chaos.name}, профиль: {(profile != null ? profile.name : "нет")}");
        }

        private static void SpawnLoot(Transform root, ItemDefinition item, int amount, Vector3 position)
        {
            Vector3 point = position;

            if (Physics.Raycast(position + Vector3.up * 6f, Vector3.down, out RaycastHit hit, 20f))
                point = hit.point + Vector3.up * 0.35f;

            var pickup = WorldItemSpawner.Spawn(item, amount, point);
            if (pickup == null)
            {
                Debug.LogWarning($"[LabSetup] не удалось создать предмет {item?.SafeId}");
                return;
            }

            pickup.transform.SetParent(root, true);
            pickup.name = $"Loot_{item.SafeId}_{root.childCount}";
        }

        // ─────────────────────────────────────────────────────────────
        //  Ассеты
        // ─────────────────────────────────────────────────────────────

        private static void CreateOrUpdateItems()
        {
            GetOrCreateItem(
                "Sword_Steel", "weapon_sword", "Стальной меч",
                "Тяжёлый меч стражи. Сильно повышает атаку, но немного замедляет.",
                ItemCategory.Equipment, EquipSlot.Weapon, 1, 3.2f, 40,
                new Color(0.76f, 0.79f, 0.86f),
                new[]
                {
                    new StatModifier(StatType.Attack, 12f),
                    new StatModifier(StatType.MoveSpeed, -0.5f),
                    new StatModifier(StatType.CritChance, 0f, 0.05f)
                });

            GetOrCreateItem(
                "Shield_Oak", "shield_oak", "Дубовый щит",
                "Простой щит. Прибавляет защиту и вес.",
                ItemCategory.Equipment, EquipSlot.Shield, 1, 4f, 25,
                new Color(0.58f, 0.42f, 0.26f),
                new[]
                {
                    new StatModifier(StatType.Defense, 6f),
                    new StatModifier(StatType.MoveSpeed, -0.3f)
                });

            GetOrCreateItem(
                "Helmet_Iron", "helmet_iron", "Железный шлем",
                "Надёжно защищает голову и придаёт уверенности.",
                ItemCategory.Equipment, EquipSlot.Head, 1, 2f, 30,
                new Color(0.56f, 0.59f, 0.63f),
                new[]
                {
                    new StatModifier(StatType.Defense, 4f),
                    new StatModifier(StatType.MaxHealth, 15f)
                });

            GetOrCreateItem(
                "Chest_Leather", "chest_leather", "Кожаная куртка",
                "Лёгкая броня, почти не стесняет движений.",
                ItemCategory.Equipment, EquipSlot.Chest, 1, 2.5f, 20,
                new Color(0.46f, 0.33f, 0.22f),
                new[]
                {
                    new StatModifier(StatType.Defense, 3f),
                    new StatModifier(StatType.MoveSpeed, 0f, 0.07f)
                });

            GetOrCreateItem(
                "Boots_Swift", "boots_swift", "Сапоги скорости",
                "В них можно обогнать даже собственные мысли.",
                ItemCategory.Equipment, EquipSlot.Feet, 1, 1.2f, 35,
                new Color(0.35f, 0.56f, 0.82f),
                new[]
                {
                    new StatModifier(StatType.MoveSpeed, 1.2f)
                });

            GetOrCreateItem(
                "Amulet_Might", "amulet_might", "Амулет силы",
                "Слабый, но приятный прирост к атаке и криту.",
                ItemCategory.Equipment, EquipSlot.Amulet, 1, 0.2f, 60,
                new Color(0.86f, 0.66f, 0.26f),
                new[]
                {
                    new StatModifier(StatType.Attack, 5f),
                    new StatModifier(StatType.CritChance, 0f, 0.05f)
                });

            GetOrCreateItem(
                "Potion_Heal", "potion_heal", "Зелье лечения",
                "Красная жидкость. Пахнет ягодами.",
                ItemCategory.Consumable, EquipSlot.None, 5, 0.3f, 10,
                new Color(0.86f, 0.26f, 0.36f),
                Array.Empty<StatModifier>());

            GetOrCreateItem(
                "Berry", "berry", "Ягода",
                "Квестовый предмет из ПЗ №6.",
                ItemCategory.Quest, EquipSlot.None, 20, 0.1f, 2,
                new Color(0.82f, 0.16f, 0.32f),
                Array.Empty<StatModifier>());
        }

        private static ItemDefinition GetOrCreateItem(string fileName, string id, string displayName, string description,
            ItemCategory category, EquipSlot slot, int maxStack, float weight, int gold, Color tint,
            StatModifier[] modifiers)
        {
            string path = $"{ItemFolder}/{fileName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.itemId = id;
            asset.displayName = displayName;
            asset.description = description;
            asset.category = category;
            asset.equipSlot = slot;
            asset.maxStack = Mathf.Max(1, maxStack);
            asset.weight = weight;
            asset.goldValue = gold;
            asset.tint = tint;
            asset.modifiers = modifiers ?? Array.Empty<StatModifier>();

            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ChaosProfile GetOrCreateProfile()
        {
            string path = $"{ItemFolder}/ChaosProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<ChaosProfile>(path);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<ChaosProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            profile.chaos = 0.8f;
            profile.reactionChance = 0.85f;
            profile.antiRepeatStrength = 0.75f;
            profile.historyDepth = 4;
            profile.durationScale = 1f;

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static ItemDefinition LoadItem(string fileName)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{ItemFolder}/{fileName}.asset");
            if (asset == null) throw new Exception($"не найден ассет предмета {fileName}");
            return asset;
        }

        private static ChaosProfile LoadProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<ChaosProfile>($"{ItemFolder}/ChaosProfile.asset");
            if (profile == null) throw new Exception("не найден ассет ChaosProfile");
            return profile;
        }

        // ─────────────────────────────────────────────────────────────
        //  Вспомогательное
        // ─────────────────────────────────────────────────────────────

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            if (component != null) return component;

            component = target.AddComponent<T>();
            Debug.Log($"[LabSetup] на {target.name} добавлен {typeof(T).Name}");
            return component;
        }

        private static void RegisterInBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == scenePath)) return;

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
