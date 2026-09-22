using System;
using UnityEngine;

public class NPCController : MonoBehaviour
{
    float distance;
    public bool isInRadius = false;
    float radius = 4f;

    int questItemCount = 3;

    Animator animator;

    //["Bools"]
    [SerializeField]
    public bool isTalking = false;
    [SerializeField]
    public bool isQuestAccepted = false;
    [SerializeField]
    public bool isQuestDone = false;

    GameObject player;
    PlayerInventory inventory;

    private void Start()
    {
        player = GameObject.FindWithTag("Player");
        inventory = player.GetComponent<PlayerInventory>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        IsTalking();
        IsQuestAccepted();
        IsQuestDone();
        QuestTurnIn();
        AnimationControl();
    }

    public void AnimationControl()
    {
        if (isTalking)
        {
            animator.SetBool("isTalking", true);
        }
        else
        {
            animator.SetBool("isTalking", false);
        }

        if (isQuestAccepted)
        {
            animator.SetBool("isQuestAccepted", true);
        }
        else
        {
            animator.SetBool("isQuestAccepted", false);
        }

        if (isQuestDone)
        {
            animator.SetBool("isQuestDone", true);
        }
        else
        {
            animator.SetBool("isQuestDone", false);
        }
    }

    private void IsTalking()
    {
        distance = Vector3.Distance(transform.position, player.transform.position);
        isInRadius = distance <= radius;
        if (isInRadius)
        {
            isTalking = true;
        }
        else
        {
            isTalking = false;
        }
    }

    private void IsQuestAccepted()
    {
        if (isInRadius && Input.GetKeyDown(KeyCode.E))
        {
            isQuestAccepted = true;
            TriggerNPCDialogue();

        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            isQuestAccepted = false;
        }
    }

    private void IsQuestDone()
    {
        if (inventory.questItem.count >= questItemCount)
        {
            isQuestDone = true;
        }
        else
        {
            isQuestDone = false;
        }
    }

    private void QuestTurnIn()
    {
        if (isTalking && isQuestDone && isInRadius && Input.GetKeyDown(KeyCode.F))
        {
            isQuestDone = false;
            inventory.questItem.ResetCount();
        }
    }

    private void TriggerNPCDialogue()
    {
        Debug.Log("Соберёшь мне ягоды?");

    }
}
