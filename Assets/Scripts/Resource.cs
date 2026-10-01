using UnityEngine;

public class Resource : MonoBehaviour
{
    [Header("Detection")]
    public float maxDistance = 15f;
    public string resourceLayerName = "Resource";

    private GameObject player;
    private PlayerInventory inventory;
    private int layerMask;
    private Camera cam;

    void Start()
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
            Debug.LogWarning("У игрока нет PlayerInventory!");

        int layerIndex = LayerMask.NameToLayer(resourceLayerName);
        if (layerIndex == -1)
        {
            Debug.LogError($"Слой '{resourceLayerName}' не существует!");
            enabled = false;
            return;
        }
        layerMask = 1 << layerIndex;

        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Camera.main не найдена! Проверьте тег камеры.");
            enabled = false;
        }
    }

    void Update()
    {
        GetResource();

    }

    public void GetResource()
    {
        // Стреляем из центра экрана
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        int layerMask = LayerMask.GetMask("Resource");

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log($"Попадание: {hit.collider.tag} ({hit.collider.name})");

            if (hit.collider.CompareTag("Resource"))
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    if (inventory != null && inventory.questItem != null)
                        inventory.questItem.IncreaseCount();
                    else
                        Debug.LogWarning("Инвентарь или questItem не готовы!");
                }
            }
        }
        // Если hit не сработал — ничего не делаем, hit.collider не трогаем
    }
}