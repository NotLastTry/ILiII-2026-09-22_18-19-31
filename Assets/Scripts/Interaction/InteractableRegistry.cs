using System.Collections.Generic;
using UnityEngine;

namespace LabGame
{
    /// <summary>
    /// Реестр всех интерактивных объектов сцены.
    /// Позволяет найти ближайший предмет без Physics.OverlapSphere
    /// и без поиска по всей сцене каждый кадр.
    /// </summary>
    public static class InteractableRegistry
    {
        private static readonly List<IInteractable> all = new List<IInteractable>();

        public static IReadOnlyList<IInteractable> All => all;

        public static void Register(IInteractable interactable)
        {
            if (interactable == null || all.Contains(interactable)) return;
            all.Add(interactable);
        }

        public static void Unregister(IInteractable interactable)
        {
            if (interactable == null) return;
            all.Remove(interactable);
        }

        public static void Clear() => all.Clear();

        public static IInteractable FindClosest(Vector3 origin, float radius, PlayerInventory player)
        {
            IInteractable best = null;
            float bestSqr = radius * radius;

            for (int i = all.Count - 1; i >= 0; i--)
            {
                var candidate = all[i];

                // Объект мог быть уничтожен — Unity-объекты сравниваются с null особым образом.
                if (candidate is UnityEngine.Object unityObject && unityObject == null)
                {
                    all.RemoveAt(i);
                    continue;
                }

                if (!candidate.CanInteract(player)) continue;

                float sqr = (candidate.InteractionPoint - origin).sqrMagnitude;
                if (sqr > bestSqr) continue;

                bestSqr = sqr;
                best = candidate;
            }

            return best;
        }
    }
}
