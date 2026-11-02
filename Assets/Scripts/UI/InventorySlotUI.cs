using LabGame.Items;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LabGame.UI
{
    /// <summary>
    /// Ячейка сетки инвентаря. Реализует drag&amp;drop
    /// (перемещение предметов), двойной клик (экипировка) и наведение (тултип).
    /// </summary>
    public class InventorySlotUI : SlotViewBase,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private InventoryUI owner;
        private int index;

        public int Index => index;

        public void Build(InventoryUI owner, int index, float size)
        {
            this.owner = owner;
            this.index = index;

            BuildChrome(size);
            gameObject.name = $"Slot_{index}";
        }

        public void Refresh(ItemStack stack, bool isSelected)
        {
            ApplyItem(stack);
            SetSelected(isSelected);
        }

        // ── Drag & Drop ────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (owner == null) return;
            owner.BeginDragFromSlot(index, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (owner != null) owner.UpdateDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (owner != null) owner.EndDrag();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (owner != null) owner.DropOnSlot(index);
        }

        // ── Клики и наведение ──────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (owner == null || eventData.button != PointerEventData.InputButton.Left) return;
            owner.ClickSlot(index, eventData.clickCount);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (owner == null) return;
            SetHovered(true);
            owner.HoverSlot(index, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (owner == null) return;
            SetHovered(false);
            owner.HoverSlot(index, false);
        }
    }
}
