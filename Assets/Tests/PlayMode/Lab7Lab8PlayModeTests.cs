using System.Collections;
using System.Collections.Generic;
using LabGame;
using LabGame.AI;
using LabGame.Items;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LabGame.PlayModeTests
{
    /// <summary>
    /// Сквозные тесты ПЗ №7: подбор → экипировка → изменение характеристик →
    /// выбрасывание → повторный подбор.
    /// </summary>
    public class InventoryFlowTests
    {
        private readonly List<Object> created = new List<Object>();

        private GameObject playerObject;
        private PlayerInventory inventory;
        private PhysicsModule physics;

        [SetUp]
        public void SetUp()
        {
            LabServices.Reset();
            LabServices.AutoCreateInterface = false;
            GameEventBus.ClearAll();
            InteractableRegistry.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (LabHUD.Instance != null) Object.DestroyImmediate(LabHUD.Instance.gameObject);

            for (int i = 0; i < created.Count; i++)
                if (created[i] != null) Object.Destroy(created[i]);

            created.Clear();

            if (playerObject != null) Object.Destroy(playerObject);
            playerObject = null;

            LabServices.Reset();
        }

        private void CreatePlayer()
        {
            playerObject = new GameObject("Player");
            playerObject.tag = "Player";

            playerObject.AddComponent<Rigidbody>();
            playerObject.AddComponent<CapsuleCollider>();
            physics = playerObject.AddComponent<PhysicsModule>();

            inventory = playerObject.AddComponent<PlayerInventory>();
        }

        private ItemDefinition MakeItem(string id, ItemCategory category, EquipSlot slot,
            int maxStack = 1, params StatModifier[] modifiers)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.name = id;
            item.itemId = id;
            item.displayName = id;
            item.category = category;
            item.equipSlot = slot;
            item.maxStack = maxStack;
            item.modifiers = modifiers;

            created.Add(item);
            return item;
        }

        private ItemPickup CreatePickup(ItemDefinition item, int amount, Vector3? position = null)
        {
            var go = new GameObject($"Pickup_{item.SafeId}");
            go.transform.position = position ?? new Vector3(0f, 0.5f, 2f);

            var pickup = go.AddComponent<ItemPickup>();
            pickup.Setup(item, amount);

            created.Add(go);
            return pickup;
        }

        [UnityTest]
        public IEnumerator Pickup_AddsItemAndRaisesEvent()
        {
            CreatePlayer();
            yield return null;

            ItemDefinition eventItem = null;
            int eventAmount = 0;
            GameEventBus.ItemPickedUp += (item, amount) => { eventItem = item; eventAmount = amount; };

            var sword = MakeItem("sword", ItemCategory.Equipment, EquipSlot.Weapon, 1,
                new StatModifier(StatType.Attack, 12f));

            var pickup = CreatePickup(sword, 1);

            Assert.IsTrue(inventory.TryPickUp(pickup), "предмет должен подобраться");
            Assert.AreEqual(1, inventory.Model.Count(sword));
            Assert.AreSame(sword, eventItem);
            Assert.AreEqual(1, eventAmount);
        }

        [UnityTest]
        public IEnumerator Pickup_WhenInventoryFull_ReturnsFalse()
        {
            CreatePlayer();
            yield return null;

            var stone = MakeItem("stone", ItemCategory.Junk, EquipSlot.None, 1);
            for (int i = 0; i < inventory.Model.Capacity; i++) inventory.Model.Add(stone, 1);

            var pickup = CreatePickup(stone, 1);

            Assert.IsFalse(inventory.TryPickUp(pickup), "в полный инвентарь предмет не помещается");
            Assert.IsTrue(pickup != null && pickup.Item != null, "предмет должен остаться в мире");
        }

        [UnityTest]
        public IEnumerator Equip_ChangesCharacterStats()
        {
            CreatePlayer();
            yield return null;

            var sword = MakeItem("sword", ItemCategory.Equipment, EquipSlot.Weapon, 1,
                new StatModifier(StatType.Attack, 12f),
                new StatModifier(StatType.MoveSpeed, -0.5f));

            inventory.Model.Add(sword, 1);

            var before = inventory.Stats.TotalStats;

            Assert.IsTrue(inventory.EquipFromSlot(0), "меч должен экипироваться");

            var after = inventory.Stats.TotalStats;

            Assert.AreEqual(before.attack + 12f, after.attack, 0.01f, "атака должна вырасти");
            Assert.AreEqual(before.moveSpeed - 0.5f, after.moveSpeed, 0.01f, "скорость должна упасть");
            Assert.AreSame(sword, inventory.Model.GetEquipped(EquipSlot.Weapon).item);
        }

        [UnityTest]
        public IEnumerator Equip_AppliesMoveSpeedToPhysics()
        {
            CreatePlayer();
            yield return null;

            float baseSpeed = physics.maxSpeed;

            var boots = MakeItem("boots", ItemCategory.Equipment, EquipSlot.Feet, 1,
                new StatModifier(StatType.MoveSpeed, 1.2f));

            inventory.Model.Add(boots, 1);
            Assert.IsTrue(inventory.EquipFromSlot(0));

            Assert.Greater(physics.maxSpeed, baseSpeed,
                "скорость персонажа в физике должна вырасти после надевания сапог");
        }

        [UnityTest]
        public IEnumerator Unequip_ReturnsItemToInventory()
        {
            CreatePlayer();
            yield return null;

            var sword = MakeItem("sword", ItemCategory.Equipment, EquipSlot.Weapon, 1,
                new StatModifier(StatType.Attack, 12f));

            inventory.Model.Add(sword, 1);
            inventory.EquipFromSlot(0);

            Assert.IsTrue(inventory.Unequip(EquipSlot.Weapon));
            Assert.AreEqual(1, inventory.Model.Count(sword));
            Assert.IsTrue(inventory.Model.GetEquipped(EquipSlot.Weapon).IsEmpty);
        }

        [UnityTest]
        public IEnumerator Drop_SpawnsWorldItem_ThatCanBePickedUpAgain()
        {
            CreatePlayer();
            yield return null;

            var potion = MakeItem("potion", ItemCategory.Consumable, EquipSlot.None, 5);
            inventory.Model.Add(potion, 3);

            int droppedAmount = 0;
            GameEventBus.ItemDropped += (item, amount) => droppedAmount = amount;

            Assert.IsTrue(inventory.DropSlot(0), "предмет должен выброситься");

            Assert.AreEqual(0, inventory.Model.Count(potion));
            Assert.AreEqual(3, droppedAmount);

            var worldItems = Object.FindObjectsByType<ItemPickup>();
            Assert.AreEqual(1, worldItems.Length, "в мире должен появиться один предмет");
            Assert.AreSame(potion, worldItems[0].Item);

            Assert.IsTrue(inventory.TryPickUp(worldItems[0]), "выброшенный предмет можно подобрать снова");
            Assert.AreEqual(3, inventory.Model.Count(potion));
        }

        [UnityTest]
        public IEnumerator Interface_BuildsAndTogglesWithoutErrors()
        {
            // Единственный тест, который включает автосоздание UI.
            LabServices.AutoCreateInterface = true;

            CreatePlayer();
            yield return null;

            Assert.IsNotNull(LabHUD.Instance, "HUD должен создаться автоматически");
            Assert.IsNotNull(LabHUD.Instance.Inventory, "экран инвентаря должен создаться автоматически");

            var ui = LabHUD.Instance.Inventory;

            ui.Open();
            Assert.IsTrue(LabGame.UI.InventoryUI.IsOpen, "инвентарь должен открыться");
            Assert.IsTrue(LabGame.GameplayInput.Locked, "ввод должен блокироваться при открытом инвентаре");

            yield return null;

            // Ищем только после открытия: скрытые объекты FindObjectsByType не находит.
            var slotViews = Object.FindObjectsByType<LabGame.UI.InventorySlotUI>();
            var equipViews = Object.FindObjectsByType<LabGame.UI.EquipmentSlotUI>();

            Assert.AreEqual(inventory.Model.Capacity, slotViews.Length, "должны создаться все ячейки инвентаря");
            Assert.AreEqual(7, equipViews.Length, "должны создаться все слоты экипировки");

            // Наполняем инвентарь и надеваем предмет, чтобы обновились
            // панель характеристик и подписи.
            var sword = MakeItem("ui_sword", ItemCategory.Equipment, EquipSlot.Weapon, 1,
                new StatModifier(StatType.Attack, 5f));

            inventory.Model.Add(sword, 1);
            Assert.IsTrue(inventory.EquipFromSlot(0));
            yield return null;

            ui.Close();
            Assert.IsFalse(LabGame.UI.InventoryUI.IsOpen, "инвентарь должен закрыться");
            Assert.IsFalse(LabGame.GameplayInput.Locked, "ввод должен разблокироваться");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Move_SwapsItemsBetweenSlots()
        {
            CreatePlayer();
            yield return null;

            var a = MakeItem("a", ItemCategory.Junk, EquipSlot.None, 1);
            var b = MakeItem("b", ItemCategory.Junk, EquipSlot.None, 1);

            inventory.Model.Add(a, 1);
            inventory.Model.Add(b, 1);

            Assert.IsTrue(inventory.MoveItem(0, 1));

            Assert.AreSame(b, inventory.Model[0].item);
            Assert.AreSame(a, inventory.Model[1].item);
        }
    }

    /// <summary>
    /// Сквозные тесты ПЗ №8: NPC действительно получает события игры
    /// и меняет своё поведение.
    /// </summary>
    public class ChaoticNpcPlayTests
    {
        private GameObject npcObject;
        private GameObject playerObject;

        [SetUp]
        public void SetUp()
        {
            LabServices.Reset();
            LabServices.AutoCreateInterface = false;
            GameEventBus.ClearAll();

            // NavMesh в тестовой сцене отсутствует — сообщения Unity не считаем ошибкой теста.
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;

            if (npcObject != null) Object.Destroy(npcObject);
            if (playerObject != null) Object.Destroy(playerObject);

            GameEventBus.ClearAll();
            LabServices.Reset();
        }

        private ChaoticNPC CreateNpc(Vector3 playerPosition, Vector3 npcPosition)
        {
            playerObject = new GameObject("Player") { tag = "Player" };
            playerObject.transform.position = playerPosition;

            npcObject = new GameObject("ChaosNpc");
            npcObject.transform.position = npcPosition;
            npcObject.AddComponent<NavMeshAgent>();

            return npcObject.AddComponent<ChaoticNPC>();
        }

        [UnityTest]
        public IEnumerator Npc_HasBrainAndStartsWorking()
        {
            var npc = CreateNpc(Vector3.zero, new Vector3(3f, 0f, 0f));

            yield return null;

            Assert.IsNotNull(npc.Brain, "у NPC должен быть «мозг»");
            Assert.GreaterOrEqual(npc.Brain.Changes, 1, "NPC должен выбрать первое действие");
        }

        [UnityTest]
        public IEnumerator Npc_ReactsToItemPickedUp()
        {
            var npc = CreateNpc(Vector3.zero, new Vector3(3f, 0f, 0f));

            // Даём NPC два кадра: Awake + первый Update (оценка расстояния).
            yield return null;
            yield return null;

            // Событие обрабатывается синхронно, поэтому проверяем сразу же —
            // без лишнего кадра, в котором NPC мог бы отреагировать на близость.
            GameEventBus.RaiseItemPickedUp(null, 1);

            Assert.AreEqual(ChaosSignal.PlayerPickedUpItem, npc.Brain.LastSignal,
                "NPC должен заметить, что игрок подобрал предмет");
        }

        [UnityTest]
        public IEnumerator Npc_ReactsToPlayerDamage()
        {
            var npc = CreateNpc(Vector3.zero, new Vector3(3f, 0f, 0f));
            yield return null;
            yield return null;

            GameEventBus.RaisePlayerHealthChanged(100f, 100f);
            GameEventBus.RaisePlayerHealthChanged(40f, 100f);

            Assert.AreEqual(ChaosSignal.PlayerDamaged, npc.Brain.LastSignal,
                "NPC должен отреагировать на урон игрока");
        }

        [UnityTest]
        public IEnumerator Npc_BrainKeepsTicking()
        {
            var npc = CreateNpc(Vector3.zero, new Vector3(4f, 0f, 0f));
            yield return null;

            int before = npc.Brain.Ticks;
            yield return new WaitForSeconds(0.5f);

            Assert.Greater(npc.Brain.Ticks, before, "«мозг» NPC должен обновляться каждый кадр");
        }
    }
}
