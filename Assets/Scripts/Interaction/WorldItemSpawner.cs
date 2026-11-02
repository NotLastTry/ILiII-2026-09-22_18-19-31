using LabGame.Items;
using UnityEngine;

namespace LabGame
{
    /// <summary>
    /// Создаёт предмет в мире: либо по префабу из ItemDefinition,
    /// либо процедурный куб с цветом предмета (чтобы лабораторная
    /// работа не зависела от наличия арт-ассетов).
    /// </summary>
    public static class WorldItemSpawner
    {
        public static ItemPickup Spawn(ItemDefinition item, int amount, Vector3 position)
        {
            if (item == null) return null;

            GameObject go;
            if (item.worldPrefab != null)
            {
                go = Object.Instantiate(item.worldPrefab, position, Quaternion.identity);
            }
            else
            {
                go = CreateProceduralItem(item, position);
            }

            go.name = $"Item_{item.SafeName}";

            var pickup = go.GetComponent<ItemPickup>();
            if (pickup == null) pickup = go.AddComponent<ItemPickup>();

            pickup.Setup(item, amount);
            return pickup;
        }

        private static GameObject CreateProceduralItem(ItemDefinition item, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.32f;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(renderer.sharedMaterial) { color = item.tint };
                renderer.sharedMaterial = material;
            }

            // Триггер, чтобы лежащий предмет не толкал игрока и не мешал движению.
            var collider = go.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;

            return go;
        }
    }
}
