using LabGame.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace LabGame.EditModeTests
{
    /// <summary>
    /// Проверки того, что сборка интерфейса повторяемая: при повторном
    /// вызове объекты находятся, а не создаются заново, и правки,
    /// сделанные в сцене, не перезаписываются.
    /// </summary>
    public class UIFactoryTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("root", typeof(RectTransform));
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [Test]
        public void CreateRect_ReusesExistingChild()
        {
            var first = UIFactory.CreateRect(root.transform, "child", 0f, 0f, 10f, 10f);
            var second = UIFactory.CreateRect(root.transform, "child", 0f, 0f, 50f, 50f);

            Assert.AreSame(first, second, "должен вернуться существующий объект");
            Assert.AreEqual(1, root.transform.childCount, "дубликат создаваться не должен");
            Assert.AreEqual(10f, second.sizeDelta.x, 0.01f, "размер существующего объекта не перезаписывается");
        }

        [Test]
        public void CreateText_KeepsManualEdits()
        {
            var first = UIFactory.CreateText(root.transform, "label", "первый", 16, Color.black,
                TextAnchor.MiddleLeft, 0f, 0f, 100f, 20f);

            first.text = "изменено вручную";

            var second = UIFactory.CreateText(root.transform, "label", "второй", 16, Color.black,
                TextAnchor.MiddleLeft, 0f, 0f, 100f, 20f);

            Assert.AreSame(first, second);
            Assert.AreEqual("изменено вручную", second.text, "текст, поправленный в сцене, не перезаписывается");
        }

        [Test]
        public void CreateImage_AddsComponentOnlyOnce()
        {
            var first = UIFactory.CreateImage(root.transform, "img", 0f, 0f, 10f, 10f, Color.white);
            var second = UIFactory.CreateImage(root.transform, "img", 0f, 0f, 10f, 10f, Color.red);

            Assert.AreSame(first, second);
            Assert.AreEqual(1, root.GetComponentsInChildren<Image>(true).Length);
            Assert.AreEqual(Color.white, second.color, "цвет существующего элемента не перезаписывается");
        }

        [Test]
        public void CreatePanel_CreatesSingleFill()
        {
            UIFactory.CreatePanel(root.transform, "panel", 0f, 0f, 100f, 60f, Color.white, Color.black);
            UIFactory.CreatePanel(root.transform, "panel", 0f, 0f, 100f, 60f, Color.white, Color.black);

            var panel = root.transform.Find("panel");

            Assert.IsNotNull(panel);
            Assert.AreEqual(1, panel.childCount, "внутри панели должна быть одна заливка");
            Assert.AreEqual("Fill", panel.GetChild(0).name);
        }
    }
}
