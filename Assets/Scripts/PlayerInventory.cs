using System;
using LabGame;
using LabGame.Items;
using UnityEngine;

/// <summary>
/// Инвентарь игрока (ПЗ №7).
/// Компонент-«владелец» модели: хранит <see cref="InventoryModel"/>,
/// связывает её с миром (подбор/выбрасывание) и с HUD.
/// Логика самих правил живёт в InventoryModel и не зависит от сцены.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("Инвентарь")]
    [SerializeField, Min(1)] private int capacity = InventoryModel.DefaultCapacity;

    [Tooltip("Автоматически создать HUD/инвентарь при старте, если его нет в сцене.")]
    [SerializeField] private bool autoCreateInterface = true;

    [Header("ПЗ №6 (совместимость со старым квестом)")]
    public QuestItem questItem = new QuestItem();

    /// <summary>Правила инвентаря.</summary>
    public InventoryModel Model { get; private set; }

    /// <summary>Характеристики персонажа (создаются автоматически).</summary>
    public CharacterStats Stats { get; private set; }

    /// <summary>Предмет выброшен в мир (для ИИ и статистики).</summary>
    public event Action<ItemStack> ItemDroppedInWorld;

    private void Awake()
    {
        Model = new InventoryModel(capacity);

        Stats = GetComponent<CharacterStats>();
        if (Stats == null) Stats = gameObject.AddComponent<CharacterStats>();

        if (GetComponent<PhysicsModule>() != null && GetComponent<PlayerSpeedLink>() == null)
            gameObject.AddComponent<PlayerSpeedLink>();

        LabServices.RegisterPlayer(this);

        if (autoCreateInterface && LabServices.AutoCreateInterface) LabHUD.Ensure(this);
    }

    private void OnDestroy()
    {
        LabServices.UnregisterPlayer(this);
    }

    // ─────────────────────────────────────────────────────────────
    //  ПОДБОР
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Подобрать предмет из мира. Возвращает false, если предмет
    /// не помещается целиком — тогда предмет остаётся лежать на земле.
    /// </summary>
    public bool TryPickUp(ItemPickup pickup)
    {
        if (pickup == null || pickup.Item == null) return false;

        if (!Model.CanAdd(pickup.Item, pickup.Amount))
        {
            LabHUD.Toast($"Инвентарь полон — «{pickup.Item.SafeName}» не влезает");
            return false;
        }

        Model.Add(pickup.Item, pickup.Amount);

        GameEventBus.RaiseItemPickedUp(pickup.Item, pickup.Amount);
        LabHUD.Toast($"Подобрано: {pickup.Item.SafeName} x{pickup.Amount}");

        return true;
    }

    // ─────────────────────────────────────────────────────────────
    //  ВЫБРАСЫВАНИЕ
    // ─────────────────────────────────────────────────────────────

    /// <summary>Выбросить предмет из слота инвентаря в мир.</summary>
    public bool DropSlot(int slotIndex)
    {
        var stack = Model.Take(slotIndex);
        if (stack == null) return false;

        ThrowIntoWorld(stack);
        return true;
    }

    /// <summary>Снять предмет из слота экипировки и сразу выбросить его.</summary>
    public bool DropEquipped(EquipSlot slot)
    {
        var stack = Model.TakeEquipped(slot);
        if (stack == null) return false;

        ThrowIntoWorld(stack);
        return true;
    }

    /// <summary>Снять надетый предмет в инвентарь.</summary>
    public bool Unequip(EquipSlot slot) => Model.Unequip(slot);

    private void ThrowIntoWorld(ItemStack stack)
    {
        Vector3 position = ResolveDropPosition();

        WorldItemSpawner.Spawn(stack.item, stack.amount, position);

        GameEventBus.RaiseItemDropped(stack.item, stack.amount);
        ItemDroppedInWorld?.Invoke(stack);
        LabHUD.Toast($"Выброшено: {stack.item.SafeName} x{stack.amount}");

        Debug.Log($"[PlayerInventory] выброшен {stack.item.SafeName} x{stack.amount} в {position}");
    }

    /// <summary>Точка перед игроком, «прижатая» к земле.</summary>
    private Vector3 ResolveDropPosition()
    {
        Vector3 origin = transform.position + Vector3.up * 1.2f + transform.forward * 1.4f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 8f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point + Vector3.up * 0.25f;

        return origin;
    }

    // ─────────────────────────────────────────────────────────────
    //  УДОБНЫЕ ОБЁРТКИ (используются UI)
    // ─────────────────────────────────────────────────────────────

    public bool EquipFromSlot(int slotIndex) => Model.Equip(slotIndex);

    public bool EquipToSlot(int slotIndex, EquipSlot target) => Model.EquipTo(slotIndex, target);

    public bool MoveItem(int from, int to) => Model.Move(from, to);

    public bool DropSelected(int slotIndex)
    {
        if (!Model.IsValidIndex(slotIndex) || Model[slotIndex].IsEmpty) return false;
        return DropSlot(slotIndex);
    }
}
