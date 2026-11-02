using LabGame.Items;
using UnityEngine;
using UnityEngine.UI;

namespace LabGame.UI
{
    /// <summary>
    /// Общая база для «ячейки» интерфейса: прямоугольник, иконка, количество.
    /// Без рамок, скруглений и подсветки — только оттенки серого.
    /// </summary>
    public abstract class SlotViewBase : MonoBehaviour
    {
        protected const float IconPadding = 14f;

        protected Image frame;
        protected Image iconBackground;
        protected Image iconImage;
        protected Text letterText;
        protected Text amountText;

        protected float slotSize;
        protected bool hovered;
        protected bool selected;
        protected ItemDefinition currentItem;

        protected static readonly Color IdleFill = new Color(0.78f, 0.78f, 0.78f, 1f);
        protected static readonly Color HoverFill = new Color(0.68f, 0.68f, 0.68f, 1f);
        protected static readonly Color SelectedFill = new Color(0.55f, 0.55f, 0.55f, 1f);

        protected void BuildChrome(float size)
        {
            slotSize = size;

            frame = gameObject.AddComponent<Image>();
            frame.sprite = UIFactory.RoundedSprite;
            frame.type = Image.Type.Simple;
            frame.color = IdleFill;
            frame.raycastTarget = true;

            float inner = size - IconPadding * 2f;

            iconBackground = UIFactory.CreateImage(transform, "IconBg", IconPadding, IconPadding, inner, inner,
                new Color(0.88f, 0.88f, 0.88f, 1f));

            iconImage = UIFactory.CreateImage(iconBackground.transform, "Icon", 2f, 2f, inner - 4f, inner - 4f,
                Color.white);

            letterText = UIFactory.CreateText(iconBackground.transform, "Letter", string.Empty,
                Mathf.RoundToInt(size * 0.36f), new Color(0.10f, 0.10f, 0.10f, 1f), TextAnchor.MiddleCenter,
                0f, 0f, inner, inner, FontStyle.Bold);

            amountText = UIFactory.CreateText(transform, "Amount", string.Empty, 18, UIFactory.TextMain,
                TextAnchor.LowerRight, 6f, 6f, size - 16f, size - 12f, FontStyle.Bold);
        }

        protected void ApplyFrame()
        {
            if (frame == null) return;

            if (selected) frame.color = SelectedFill;
            else if (hovered) frame.color = HoverFill;
            else frame.color = IdleFill;
        }

        protected void ApplyItem(ItemStack stack)
        {
            if (stack == null || stack.IsEmpty || stack.item == null)
            {
                currentItem = null;
                iconBackground.enabled = false;
                iconImage.enabled = false;
                letterText.text = string.Empty;
                amountText.text = string.Empty;
                return;
            }

            var item = stack.item;
            currentItem = item;

            iconBackground.enabled = true;
            iconImage.enabled = true;

            iconBackground.color = new Color(0.88f, 0.88f, 0.88f, 1f);

            if (item.icon != null)
            {
                iconImage.sprite = item.icon;
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true;
                iconImage.color = Color.white;
                letterText.text = string.Empty;
            }
            else
            {
                iconImage.sprite = UIFactory.RoundedSprite;
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = false;
                iconImage.color = new Color(1f, 1f, 1f, 1f);
                letterText.text = FirstLetter(item.SafeName);
            }

            amountText.text = stack.amount > 1 ? stack.amount.ToString() : string.Empty;
        }

        protected static string FirstLetter(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Substring(0, 1).ToUpperInvariant();
        }

        public void SetHovered(bool value)
        {
            hovered = value;
            ApplyFrame();
        }

        public void SetSelected(bool value)
        {
            selected = value;
            ApplyFrame();
        }
    }
}
