using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BattleChess.EditorTools
{
    /// <summary>
    /// Turns an imported, rigged character (e.g. FBX from Mixamo or the Asset Store) into a ready-to-use
    /// Battle Chess piece prefab: builds an Animator Controller with the states the game expects and
    /// auto-assigns animation clips found next to the model by name.
    ///
    /// Clip name keywords (case-insensitive):  idle · walk · run · attack/slash/strike/shoot/fire ·
    ///                                         hit/react/impact · death/die/dying · victory/cheer/celebrate · selected/ready/alert
    /// </summary>
    public static class CharacterSetupWizard
    {
        [MenuItem("Battle Chess/Make Character From Selected Model", priority = 10)]
        public static void MakeFromSelection()
        {
            var model = Selection.activeObject as GameObject;
            string path = model != null ? AssetDatabase.GetAssetPath(model) : null;
            if (model == null || string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("Battle Chess", "Select a character model (FBX or prefab) in the Project window first.", "OK");
                return;
            }

            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null && importer.animationType != ModelImporterAnimationType.Human)
            {
                if (EditorUtility.DisplayDialog("Battle Chess", "This model is not set to a Humanoid rig. Switch it to Humanoid so animations can be shared between characters?", "Make Humanoid", "Keep as is"))
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.SaveAndReimport();
                }
            }

            string folder = Path.GetDirectoryName(path).Replace('\\', '/');
            var clips = FindClips(folder);
            string baseName = model.name;
            string ctrlPath = folder + "/" + baseName + "_BattleChess.controller";
            var ctrl = BuildController(ctrlPath, clips);

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            if (inst == null) inst = Object.Instantiate(model);
            var anim = inst.GetComponentInChildren<Animator>();
            if (anim == null) anim = inst.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            if (anim.GetComponent<AnimationEventRelay>() == null) anim.gameObject.AddComponent<AnimationEventRelay>();
            string prefabPath = folder + "/" + baseName + "_Piece.prefab";
            PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
            Object.DestroyImmediate(inst);
            AssetDatabase.SaveAssets();

            var found = string.Join(", ", clips.Select(kv => kv.Key + "=" + kv.Value.name).ToArray());
            var missing = string.Join(", ", new[] { "idle", "walk", "attack", "death" }.Where(k => !clips.ContainsKey(k)).ToArray());
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            EditorUtility.DisplayDialog("Battle Chess",
                "Created:\n" + prefabPath + "\n" + ctrlPath +
                "\n\nClips found: " + (found.Length > 0 ? found : "none") +
                (missing.Length > 0 ? "\nMissing (add later, then run again): " + missing : "") +
                "\n\nNext: select the theme asset (Assets/BattleChess/Resources/Themes) and drag this prefab into a piece's 'Prefab' field. Add an empty child named 'Muzzle' at the gun tip for shooters.", "OK");
        }

        static Dictionary<string, AnimationClip> FindClips(string folder)
        {
            var keys = new Dictionary<string, string[]>
            {
                { "idle", new[] { "idle" } },
                { "walk", new[] { "walk" } },
                { "run", new[] { "run", "gallop", "sprint" } },
                { "attack", new[] { "attack", "slash", "strike", "shoot", "fire", "punch", "stab", "swing" } },
                { "hit", new[] { "hit", "react", "impact" } },
                { "death", new[] { "death", "die", "dying" } },
                { "victory", new[] { "victory", "cheer", "celebrat" } },
                { "selected", new[] { "selected", "ready", "alert" } },
            };
            var all = new List<AnimationClip>();
            foreach (var guid in AssetDatabase.FindAssets("", new[] { folder }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
                {
                    var c = o as AnimationClip;
                    if (c != null && !c.name.StartsWith("__preview__")) all.Add(c);
                }
            }
            var result = new Dictionary<string, AnimationClip>();
            foreach (var kv in keys)
            {
                var c = all.FirstOrDefault(a => kv.Value.Any(k => a.name.ToLowerInvariant().Contains(k)));
                if (c != null) result[kv.Key] = c;
            }
            return result;
        }

        static AnimatorController BuildController(string path, Dictionary<string, AnimationClip> clips)
        {
            AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Selected", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Die", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Victory", AnimatorControllerParameterType.Trigger);

            var sm = ctrl.layers[0].stateMachine;
            AnimationClip c;

            BlendTree tree;
            var loco = ctrl.CreateBlendTreeInController("Locomotion", out tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            if (clips.TryGetValue("idle", out c)) tree.AddChild(c, 0f);
            if (clips.TryGetValue("walk", out c)) tree.AddChild(c, 1.2f);
            if (clips.TryGetValue("run", out c)) tree.AddChild(c, 3.5f);
            sm.defaultState = loco;

            var attack = State(sm, "Attack", clips, "attack", "Attack");
            var hit = State(sm, "Hit", clips, "hit", null);
            var die = State(sm, "Die", clips, "death", null);
            var victory = State(sm, "Victory", clips, "victory", null);
            var ready = State(sm, "Selected", clips, "selected", null);

            Any(sm, attack, "Attack"); Any(sm, hit, "Hit"); Any(sm, die, "Die"); Any(sm, victory, "Victory");
            Back(attack, loco); Back(hit, loco);

            var toReady = loco.AddTransition(ready);
            toReady.AddCondition(AnimatorConditionMode.If, 0, "Selected");
            toReady.hasExitTime = false; toReady.duration = 0.15f;
            var fromReady = ready.AddTransition(loco);
            fromReady.AddCondition(AnimatorConditionMode.IfNot, 0, "Selected");
            fromReady.hasExitTime = false; fromReady.duration = 0.2f;

            AssetDatabase.SaveAssets();
            return ctrl;
        }

        static AnimatorState State(AnimatorStateMachine sm, string name, Dictionary<string, AnimationClip> clips, string key, string tag)
        {
            var st = sm.AddState(name);
            AnimationClip c;
            if (clips.TryGetValue(key, out c)) st.motion = c;
            else if (clips.TryGetValue("idle", out c)) st.motion = c;
            if (tag != null) st.tag = tag;
            return st;
        }

        static void Any(AnimatorStateMachine sm, AnimatorState to, string trigger)
        {
            var t = sm.AddAnyStateTransition(to);
            t.AddCondition(AnimatorConditionMode.If, 0, trigger);
            t.hasExitTime = false; t.duration = 0.1f; t.canTransitionToSelf = false;
        }

        static void Back(AnimatorState from, AnimatorState to)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = true; t.exitTime = 0.92f; t.duration = 0.15f;
        }
    }
}
