using UnityEngine;

public class QuestItem
{
    public int count = 0;

    public void IncreaseCount()
    {
        count++;
        Debug.Log("+1 Ягода");
    }

    public void DecreaseCount() { count--; }

    public void ResetCount()
    {
        count = 0;
    }
}
