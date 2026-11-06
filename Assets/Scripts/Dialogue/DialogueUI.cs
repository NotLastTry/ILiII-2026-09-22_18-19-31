using LabGame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LabGame.Dialogue
{
    /// <summary>
    /// Окно диалога: имя говорящего, реплика и кнопки ответов.
    /// Строится теми же «создать-или-найти» хелперами, что и остальной HUD,
    /// поэтому панель попадает в сцену и не ломается при повторной сборке.
    ///
    /// Управление: 1–4 — выбрать ответ, Esc — закрыть.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        private const int MaxAnswers = 4;
        private const float PanelWidth = 900f;
        private const float PanelHeight = 320f;

        private readonly Button[] buttons = new Button[MaxAnswers];
        private readonly Text[] labels = new Text[MaxAnswers];

        private DialogueRunner runner;
        private RectTransform panel;
        private Text speakerText;
        private Text bodyText;

        public bool IsShown => panel != null && panel.gameObject.activeSelf;

        /// <summary>Находит или создаёт окно диалога внутри HUD.</summary>
        public static DialogueUI Ensure(LabHUD hud)
        {
            if (hud == null || hud.Root == null) return null;

            var ui = hud.GetComponent<DialogueUI>();

            if (ui == null) ui = hud.gameObject.AddComponent<DialogueUI>();

            var fill = UIFactory.CreatePanel(hud.Root, "DialoguePanel", 0f, 0f, PanelWidth, PanelHeight,
                UIFactory.PanelFill, UIFactory.PanelFill, 2f, true);

            var panelRect = (RectTransform)fill.transform.parent;

            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 96f);
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            ui.Build(panelRect, fill.transform);
            return ui;
        }

        private void Awake()
        {
            runner = DialogueRunner.Ensure();

            if (runner == null) return;

            runner.Changed += Refresh;
            runner.Closed += HandleClosed;
        }

        private void OnDestroy()
        {
            if (runner == null) return;

            runner.Changed -= Refresh;
            runner.Closed -= HandleClosed;
        }

        /// <summary>Собирает элементы окна. Повторный вызов ничего не дублирует.</summary>
        public void Build(RectTransform panelRect, Transform content)
        {
            panel = panelRect;

            speakerText = UIFactory.CreateText(content, "Speaker", string.Empty, 18, UIFactory.TextMain,
                TextAnchor.UpperLeft, 24f, 14f, 820f, 28f, FontStyle.Bold);

            bodyText = UIFactory.CreateText(content, "Body", string.Empty, 16, UIFactory.TextMain,
                TextAnchor.UpperLeft, 24f, 48f, PanelWidth - 72f, 104f);

            for (int i = 0; i < MaxAnswers; i++)
            {
                var fill = UIFactory.CreatePanel(content, $"Answer{i}", 24f, 160f + i * 36f,
                    PanelWidth - 72f, 32f, UIFactory.CardFill, UIFactory.Border, 1.5f, true);

                var root = (RectTransform)fill.transform.parent;

                var button = root.GetComponent<Button>();

                if (button == null)
                {
                    button = root.gameObject.AddComponent<Button>();
                    button.targetGraphic = root.GetComponent<Image>();
                    button.navigation = new Navigation { mode = Navigation.Mode.None };
                }

                int index = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Choose(index));

                labels[i] = UIFactory.CreateText(fill.transform, "Label", string.Empty, 16,
                    UIFactory.TextMain, TextAnchor.MiddleLeft, 10f, 0f, PanelWidth - 96f, 32f);

                buttons[i] = button;
                root.gameObject.SetActive(false);
            }

            if (panel != null) panel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (runner == null || !runner.IsOpen) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                runner.Close();
                return;
            }

            for (int i = 0; i < MaxAnswers; i++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;

                runner.Choose(i);
                return;
            }
        }

        private void Choose(int index)
        {
            if (runner != null) runner.Choose(index);
        }

        // ─────────────────────────────────────────────────────────────
        //  Отображение
        // ─────────────────────────────────────────────────────────────

        private void Refresh()
        {
            bool open = runner != null && runner.IsOpen;

            if (panel != null && panel.gameObject.activeSelf != open)
                panel.gameObject.SetActive(open);

            if (!open) return;

            var node = runner.Current;

            if (node == null) return;

            if (speakerText != null) speakerText.text = node.Speaker ?? string.Empty;
            if (bodyText != null) bodyText.text = node.Text ?? string.Empty;

            var answers = runner.Answers;

            for (int i = 0; i < MaxAnswers; i++)
            {
                if (buttons[i] == null) continue;

                bool visible = i < answers.Count;
                buttons[i].gameObject.SetActive(visible);

                if (visible && labels[i] != null)
                    labels[i].text = $"{i + 1}. {answers[i].Text}";
            }

            LabHUD.SetPrompt(null);

            GameplayInput.Locked = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void HandleClosed()
        {
            if (panel != null) panel.gameObject.SetActive(false);

            GameplayInput.Locked = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
