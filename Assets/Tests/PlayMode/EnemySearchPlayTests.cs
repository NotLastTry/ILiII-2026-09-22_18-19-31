using System.Collections;
using LabGame;
using LabGame.AI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LabGame.PlayModeTests
{
    /// <summary>
    /// Сквозные проверки противника с деревом поведения (ПЗ №5).
    /// В тестовой сцене нет NavMesh, поэтому агент не двигается,
    /// но восприятие, выбор ветки и атака работают.
    /// </summary>
    public class EnemySearchPlayTests
    {
        private GameObject playerObject;
        private GameObject enemyObject;

        [SetUp]
        public void SetUp()
        {
            LabServices.Reset();
            LabServices.AutoCreateInterface = false;
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;

            if (enemyObject != null) Object.Destroy(enemyObject);
            if (playerObject != null) Object.Destroy(playerObject);

            LabServices.Reset();
        }

        private CharacterStats CreatePlayer(Vector3 position)
        {
            playerObject = new GameObject("Player") { tag = "Player" };
            playerObject.transform.position = position;

            var inventory = playerObject.AddComponent<PlayerInventory>();
            return inventory != null ? inventory.Stats : null;
        }

        private RovingNPC CreateEnemy(Vector3 position)
        {
            enemyObject = new GameObject("Enemy");
            enemyObject.transform.position = position;
            enemyObject.AddComponent<NavMeshAgent>();

            return enemyObject.AddComponent<RovingNPC>();
        }

        [UnityTest]
        public IEnumerator Enemy_BuildsBehaviourTree()
        {
            CreatePlayer(new Vector3(0f, 0f, 20f));
            var enemy = CreateEnemy(Vector3.zero);

            yield return null;

            string dump = enemy.BuildTreeDebug();

            Assert.IsFalse(string.IsNullOrEmpty(dump), "дерево должно быть построено");
            StringAssert.Contains("Корень", dump);
            StringAssert.Contains("Бой", dump);
            StringAssert.Contains("Преследование", dump);
            StringAssert.Contains("Поиск", dump);
            StringAssert.Contains("Возвращение", dump);
            StringAssert.Contains("Патрулирование", dump);
        }

        [UnityTest]
        public IEnumerator Enemy_DoesNotChasePlayerOutsideSight()
        {
            CreatePlayer(new Vector3(0f, 0f, 40f));
            var enemy = CreateEnemy(Vector3.zero);

            yield return null;
            yield return null;

            Assert.IsFalse(enemy.CanSeePlayer, "игрок слишком далеко");
            Assert.AreNotEqual(EnemyState.Chase, enemy.State);
            Assert.AreNotEqual(EnemyState.Attack, enemy.State);
        }

        [UnityTest]
        public IEnumerator Enemy_AttacksPlayerInRange()
        {
            var stats = CreatePlayer(new Vector3(0f, 0f, 1.4f));
            var enemy = CreateEnemy(Vector3.zero);

            yield return null;

            Assert.IsNotNull(stats, "у игрока должны быть характеристики");
            Assert.IsTrue(enemy.CanSeePlayer, "игрок стоит прямо перед противником");
            Assert.AreEqual(EnemyState.PrepareAttack, enemy.State, "сначала противник замахивается");

            float before = stats.CurrentHealth;

            yield return new WaitForSeconds(1.5f);

            Assert.Less(stats.CurrentHealth, before, "противник должен нанести урон");
        }

        [UnityTest]
        public IEnumerator Enemy_StateIsMeaningful()
        {
            CreatePlayer(new Vector3(0f, 0f, 3f));
            var enemy = CreateEnemy(Vector3.zero);

            yield return null;
            yield return null;

            // Игрок в пределах видимости, но дальше зоны удара — преследование.
            Assert.AreEqual(EnemyState.Chase, enemy.State);
        }
    }
}
