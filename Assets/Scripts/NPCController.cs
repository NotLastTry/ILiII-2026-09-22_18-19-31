using System;
using LabGame;
using LabGame.Controls;
using LabGame.Dialogue;
using UnityEngine;

public enum NPCQuestState
{
    Idle,           // NPC ждёт игрока
    Offer,          // Игрок в радиусе, NPC предлагает квест
    QuestActive,    // Квест принят, игрок собирает предметы
    QuestDone,      // Игрок собрал нужное количество
    RewardGiven     // Награда выдана, квест завершён
}

public class NPCController : MonoBehaviour
{
    [Header("Interaction")]
    public float radius = 4f;
    [SerializeField] public bool isInRadius = false;

    [Header("Quest")]
    public int questItemCount = 3;
    public NPCQuestState questState = NPCQuestState.Idle;

    // Флаги для Animator
    [SerializeField] public bool isTalking = false;
    [SerializeField] public bool isQuestAccepted = false;
    [SerializeField] public bool isQuestDone = false;

    private GameObject player;
    private PlayerInventory inventory;
    private Animator animator;
    private DialogueAgent dialogueAgent;

    private NPCQuestState lastNotifiedQuestState = (NPCQuestState)(-1);

    private void Start()
    {
        player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Игрок с тегом 'Player' не найден!");
            enabled = false;
            return;
        }

        inventory = player.GetComponent<PlayerInventory>();
        if (inventory == null)
            Debug.LogWarning("У игрока нет компонента PlayerInventory!");

        animator = GetComponentInChildren<Animator>();
        dialogueAgent = GetComponent<DialogueAgent>();
    }

    private void Update()
    {
        UpdateDistance();
        HandleQuestInput();
        CheckQuestProgress();
        AnimationControl();
        NotifyQuestStateIfChanged();
    }

    // --- Сообщаем остальным системам (ПЗ №8) об изменении состояния квеста ---
    private void NotifyQuestStateIfChanged()
    {
        if (questState == lastNotifiedQuestState) return;

        lastNotifiedQuestState = questState;
        GameEventBus.RaiseQuestStateChanged((int)questState);
    }

    // --- 1. Проверка расстояния ---
    private void UpdateDistance()
    {
        if (player == null) return;
        float distance = Vector3.Distance(transform.position, player.transform.position);
        isInRadius = distance <= radius;
    }

    // --- 2. Обработка ввода (когда диалогов нет) ---
    private void HandleQuestInput()
    {
        // Инвентарь или диалог открыт — квестовые клавиши не обрабатываем.
        if (GameplayInput.Locked) return;

        // Если на NPC есть система диалогов, разговор ведёт она.
        if (dialogueAgent != null) return;

        // Одно и то же действие доступно и с клавиатуры, и с экранной кнопки.
        bool interact = PlayerControls.InteractPressed;
        bool secondary = PlayerControls.SecondaryPressed;

        // Взятие квеста
        if (isInRadius && questState == NPCQuestState.Idle && interact)
        {
            OfferQuest();
            TriggerNPCDialogue("Соберёшь мне ягоды?");
        }
        // Принятие квеста
        else if (isInRadius && questState == NPCQuestState.Offer && interact)
        {
            AcceptQuest();
            TriggerNPCDialogue("Отлично! Принеси мне 3 ягоды.");
        }
        // Сдача квеста
        else if (isInRadius && questState == NPCQuestState.QuestDone && secondary)
        {
            TurnInQuest();
        }
        // Отмена (только если квест активен)
        else if (questState == NPCQuestState.QuestActive && secondary)
        {
            ResetQuest();
            TriggerNPCDialogue("Ладно, вернёшься — поговорим.");
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Переходы квеста (их вызывает система диалогов)
    // ─────────────────────────────────────────────────────────────

    /// <summary>Предложить квест.</summary>
    public void OfferQuest()
    {
        if (questState != NPCQuestState.Idle) return;

        questState = NPCQuestState.Offer;
        isTalking = true;
    }

    /// <summary>Принять квест.</summary>
    public void AcceptQuest()
    {
        if (questState != NPCQuestState.Offer) return;

        questState = NPCQuestState.QuestActive;
        isQuestAccepted = true;
        isTalking = true;
    }

    /// <summary>Вернуть квест в начальное состояние.</summary>
    public void ResetQuest()
    {
        questState = NPCQuestState.Idle;
        isQuestAccepted = false;
        isQuestDone = false;
    }

    // --- 3. Проверка прогресса квеста ---
    private void CheckQuestProgress()
    {
        if (inventory == null || inventory.questItem == null) return;

        if (questState == NPCQuestState.QuestActive &&
            inventory.questItem.count >= questItemCount)
        {
            questState = NPCQuestState.QuestDone;
            isQuestDone = true;
        }
    }

    // --- 4. Сдача квеста ---
    public void TurnInQuest()
    {
        if (questState != NPCQuestState.QuestDone) return;

        isQuestDone = false;
        isQuestAccepted = false;
        questState = NPCQuestState.RewardGiven;

        if (inventory != null && inventory.questItem != null)
            inventory.questItem.ResetCount();

        TriggerNPCDialogue("Спасибо! Вот твоя награда.");
    }

    // --- 5. Анимация ---
    public void AnimationControl()
    {
        if (animator == null) return;
        animator.SetBool("isTalking", isTalking);
        animator.SetBool("isQuestAccepted", isQuestAccepted);
        animator.SetBool("isQuestDone", isQuestDone);
    }

    private void TriggerNPCDialogue(string text)
    {
        Debug.Log($"[{name}]: {text}");
        isTalking = true;
        // Здесь можно вызвать UI-диалог
        Invoke(nameof(StopTalking), 3f); // через 3 сек — закончить разговор
    }

    private void StopTalking() => isTalking = false;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}