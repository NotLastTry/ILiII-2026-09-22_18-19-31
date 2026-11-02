using UnityEngine;

namespace LabGame
{
    /// <summary>
    /// Связывает характеристики персонажа с физикой движения:
    /// надетые предметы меняют <see cref="CharacterStats.TotalStats.moveSpeed"/>,
    /// а этот компонент переносит изменение в <see cref="PhysicsModule.maxSpeed"/>.
    /// Так эффект экипировки виден не только в цифрах, но и в игре.
    /// </summary>
    [RequireComponent(typeof(PlayerInventory))]
    public class PlayerSpeedLink : MonoBehaviour
    {
        private CharacterStats stats;
        private PhysicsModule physics;

        private float referencePhysicsSpeed = 6f;
        private float referenceStatSpeed = 6f;

        private void Awake()
        {
            stats = GetComponent<CharacterStats>();
            physics = GetComponent<PhysicsModule>();

            if (physics != null) referencePhysicsSpeed = Mathf.Max(0.01f, physics.maxSpeed);
            if (stats != null) referenceStatSpeed = Mathf.Max(0.01f, stats.baseStats.moveSpeed);
        }

        private void Start() => Apply(stats);

        private void OnEnable()
        {
            if (stats != null) stats.Changed += Apply;
        }

        private void OnDisable()
        {
            if (stats != null) stats.Changed -= Apply;
        }

        private void Apply(CharacterStats source)
        {
            if (physics == null || source == null) return;

            float ratio = source.TotalStats.moveSpeed / referenceStatSpeed;

            // Ограничение, чтобы «скорость» не превращалась в телепорт.
            physics.maxSpeed = Mathf.Clamp(referencePhysicsSpeed * ratio, 0.5f, referencePhysicsSpeed * 2.5f);
        }
    }
}
