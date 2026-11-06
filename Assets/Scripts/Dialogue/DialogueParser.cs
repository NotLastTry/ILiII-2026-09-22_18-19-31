using System;
using System.Xml;

namespace LabGame.Dialogue
{
    /// <summary>
    /// Разбор диалогового XML (ПЗ №9). Чистая функция «строка → граф»,
    /// поэтому её легко покрыть тестами без сцены.
    ///
    /// Формат:
    /// <code>
    /// &lt;dialogue id="smith" speaker="Кузнец" start="start"&gt;
    ///   &lt;node id="start" text="Привет"&gt;
    ///     &lt;answer text="Есть работа?" to="offer" condition="quest:idle"/&gt;
    ///     &lt;answer text="Пока" to="end"/&gt;
    ///   &lt;/node&gt;
    /// &lt;/dialogue&gt;
    /// </code>
    /// </summary>
    public static class DialogueParser
    {
        /// <summary>Специальная цель ответа — закрыть диалог.</summary>
        public const string ExitTarget = "end";

        public static DialogueGraph Parse(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                throw new DialogueFormatException("пустой XML");

            var document = new XmlDocument();

            try
            {
                document.LoadXml(xml);
            }
            catch (XmlException exception)
            {
                throw new DialogueFormatException($"некорректный XML: {exception.Message}");
            }

            var root = document.DocumentElement;

            if (root == null || !string.Equals(root.Name, "dialogue", StringComparison.OrdinalIgnoreCase))
                throw new DialogueFormatException("корневой элемент должен называться <dialogue>");

            var graph = new DialogueGraph
            {
                Id = root.GetAttribute("id"),
                Speaker = root.GetAttribute("speaker"),
                StartId = string.IsNullOrWhiteSpace(root.GetAttribute("start"))
                    ? "start"
                    : root.GetAttribute("start").Trim()
            };

            var nodeElements = root.SelectNodes("node");

            if (nodeElements == null || nodeElements.Count == 0)
                throw new DialogueFormatException("в диалоге нет ни одного <node>");

            foreach (XmlNode element in nodeElements)
            {
                var node = ParseNode(element as XmlElement, graph.Speaker);

                if (graph.Contains(node.Id))
                    throw new DialogueFormatException($"узел «{node.Id}» объявлен дважды");

                graph.Add(node);
            }

            Validate(graph);
            return graph;
        }

        public static bool TryParse(string xml, out DialogueGraph graph, out string error)
        {
            try
            {
                graph = Parse(xml);
                error = null;
                return true;
            }
            catch (DialogueFormatException exception)
            {
                graph = null;
                error = exception.Message;
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  Узлы и ответы
        // ─────────────────────────────────────────────────────────────

        private static DialogueNode ParseNode(XmlElement element, string defaultSpeaker)
        {
            if (element == null)
                throw new DialogueFormatException("пустой <node>");

            string id = element.GetAttribute("id");

            if (string.IsNullOrWhiteSpace(id))
                throw new DialogueFormatException("<node> без атрибута id");

            id = id.Trim();

            var node = new DialogueNode
            {
                Id = id,
                Speaker = string.IsNullOrWhiteSpace(element.GetAttribute("speaker"))
                    ? defaultSpeaker
                    : element.GetAttribute("speaker").Trim(),
                Text = ReadText(element),
                Action = NullIfEmpty(element.GetAttribute("action"))
            };

            if (string.IsNullOrWhiteSpace(node.Text))
                throw new DialogueFormatException($"у узла «{id}» нет текста");

            var answerElements = element.SelectNodes("answer");

            if (answerElements != null)
            {
                foreach (XmlNode answerElement in answerElements)
                {
                    var answer = ParseAnswer(answerElement as XmlElement, id);
                    node.Answers.Add(answer);
                }
            }

            return node;
        }

        private static DialogueAnswer ParseAnswer(XmlElement element, string nodeId)
        {
            if (element == null)
                throw new DialogueFormatException($"у узла «{nodeId}» пустой <answer>");

            var answer = new DialogueAnswer
            {
                Text = ReadText(element),
                TargetId = element.GetAttribute("to").Trim(),
                Condition = NullIfEmpty(element.GetAttribute("condition")),
                Action = NullIfEmpty(element.GetAttribute("action"))
            };

            if (string.IsNullOrWhiteSpace(answer.Text))
                throw new DialogueFormatException($"у узла «{nodeId}» ответ без текста");

            return answer;
        }

        /// <summary>
        /// Текст берётся из атрибута text, иначе — из текстовых узлов элемента
        /// (дочерние &lt;answer&gt; при этом не мешают).
        /// </summary>
        private static string ReadText(XmlElement element)
        {
            string attribute = element.GetAttribute("text");

            if (!string.IsNullOrWhiteSpace(attribute)) return attribute.Trim();

            var builder = new System.Text.StringBuilder();

            foreach (XmlNode child in element.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Text || child.NodeType == XmlNodeType.CDATA)
                    builder.Append(child.Value);
            }

            return builder.ToString().Trim();
        }

        private static string NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        // ─────────────────────────────────────────────────────────────
        //  Проверка ссылок
        // ─────────────────────────────────────────────────────────────

        private static void Validate(DialogueGraph graph)
        {
            if (graph.Start == null)
                throw new DialogueFormatException("нет стартового узла");

            foreach (var node in graph.Nodes)
            {
                foreach (var answer in node.Answers)
                {
                    if (answer.IsExit) continue;

                    if (!graph.Contains(answer.TargetId))
                    {
                        throw new DialogueFormatException(
                            $"ответ «{answer.Text}» узла «{node.Id}» ведёт в несуществующий узел «{answer.TargetId}»");
                    }
                }
            }
        }
    }
}
