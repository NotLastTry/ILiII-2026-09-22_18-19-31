using LabGame.Items;
using UnityEngine;
using UnityEngine.UI;

namespace LabGame.UI
{
    /// <summary>
    /// Хелперы для сборки uGUI.
    ///
    /// Все методы «создать-или-найти»: если объект с таким именем уже есть
    /// у родителя, он переиспользуется, а его настройки не перезаписываются.
    /// Поэтому один и тот же код строит интерфейс и в редакторе (объекты
    /// сохраняются в сцене), и в рантайме (объекты просто находятся).
    ///
    /// Оформление намеренно без дизайна: обычные прямоугольники, чёрный текст
    /// на светлом фоне, без скруглений, рамок и цветовых акцентов.
    /// </summary>
    public static class UIFactory
    {
        private static Font cachedFont;
        private static Sprite flatSprite;

        // Вся палитра — оттенки серого, чтобы интерфейс выглядел «как есть».
        public static readonly Color PanelFill = new Color(0.92f, 0.92f, 0.92f, 0.96f);
        public static readonly Color CardFill = new Color(0.86f, 0.86f, 0.86f, 1f);
        public static readonly Color SlotFill = new Color(0.78f, 0.78f, 0.78f, 1f);
        public static readonly Color Border = new Color(0.78f, 0.78f, 0.78f, 1f);
        public static readonly Color Accent = new Color(0.10f, 0.10f, 0.10f, 1f);
        public static readonly Color TextMain = new Color(0.05f, 0.05f, 0.05f, 1f);
        public static readonly Color TextDim = new Color(0.35f, 0.35f, 0.35f, 1f);
        public static readonly Color Positive = TextMain;
        public static readonly Color Negative = TextMain;

        public static Font Font
        {
            get
            {
                if (cachedFont == null) cachedFont = LoadFont();
                return cachedFont;
            }
        }

        private static Font LoadFont()
        {
            // В Unity 2022+ встроенный шрифт называется LegacyRuntime.ttf.
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Segoe UI", 16);
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            return font;
        }

        /// <summary>Обычная белая текстура 1×1; цвет задаётся через Image.color.</summary>
        public static Sprite RoundedSprite
        {
            get
            {
                if (flatSprite == null)
                {
                    var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Point,
                        hideFlags = HideFlags.HideAndDontSave,
                        name = "LabUI_Flat"
                    };

                    texture.SetPixel(0, 0, Color.white);
                    texture.Apply();

                    flatSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
                }

                return flatSprite;
            }
        }

        /// <summary>RectTransform с привязкой к левому верхнему углу родителя.</summary>
        public static RectTransform CreateRect(Transform parent, string name, float x, float y, float width, float height)
            => CreateRect(parent, name, x, y, width, height, out _);

        /// <summary>
        /// Возвращает существующий дочерний объект с таким именем или создаёт новый.
        /// <paramref name="created"/> = true только если объект действительно создан.
        /// </summary>
        public static RectTransform CreateRect(Transform parent, string name, float x, float y, float width,
            float height, out bool created)
        {
            var existing = parent != null ? parent.Find(name) as RectTransform : null;

            if (existing != null)
            {
                created = false;
                return existing;
            }

            created = true;

            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
            return rect;
        }

        /// <summary>Заливка (обычный прямоугольник).</summary>
        public static Image CreateImage(Transform parent, string name, float x, float y, float width, float height,
            Color color, bool raycastTarget = false)
            => CreateImage(parent, name, x, y, width, height, color, raycastTarget, out _);

        public static Image CreateImage(Transform parent, string name, float x, float y, float width, float height,
            Color color, bool raycastTarget, out bool created)
        {
            var rect = CreateRect(parent, name, x, y, width, height, out created);

            var image = rect.GetComponent<Image>();

            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
                created = true;
            }

            if (created)
            {
                image.sprite = RoundedSprite;
                image.type = Image.Type.Simple;
                image.color = color;
                image.raycastTarget = raycastTarget;
            }

            return image;
        }

        /// <summary>
        /// Панель: заливка и вложенная копия того же цвета (рамки нет).
        /// Возвращает внутренний «Fill».
        /// </summary>
        public static Image CreatePanel(Transform parent, string name, float x, float y, float width, float height,
            Color fill, Color border, float borderWidth = 2f, bool raycastTarget = true)
            => CreatePanel(parent, name, x, y, width, height, fill, border, borderWidth, raycastTarget, out _);

        public static Image CreatePanel(Transform parent, string name, float x, float y, float width, float height,
            Color fill, Color border, float borderWidth, bool raycastTarget, out bool created)
        {
            var outer = CreateRect(parent, name, x, y, width, height, out created);

            var outerImage = outer.GetComponent<Image>();

            if (outerImage == null)
            {
                outerImage = outer.gameObject.AddComponent<Image>();
                created = true;
            }

            if (created)
            {
                outerImage.sprite = RoundedSprite;
                outerImage.type = Image.Type.Simple;
                outerImage.color = fill;
                outerImage.raycastTarget = raycastTarget;
            }

            return CreateImage(outer, "Fill", borderWidth, borderWidth,
                width - borderWidth * 2f, height - borderWidth * 2f, fill);
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color,
            TextAnchor anchor, float x, float y, float width, float height, FontStyle style = FontStyle.Normal)
            => CreateText(parent, name, content, fontSize, color, anchor, x, y, width, height, style, out _);

        public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color,
            TextAnchor anchor, float x, float y, float width, float height, FontStyle style, out bool created)
        {
            var rect = CreateRect(parent, name, x, y, width, height, out created);

            var text = rect.GetComponent<Text>();

            if (text == null)
            {
                text = rect.gameObject.AddComponent<Text>();
                created = true;
            }

            if (!created || text.font == null)
            {
                text.font = Font;
            }

            if (!created) return text;

            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = style;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;

            return text;
        }

        /// <summary>Кнопка-плашка (используется в подсказках).</summary>
        public static Text CreateHint(Transform parent, string name, string content, float x, float y, float width,
            float height)
        {
            var panel = CreatePanel(parent, name, x, y, width, height, CardFill, Border, 1.5f, false);
            return CreateText(panel.transform, "Text", content, 18, TextDim, TextAnchor.MiddleCenter, 0, 0, width - 4f, height - 4f);
        }

        /// <summary>Иконка предмета: спрайт из ассета либо однотонная плашка с буквой.</summary>
        public static void BuildIcon(Transform parent, ItemDefinition item, float x, float y, float size,
            out Image iconImage, out Text letter)
        {
            var background = CreateImage(parent, "IconBg", x, y, size, size, new Color(0.88f, 0.88f, 0.88f, 1f));

            iconImage = CreateImage(background.transform, "Icon", 3f, 3f, size - 6f, size - 6f,
                new Color(0.95f, 0.95f, 0.95f, 1f));

            if (item != null && item.icon != null)
            {
                iconImage.sprite = item.icon;
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true;
            }

            string letterText = item != null ? FirstLetter(item.SafeName) : string.Empty;
            letter = CreateText(background.transform, "Letter", letterText, Mathf.RoundToInt(size * 0.45f),
                new Color(0.10f, 0.10f, 0.10f, 1f), TextAnchor.MiddleCenter, 0f, 0f, size, size, FontStyle.Bold);

            if (item != null && item.icon != null) letter.text = string.Empty;
        }

        private static string FirstLetter(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Substring(0, 1).ToUpperInvariant();
        }
    }
}
