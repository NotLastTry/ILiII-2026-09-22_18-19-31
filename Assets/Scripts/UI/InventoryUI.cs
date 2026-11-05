using LabGame.Controls;
using LabGame.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LabGame.UI
{
    /// <summary>
    /// Экран инвентаря (ПЗ №7).
    ///
    /// Управление:
    ///   • TAB            — открыть/закрыть инвентарь;
    ///   • ЛКМ + перетаскивание — перемещение предметов между слотами;
    ///   • перетаскивание на слот экипировки — надеть предмет;
    ///   • двойной клик     — надеть / снять предмет;
    ///   • G или кнопка     — выбросить выделенный предмет в мир;
    ///   • ESC             — закрыть.
    ///
    /// Интерфейс строится кодом, поэтому работает в любой сцене без
    /// предварительной ручной сборки иерархии Canvas.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        // ── Размеры компоновки ────────────────────────────────────
        private const float PanelWidth = 1160f;
        private const float PanelHeight = 700f;
        private const int Columns = 6;
        private const int Rows = 4;
        private const float SlotSize = 96f;
        private const float SlotGap = 12f;
        private const float GridX = 36f;
        private const float GridY = 96f;
        private const float RightX = 724f;
        private const float EquipSlotSize = 74f;
        private const float EquipGap = 12f;

        public static InventoryUI Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.open;

        private PlayerInventory inventory;
        private RectTransform panel;
        private RectTransform panelRoot;

        private InventorySlotUI[] slotViews;
        private EquipmentSlotUI[] equipViews;
        private Text[] statValues;
        private Text[] statDeltas;
        private Text weightText;
        private Text goldText;

        private RectTransform tooltipRoot;
        private Text tooltipTitle;
        private Text tooltipBody;

        private Image dragGhost;

        private bool open;
        private int dragFrom = -1;
        private EquipSlot dragEquipFrom = EquipSlot.None;
        private bool dragHandled;

        private int selectedSlot = -1;
        private EquipSlot selectedEquip = EquipSlot.None;

        // ─────────────────────────────────────────────────────────────
        //  Создание
        // ─────────────────────────────────────────────────────────────

        public static InventoryUI Create(LabHUD hud, PlayerInventory player)
        {
            // В сцене экран инвентаря уже может быть создан заранее — тогда используем его.
            var existing = hud.GetComponentInChildren<InventoryUI>(true);

            if (existing != null)
            {
                existing.Build(player);
                return existing;
            }

            var go = new GameObject("InventoryUI", typeof(RectTransform));
            go.transform.SetParent(hud.transform, false);

            var ui = go.AddComponent<InventoryUI>();
            ui.ApplyStretch();
            ui.Build(player);
            return ui;
        }

        private void Awake() => ApplyStretch();

        private void ApplyStretch()
        {
            var rect = (RectTransform)transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void Build(PlayerInventory player)
        {
            Instance = this;
            inventory = player;

            BuildPanel();
            BuildTooltip();
            BuildDragGhost();

            if (inventory != null && inventory.Model != null)
            {
                inventory.Model.Changed += RefreshAll;
                if (inventory.Stats != null) inventory.Stats.Changed += OnStatsChanged;
            }

            RefreshAll();

            // В редакторе панель оставляем открытой, чтобы её было видно и можно было настроить.
            if (Application.isPlaying) SetOpen(false);
        }

        private void OnDestroy()
        {
            if (inventory != null)
            {
                inventory.Model.Changed -= RefreshAll;
                if (inventory.Stats != null) inventory.Stats.Changed -= OnStatsChanged;
            }

            if (Instance == this) Instance = null;
            if (open)
            {
                GameplayInput.Locked = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void BuildPanel()
        {
            panelRoot = UIFactory.CreateRect(transform, "InventoryPanel", 0f, 0f, PanelWidth, PanelHeight,
                out bool rootCreated);

            if (rootCreated)
            {
                panelRoot.anchorMin = panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
                panelRoot.pivot = new Vector2(0.5f, 0.5f);
                panelRoot.anchoredPosition = Vector2.zero;
            }

            panel = UIFactory.CreatePanel(panelRoot, "Background", 0f, 0f, PanelWidth, PanelHeight,
                UIFactory.PanelFill, UIFactory.PanelFill, 2f, true).transform.parent as RectTransform;

            UIFactory.CreateText(panel, "Title", "ИНВЕНТАРЬ", 32, UIFactory.TextMain, TextAnchor.MiddleLeft,
                36f, 20f, 420f, 44f, FontStyle.Bold);

            UIFactory.CreateText(panel, "Controls",
                "TAB — закрыть    G — выбросить    двойной клик — надеть/снять",
                16, UIFactory.TextDim, TextAnchor.MiddleRight, 520f, 24f, 604f, 36f);

            BuildGrid();
            BuildEquipment();
            BuildStats();

            UIFactory.CreateText(panel, "Footer",
                "Перетаскивайте предметы мышью • Двойной клик надевает и снимает экипировку",
                16, UIFactory.TextDim, TextAnchor.MiddleLeft, 36f, 640f, 720f, 30f);

            BuildPanelButton("CloseButton", "Закрыть", 780f, 160f, Close);
            BuildPanelButton("DropButton", "Выбросить", 960f, 164f, DropSelected);
        }

        /// <summary>
        /// Кнопки внизу панели: нужны на сенсорном экране, где нет клавиш Tab и G.
        /// </summary>
        private void BuildPanelButton(string name, string label, float x, float width,
            UnityEngine.Events.UnityAction action)
        {
            var rect = UIFactory.CreateRect(panel, name, x, 634f, width, 34f);

            var image = rect.GetComponent<Image>();

            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
                image.sprite = UIFactory.RoundedSprite;
                image.color = new Color(0.78f, 0.78f, 0.78f, 1f);
                image.raycastTarget = true;
            }

            UIFactory.CreateText(rect, "Label", label, 16, UIFactory.TextMain,
                TextAnchor.MiddleCenter, 0f, 0f, width, 34f);

            var button = rect.GetComponent<Button>();

            if (button == null)
            {
                button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void BuildGrid()
        {
            int count = inventory != null && inventory.Model != null
                ? inventory.Model.Capacity
                : InventoryModel.DefaultCapacity;
            slotViews = new InventorySlotUI[count];

            for (int i = 0; i < count; i++)
            {
                int column = i % Columns;
                int row = i / Columns;

                float x = GridX + column * (SlotSize + SlotGap);
                float y = GridY + row * (SlotSize + SlotGap);

                var rect = UIFactory.CreateRect(panel, $"Slot_{i}", x, y, SlotSize, SlotSize);

                var view = rect.GetComponent<InventorySlotUI>();
                if (view == null) view = rect.gameObject.AddComponent<InventorySlotUI>();

                view.Build(this, i, SlotSize);
                slotViews[i] = view;
            }

            // Подпись «сколько слотов занято» под сеткой.
            UIFactory.CreateText(panel, "CapacityText", string.Empty, 16, UIFactory.TextDim,
                TextAnchor.MiddleLeft, GridX, GridY + Rows * (SlotSize + SlotGap) + 4f, 400f, 24f)
                .name = "CapacityText";
        }

        private void BuildEquipment()
        {
            UIFactory.CreateText(panel, "EquipTitle", "ЭКИПИРОВКА", 20, UIFactory.Accent, TextAnchor.MiddleLeft,
                RightX, 96f, 300f, 28f, FontStyle.Bold);

            var slots = new[]
            {
                EquipSlot.Head, EquipSlot.Chest, EquipSlot.Hands, EquipSlot.Feet,
                EquipSlot.Weapon, EquipSlot.Shield, EquipSlot.Amulet
            };

            equipViews = new EquipmentSlotUI[slots.Length];

            for (int i = 0; i < slots.Length; i++)
            {
                int column = i % 4;
                int row = i / 4;

                float x = RightX + column * (EquipSlotSize + EquipGap);
                float y = 134f + row * (EquipSlotSize + 44f);

                var rect = UIFactory.CreateRect(panel, $"Equip_{slots[i]}", x, y, EquipSlotSize, EquipSlotSize);

                var view = rect.GetComponent<EquipmentSlotUI>();
                if (view == null) view = rect.gameObject.AddComponent<EquipmentSlotUI>();

                view.Build(this, slots[i], EquipSlotSize);
                equipViews[i] = view;

                UIFactory.CreateText(panel, $"EquipLabel_{slots[i]}", EquipmentSlotUI.ShortSlotName(slots[i]),
                    14, UIFactory.TextDim, TextAnchor.MiddleCenter, x, y + EquipSlotSize + 2f, EquipSlotSize, 18f);
            }
        }

        private void BuildStats()
        {
            const float statsTop = 400f;

            UIFactory.CreateText(panel, "StatsTitle", "ХАРАКТЕРИСТИКИ", 20, UIFactory.Accent,
                TextAnchor.MiddleLeft, RightX, 356f, 300f, 28f, FontStyle.Bold);

            statValues = new Text[StatBlock.All.Length];
            statDeltas = new Text[StatBlock.All.Length];

            for (int i = 0; i < StatBlock.All.Length; i++)
            {
                var type = StatBlock.All[i];
                float y = statsTop + i * 28f;

                UIFactory.CreateText(panel, $"StatLabel_{type}", StatBlock.ShortName(type), 18, UIFactory.TextDim,
                    TextAnchor.MiddleLeft, RightX, y, 200f, 26f);

                statValues[i] = UIFactory.CreateText(panel, $"StatValue_{type}", "0", 18, UIFactory.TextMain,
                    TextAnchor.MiddleRight, RightX + 210f, y, 90f, 26f, FontStyle.Bold);

                statDeltas[i] = UIFactory.CreateText(panel, $"StatDelta_{type}", string.Empty, 17, UIFactory.Positive,
                    TextAnchor.MiddleLeft, RightX + 308f, y, 100f, 26f);
            }

            weightText = UIFactory.CreateText(panel, "Weight", string.Empty, 16, UIFactory.TextDim,
                TextAnchor.MiddleLeft, RightX, statsTop + StatBlock.All.Length * 28f + 6f, 300f, 24f);

            goldText = UIFactory.CreateText(panel, "Gold", string.Empty, 16, UIFactory.TextDim,
                TextAnchor.MiddleLeft, RightX, statsTop + StatBlock.All.Length * 28f + 30f, 300f, 24f);
        }

        private void BuildTooltip()
        {
            tooltipRoot = UIFactory.CreateRect(transform, "Tooltip", 0f, 0f, 390f, 160f, out bool tipCreated);

            if (tipCreated)
            {
                tooltipRoot.anchorMin = tooltipRoot.anchorMax = new Vector2(0.5f, 0.5f);
                tooltipRoot.pivot = new Vector2(0f, 0f);
            }

            var inner = UIFactory.CreatePanel(tooltipRoot, "Bg", 0f, 0f, 390f, 160f,
                UIFactory.PanelFill, UIFactory.PanelFill, 2f, false);

            tooltipTitle = UIFactory.CreateText(inner.transform, "Title", string.Empty, 21, UIFactory.Accent,
                TextAnchor.UpperLeft, 16f, 12f, 358f, 30f, FontStyle.Bold);

            tooltipBody = UIFactory.CreateText(inner.transform, "Body", string.Empty, 16, UIFactory.TextMain,
                TextAnchor.UpperLeft, 16f, 46f, 358f, 100f);

            tooltipRoot.gameObject.SetActive(false);
        }

        private void BuildDragGhost()
        {
            dragGhost = UIFactory.CreateImage(transform, "DragGhost", 0f, 0f, SlotSize, SlotSize,
                new Color(0.85f, 0.85f, 0.85f, 0.9f));
            dragGhost.raycastTarget = false;
            dragGhost.gameObject.SetActive(false);
        }

        // ─────────────────────────────────────────────────────────────
        //  Открытие / закрытие
        // ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (PlayerControls.ToggleInventoryPressed) Toggle();

            if (!open) return;

            if (PlayerControls.ClosePressed) Close();

            if (PlayerControls.DropPressed) DropSelected();

            UpdateTooltipPosition();
        }

        public void Toggle()
        {
            if (open) Close();
            else Open();
        }

        public void Open()
        {
            if (open) return;
            SetOpen(true);
        }

        public void Close()
        {
            if (!open) return;
            SetOpen(false);
        }

        private void SetOpen(bool value)
        {
            open = value;

            if (panelRoot != null) panelRoot.gameObject.SetActive(value);

            GameplayInput.Locked = value;
            Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = value;

            if (value)
            {
                RefreshAll();
            }
            else
            {
                HideTooltip();
                EndDrag();
                selectedSlot = -1;
                selectedEquip = EquipSlot.None;
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  Обновление содержимого
        // ─────────────────────────────────────────────────────────────

        public void RefreshAll()
        {
            var model = inventory != null ? inventory.Model : null;

            for (int i = 0; i < slotViews.Length; i++)
            {
                if (slotViews[i] == null) continue;
                slotViews[i].Refresh(model != null ? model[i] : null, selectedSlot == i);
            }

            for (int i = 0; i < equipViews.Length; i++)
            {
                if (equipViews[i] == null) continue;
                var slot = equipViews[i].Slot;
                equipViews[i].Refresh(model != null ? model.GetEquipped(slot) : null, selectedEquip == slot);
            }

            RefreshStats();
            RefreshCapacity();
        }

        private void RefreshCapacity()
        {
            var capacityText = panel.Find("CapacityText")?.GetComponent<Text>();
            if (capacityText == null || inventory == null || inventory.Model == null) return;

            capacityText.text = $"Занято слотов: {inventory.Model.UsedSlots} / {inventory.Model.Capacity}";
        }

        private void OnStatsChanged(CharacterStats stats) => RefreshStats();

        private void RefreshStats()
        {
            if (inventory == null || inventory.Stats == null) return;

            var total = inventory.Stats.TotalStats;
            var bonus = inventory.Stats.EquipmentBonus;

            for (int i = 0; i < StatBlock.All.Length; i++)
            {
                var type = StatBlock.All[i];
                float value = total.Get(type);
                float delta = bonus.Get(type);

                bool percentStat = type == StatType.CritChance;

                if (statValues[i] != null)
                    statValues[i].text = percentStat ? $"{value * 100f:0.#}%" : $"{value:0.##}";

                if (statDeltas[i] != null)
                {
                    if (Mathf.Abs(delta) < 0.001f)
                    {
                        statDeltas[i].text = string.Empty;
                    }
                    else
                    {
                        string deltaText = percentStat
                            ? $"{delta * 100f:+0.#;-0.#}%"
                            : $"{delta:+0.##;-0.##}";

                        statDeltas[i].text = deltaText;
                        statDeltas[i].color = UIFactory.TextDim;
                    }
                }
            }

            if (weightText != null)
            {
                float weight = inventory.Model.TotalWeight();
                weightText.text = $"Вес: {weight:0.##} / {total.carryWeight:0.#}";
                weightText.color = UIFactory.TextDim;
            }

            if (goldText != null)
            {
                int gold = 0;
                for (int i = 0; i < inventory.Model.Capacity; i++)
                {
                    var stack = inventory.Model[i];
                    if (stack != null && !stack.IsEmpty) gold += stack.item.goldValue * stack.amount;
                }
                goldText.text = $"Стоимость содержимого: {gold}";
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  Перетаскивание
        // ─────────────────────────────────────────────────────────────

        public void BeginDragFromSlot(int index, PointerEventData eventData)
        {
            if (inventory == null) return;

            var stack = inventory.Model[index];
            if (stack == null || stack.IsEmpty) return;

            dragFrom = index;
            dragEquipFrom = EquipSlot.None;
            dragHandled = false;

            ShowDragGhost(stack);
            HideTooltip();
        }

        public void BeginDragFromEquipped(EquipSlot slot, PointerEventData eventData)
        {
            if (inventory == null) return;

            var stack = inventory.Model.GetEquipped(slot);
            if (stack == null || stack.IsEmpty) return;

            dragFrom = -1;
            dragEquipFrom = slot;
            dragHandled = false;

            ShowDragGhost(stack);
            HideTooltip();
        }

        public void UpdateDrag(PointerEventData eventData)
        {
            if (dragGhost == null || !dragGhost.gameObject.activeSelf) return;
            PlaceAtPointer(dragGhost.rectTransform, eventData.position, new Vector2(-SlotSize * 0.5f, SlotSize * 0.5f));
        }

        public void EndDrag()
        {
            dragFrom = -1;
            dragEquipFrom = EquipSlot.None;
            dragHandled = false;

            if (dragGhost != null) dragGhost.gameObject.SetActive(false);
        }

        public void DropOnSlot(int targetIndex)
        {
            if (dragHandled || inventory == null) return;
            dragHandled = true;

            if (dragFrom >= 0)
            {
                if (!inventory.MoveItem(dragFrom, targetIndex))
                    LabHUD.Toast("Эти стопки нельзя объединить");
            }
            else if (dragEquipFrom != EquipSlot.None)
            {
                // Надетый предмет возвращаем в инвентарь.
                if (!inventory.Unequip(dragEquipFrom))
                    LabHUD.Toast("Нет свободного места, чтобы снять предмет");
            }

            RefreshAll();
        }

        public void DropOnEquip(EquipSlot target)
        {
            if (dragHandled || inventory == null) return;
            dragHandled = true;

            if (dragFrom >= 0)
            {
                if (!inventory.EquipToSlot(dragFrom, target))
                {
                    var stack = inventory.Model[dragFrom];
                    if (stack != null && !stack.IsEmpty && stack.item.IsEquippable)
                        LabHUD.Toast($"«{stack.item.SafeName}» не подходит для слота " +
                                     EquipmentSlotUI.ShortSlotName(target));
                    else
                        LabHUD.Toast("Этот предмет нельзя экипировать");
                }
            }

            RefreshAll();
        }

        private void ShowDragGhost(ItemStack stack)
        {
            if (dragGhost == null) return;

            dragGhost.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);
            dragGhost.gameObject.SetActive(true);
            dragGhost.transform.SetAsLastSibling();
        }

        // ─────────────────────────────────────────────────────────────
        //  Клики и выделение
        // ─────────────────────────────────────────────────────────────

        public void ClickSlot(int index, int clickCount)
        {
            if (inventory == null) return;

            selectedSlot = index;
            selectedEquip = EquipSlot.None;

            if (clickCount >= 2)
            {
                var stack = inventory.Model[index];
                if (stack != null && !stack.IsEmpty)
                {
                    if (stack.item.IsEquippable)
                    {
                        inventory.EquipFromSlot(index);
                        selectedSlot = -1;
                        LabHUD.Toast($"Надето: {stack.item.SafeName}");
                    }
                    else
                    {
                        LabHUD.Toast("Этот предмет нельзя надеть");
                    }
                }
            }

            RefreshAll();
        }

        public void ClickEquipped(EquipSlot slot, int clickCount)
        {
            if (inventory == null) return;

            selectedSlot = -1;
            selectedEquip = slot;

            if (clickCount >= 2)
            {
                var stack = inventory.Model.GetEquipped(slot);
                if (stack != null && !stack.IsEmpty)
                {
                    if (inventory.Unequip(slot)) LabHUD.Toast($"Снято: {stack.item.SafeName}");
                    else LabHUD.Toast("В инвентаре нет места");
                }
                selectedEquip = EquipSlot.None;
            }

            RefreshAll();
        }

        public void DropSelected()
        {
            if (inventory == null) return;

            if (selectedEquip != EquipSlot.None)
            {
                if (inventory.DropEquipped(selectedEquip)) selectedEquip = EquipSlot.None;
            }
            else if (selectedSlot >= 0)
            {
                if (inventory.DropSlot(selectedSlot)) selectedSlot = -1;
            }
            else
            {
                LabHUD.Toast("Сначала выберите предмет (ЛКМ), затем G");
                return;
            }

            RefreshAll();
        }

        // ─────────────────────────────────────────────────────────────
        //  Тултип
        // ─────────────────────────────────────────────────────────────

        public void HoverSlot(int index, bool inside)
        {
            if (!inside || inventory == null)
            {
                HideTooltip();
                return;
            }

            var stack = inventory.Model[index];
            if (stack == null || stack.IsEmpty) HideTooltip();
            else ShowTooltip(stack);
        }

        public void HoverEquipped(EquipSlot slot, bool inside)
        {
            if (!inside || inventory == null)
            {
                HideTooltip();
                return;
            }

            var stack = inventory.Model.GetEquipped(slot);
            if (stack == null || stack.IsEmpty) HideTooltip();
            else ShowTooltip(stack);
        }

        private void ShowTooltip(ItemStack stack)
        {
            if (tooltipRoot == null) return;

            var item = stack.item;

            tooltipTitle.text = $"{item.SafeName}" + (stack.amount > 1 ? $"  x{stack.amount}" : string.Empty);

            var sb = new System.Text.StringBuilder();

            string category = item.IsEquippable
                ? $"Экипировка • {EquipmentSlotUI.ShortSlotName(item.equipSlot)}"
                : item.category == ItemCategory.Quest ? "Квестовый предмет"
                : item.category == ItemCategory.Consumable ? "Расходуемый предмет"
                : "Прочее";

            sb.AppendLine(category);

            if (!string.IsNullOrEmpty(item.description)) sb.AppendLine(item.description);

            if (item.modifiers != null && item.modifiers.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Бонусы:");
                for (int i = 0; i < item.modifiers.Length; i++)
                {
                    var mod = item.modifiers[i];
                    sb.AppendLine($"  • {StatBlock.ShortName(mod.stat)}: {mod}");
                }
            }

            sb.AppendLine();
            sb.AppendLine(item.IsEquippable
                ? "Двойной клик — надеть / снять. Перетащите, чтобы надеть вручную."
                : "G — выбросить предмет в мир.");

            tooltipBody.text = sb.ToString();

            // Высота подстраивается под количество строк.
            int lines = tooltipBody.text.Split('\n').Length;
            float height = Mathf.Clamp(58f + lines * 21f, 120f, 420f);

            tooltipRoot.sizeDelta = new Vector2(390f, height);

            if (tooltipRoot.childCount > 0 && tooltipRoot.GetChild(0) is RectTransform inner)
            {
                inner.sizeDelta = new Vector2(390f, height);
                inner.Find("Fill").GetComponent<RectTransform>().sizeDelta =
                    new Vector2(390f - 4f, height - 4f);
            }

            tooltipBody.rectTransform.sizeDelta = new Vector2(358f, height - 52f);
            tooltipRoot.gameObject.SetActive(true);
            tooltipRoot.SetAsLastSibling();
        }

        private void HideTooltip()
        {
            if (tooltipRoot != null) tooltipRoot.gameObject.SetActive(false);
        }

        private void UpdateTooltipPosition()
        {
            if (tooltipRoot == null || !tooltipRoot.gameObject.activeSelf) return;

            PlaceAtPointer(tooltipRoot, Input.mousePosition, new Vector2(20f, 20f), clampRight: 400f);
        }

        /// <summary>Ставит RectTransform так, чтобы он «прилипал» к курсору.</summary>
        private void PlaceAtPointer(RectTransform rect, Vector2 screenPosition, Vector2 offset, float clampRight = 0f)
        {
            if (rect == null) return;

            var container = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screenPosition, null, out var local))
                return;

            var half = container.rect.size * 0.5f;

            Vector2 anchored = local + offset;

            if (clampRight > 0f)
            {
                float maxX = half.x - clampRight;
                if (anchored.x > maxX) anchored.x = maxX;
                float maxY = half.y - tooltipRoot.sizeDelta.y - 20f;
                if (anchored.y > maxY) anchored.y = maxY;
            }

            rect.anchoredPosition = anchored;
        }
    }
}
