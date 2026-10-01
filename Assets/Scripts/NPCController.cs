using System;
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
    }

    private void Update()
    {
        UpdateDistance();
        HandleQuestInput();
        CheckQuestProgress();
        AnimationControl();
    }

    // --- 1. Проверка расстояния ---
    private void UpdateDistance()
    {
        if (player == null) return;
        float distance = Vector3.Distance(transform.position, player.transform.position);
        isInRadius = distance <= radius;
    }

    // --- 2. Обработка ввода ---
    private void HandleQuestInput()
    {
        // Взятие квеста
        if (isInRadius && questState == NPCQuestState.Idle && Input.GetKeyDown(KeyCode.E))
        {
            questState = NPCQuestState.Offer;
            TriggerNPCDialogue("Соберёшь мне ягоды?");
        }
        // Принятие квеста
        else if (isInRadius && questState == NPCQuestState.Offer && Input.GetKeyDown(KeyCode.E))
        {
            questState = NPCQuestState.QuestActive;
            isQuestAccepted = true;
            TriggerNPCDialogue("Отлично! Принеси мне 3 ягоды.");
        }
        // Сдача квеста
        else if (isInRadius && questState == NPCQuestState.QuestDone && Input.GetKeyDown(KeyCode.F))
        {
            TurnInQuest();
        }
        // Отмена (только если квест активен)
        else if (questState == NPCQuestState.QuestActive && Input.GetKeyDown(KeyCode.F))
        {
            questState = NPCQuestState.Idle;
            isQuestAccepted = false;
            TriggerNPCDialogue("Ладно, вернёшься — поговорим.");
        }
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
    private void TurnInQuest()
    {
        isQuestDone = false;
        isQuestAccepted = false;
        questState = NPCQuestState.RewardGiven;

        inventory.questItem.ResetCount();
        TriggerNPCDialogue("Спасибо! Вот твоя награда.");
        // Здесь можно выдать предмет/опыт
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