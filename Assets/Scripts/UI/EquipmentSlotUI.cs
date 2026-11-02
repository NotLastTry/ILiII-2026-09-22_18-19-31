using LabGame.Items;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LabGame.UI
{
    /// <summary>
    /// Слот экипировки (шлем, броня, оружие и т.д.).
    /// Принимает предмет только соответствующего типа; двойной клик — снять.
    /// </summary>
    public class EquipmentSlotUI : SlotViewBase,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private InventoryUI owner;
        private EquipSlot slot;

        public EquipSlot Slot => slot;

        public void Build(InventoryUI owner, EquipSlot slot, float size)
        {
            this.owner = owner;
            this.slot = slot;

            BuildChrome(size);

            // Подпись типа слота — видна, когда слот пуст.
            letterText.fontSize = Mathf.RoundToInt(size * 0.20f);
            letterText.color = new Color(0.40f, 0.40f, 0.40f, 1f);

            gameObject.name = $"Equip_{slot}";
        }

        public void Refresh(ItemStack stack, bool isSelected)
        {
            ApplyItem(stack);

            if (stack == null || stack.IsEmpty)
            {
                iconBackground.enabled = true;
                iconImage.enabled = true;
                iconImage.sprite = UIFactory.RoundedSprite;
                iconImage.type = UnityEngine.UI.Image.Type.Simple;
                iconImage.color = new Color(0.88f, 0.88f, 0.88f, 1f);
                letterText.text = ShortSlotName(slot);
            }

            SetSelected(isSelected);
        }

        public static string ShortSlotName(EquipSlot slot)
        {
            switch (slot)
            {
                case EquipSlot.Head: return "Шлем";
                case EquipSlot.Chest: return "Броня";
                case EquipSlot.Hands: return "Перчатки";
                case EquipSlot.Feet: return "Обувь";
                case EquipSlot.Weapon: return "Оружие";
                case EquipSlot.Shield: return "Щит";
                case EquipSlot.Amulet: return "Амулет";
                default: return slot.ToString();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (owner == null) return;
            owner.BeginDragFromEquipped(slot, eventData);
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
            if (owner != null) owner.DropOnEquip(slot);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (owner == null || eventData.button != PointerEventData.InputButton.Left) return;
            owner.ClickEquipped(slot, eventData.clickCount);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (owner == null) return;
            SetHovered(true);
            owner.HoverEquipped(slot, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (owner == null) return;
            SetHovered(false);
            owner.HoverEquipped(slot, false);
        }
    }
}
