using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LabGame.EditorTools
{
    /// <summary>
    /// Настраивает аниматор NPC под поведение из ПЗ №5:
    /// добавляет состояния замаха и удара и переходы по триггерам,
    /// которые выставляет RovingNPC.
    ///
    /// Контроллер общий для всех NPC, поэтому настройка применяется
    /// сразу к обычному NPC и к хаотичному.
    /// </summary>
    public static class NpcAnimatorSetup
    {
        private const string ControllerPath = "Assets/Models/NPC/NPC.controller";
        private const string ClipsFolder = "Assets/Anims/NPC";

        private const string OkMarker = "NPCANIM_OK";
        private const string FailMarker = "NPCANIM_FAIL";

        [MenuItem("Lab 7-8/4. Настроить аниматор NPC", priority = 4)]
        public static void Setup()
        {
            try
            {
                Run();
                Debug.Log(OkMarker);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{FailMarker}: {exception}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void Run()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) throw new Exception($"не найден {ControllerPath}");

            EnsureParameter(controller, "PrepareAttack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);

            var machine = controller.layers[0].stateMachine;

            var idle = FindState(machine, "Idle");
            if (idle == null) throw new Exception("в контроллере нет состояния Idle");

            // Если в проекте есть настоящие боевые клипы — используем их,
            // иначе откатываемся на нейтральные.
            var prepareClip = TryLoadClip("Fighting Idle") ?? LoadClip("Holding Idle");
            var attackClip = TryLoadClip("Standing Melee Attack Horizontal") ?? LoadClip("Happy");

            var prepare = EnsureState(machine, "PrepareAttack", prepareClip, 1.2f);
            var attack = EnsureState(machine, "Attack", attackClip, 1.4f);

            // Триггеры срабатывают из любого состояния — удар важнее анимации покоя.
            EnsureTriggerTransition(machine, prepare, "PrepareAttack");
            EnsureTriggerTransition(machine, attack, "Attack");

            // После проигрывания возвращаемся в стойку.
            EnsureExitTransition(prepare, idle, 0.95f);
            EnsureExitTransition(attack, idle, 0.95f);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            var states = string.Join(", ", machine.states.Select(s => s.state.name));
            Debug.Log($"[NpcAnimator] состояния: {states}");
        }

        // ─────────────────────────────────────────────────────────────
        //  Параметры и состояния
        // ─────────────────────────────────────────────────────────────

        private static void EnsureParameter(AnimatorController controller, string name,
            AnimatorControllerParameterType type)
        {
            if (controller.parameters.Any(p => p.name == name && p.type == type)) return;

            controller.AddParameter(name, type);
        }

        private static AnimatorState EnsureState(AnimatorStateMachine machine, string name,
            AnimationClip clip, float speed)
        {
            var state = FindState(machine, name);

            if (state == null)
            {
                state = machine.AddState(name);
                Debug.Log($"[NpcAnimator] добавлено состояние {name}");
            }

            state.motion = clip;
            state.speed = speed;
            state.writeDefaultValues = true;

            return state;
        }

        private static AnimatorState FindState(AnimatorStateMachine machine, string name)
        {
            for (int i = 0; i < machine.states.Length; i++)
            {
                var state = machine.states[i].state;
                if (state != null && state.name == name) return state;
            }

            return null;
        }

        // ─────────────────────────────────────────────────────────────
        //  Переходы
        // ─────────────────────────────────────────────────────────────

        private static void EnsureTriggerTransition(AnimatorStateMachine machine, AnimatorState destination,
            string trigger)
        {
            bool exists = machine.anyStateTransitions.Any(t => t != null && t.destinationState == destination);
            if (exists) return;

            var transition = machine.AddAnyStateTransition(destination);

            transition.hasExitTime = false;
            transition.duration = 0.05f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void EnsureExitTransition(AnimatorState source, AnimatorState destination, float exitTime)
        {
            bool exists = source.transitions.Any(t => t != null && t.destinationState == destination);
            if (exists) return;

            var transition = source.AddTransition(destination);

            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.duration = 0.1f;
            transition.canTransitionToSelf = false;
        }

        // ─────────────────────────────────────────────────────────────
        //  Клипы
        // ─────────────────────────────────────────────────────────────

        private static AnimationClip LoadClip(string fileName)
        {
            var clip = TryLoadClip(fileName);
            if (clip != null) return clip;

            throw new Exception($"не найден клип {fileName} в {ClipsFolder}");
        }

        private static AnimationClip TryLoadClip(string fileName)
        {
            string path = $"{ClipsFolder}/{fileName}.fbx";

            if (!System.IO.File.Exists(path)) return null;

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);

            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }

            return null;
        }
    }
}
