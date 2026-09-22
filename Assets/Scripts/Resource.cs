using UnityEngine;

public class Resource : MonoBehaviour
{
    GameObject player;
    PlayerInventory inventory;

    public float maxDistance = 5f;
    void Start()
    {
        player = GameObject.FindWithTag("Player");
        inventory = player.GetComponent<PlayerInventory>();
    }

    void Update()
    {
        GetResource();
    }

    public void GetResource()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // Проверяем, попал ли луч в объект
        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            // Проверяем имя или тег конкретного объекта
            if (hit.collider.CompareTag("Resource"))
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    inventory.questItem.IncreaseCount();

                }
            }
        }

    }
}
