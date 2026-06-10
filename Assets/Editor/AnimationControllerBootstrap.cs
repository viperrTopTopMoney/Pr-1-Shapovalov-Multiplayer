using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class AnimationControllerBootstrap
{
    private const string OutputFolder = "Assets/Generated/Animations";

    [MenuItem("THE LAST LOCK/Build Player Animator Controller")]
    public static void BuildPlayerController()
    {
        BuildController("Player.controller", isPlayer: true);
    }

    [MenuItem("THE LAST LOCK/Build Zombie Animator Controller")]
    public static void BuildZombieController()
    {
        BuildController("Zombie.controller", isPlayer: false);
    }

    private static void BuildController(string fileName, bool isPlayer)
    {
        EnsureFolder(OutputFolder);

        string path = Path.Combine(OutputFolder, fileName).Replace("\\", "/");
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            AssetDatabase.DeleteAsset(path);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        if (isPlayer)
            ConfigurePlayerController(controller);
        else
            ConfigureZombieController(controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Built animator controller: {path}");
    }

    private static void ConfigurePlayerController(AnimatorController controller)
    {
        AddParameters(controller,
            ("Speed", AnimatorControllerParameterType.Float),
            ("MoveSpeed", AnimatorControllerParameterType.Float),
            ("IsRunning", AnimatorControllerParameterType.Bool),
            ("IsDowned", AnimatorControllerParameterType.Bool),
            ("IsDead", AnimatorControllerParameterType.Bool),
            ("Hit", AnimatorControllerParameterType.Trigger),
            ("Revive", AnimatorControllerParameterType.Trigger),
            ("Death", AnimatorControllerParameterType.Trigger));

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState idle = CreateState(sm, "Idle", FindClip("idle"));
        AnimatorState walk = CreateState(sm, "Walk", FindClip("walk"));
        AnimatorState run = CreateState(sm, "Run", FindClip("run"));
        // Survivalist ships without downed/death clips. Idle is a safe placeholder
        // until dedicated clips are imported; the state machine remains ready for them.
        AnimatorState downed = CreateState(sm, "Downed", FindClip("down") ?? FindClip("idle"));
        AnimatorState death = CreateState(sm, "Death", FindClip("death") ?? FindClip("idle"));

        sm.defaultState = idle;
        AddTransition(idle, walk, "Speed", 0.1f, true);
        AddTransition(walk, idle, "Speed", 0.05f, false);
        AddTransition(walk, run, "IsRunning", true, "Speed", 0.1f);
        AddTransition(run, walk, "IsRunning", false, "Speed", 0.1f);
        AddAnyStateTransition(sm, death, "Death");
        AddAnyStateTransition(sm, downed, "IsDowned", true);
        AddAnyStateTransition(sm, idle, "Revive");
    }

    private static void ConfigureZombieController(AnimatorController controller)
    {
        AddParameters(controller,
            ("Speed", AnimatorControllerParameterType.Float),
            ("IsAttacking", AnimatorControllerParameterType.Bool),
            ("IsDead", AnimatorControllerParameterType.Bool),
            ("Death", AnimatorControllerParameterType.Trigger));

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState idle = CreateState(sm, "Idle", FindClip("idle"));
        AnimatorState walk = CreateState(sm, "Walk", FindClip("walk"));
        AnimatorState attack = CreateState(sm, "Attack", FindClip("attack"));
        AnimatorState death = CreateState(sm, "Death", FindClip("death"));

        sm.defaultState = idle;
        AddTransition(idle, walk, "Speed", 0.15f, true);
        AddTransition(walk, idle, "Speed", 0.05f, false);
        AddAnyStateTransition(sm, attack, "IsAttacking", true);
        AddAnyStateTransition(sm, death, "Death");
    }

    private static void AddParameters(AnimatorController controller, params (string name, AnimatorControllerParameterType type)[] parameters)
    {
        foreach (var parameter in parameters)
        {
            if (controller.parameters.Any(p => p.name == parameter.name))
                continue;

            controller.AddParameter(parameter.name, parameter.type);
        }
    }

    private static AnimatorState CreateState(AnimatorStateMachine sm, string name, AnimationClip clip)
    {
        AnimatorState state = sm.states.FirstOrDefault(s => s.state.name == name).state;
        if (state == null)
            state = sm.AddState(name);

        state.motion = clip;
        return state;
    }

    private static void AddTransition(AnimatorState from, AnimatorState to, string param, float threshold, bool greaterThan)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0.12f;
        transition.AddCondition(greaterThan ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, threshold, param);
    }

    private static void AddTransition(AnimatorState from, AnimatorState to, string boolParam, bool expected, string speedParam, float minSpeed)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0.12f;
        transition.AddCondition(expected ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, boolParam);
        transition.AddCondition(AnimatorConditionMode.Greater, minSpeed, speedParam);
    }

    private static void AddAnyStateTransition(AnimatorStateMachine sm, AnimatorState to, string triggerParam)
    {
        AnimatorStateTransition transition = sm.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0.1f;
        transition.AddCondition(AnimatorConditionMode.If, 0f, triggerParam);
    }

    private static void AddAnyStateTransition(AnimatorStateMachine sm, AnimatorState to, string boolParam, bool expected)
    {
        AnimatorStateTransition transition = sm.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0.1f;
        transition.AddCondition(expected ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, boolParam);
    }

    private static AnimationClip FindClip(string token)
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets" });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant().Contains(token.ToLowerInvariant()))
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        }

        return null;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
        string leaf = Path.GetFileName(folder);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        if (string.IsNullOrEmpty(parent))
            AssetDatabase.CreateFolder("Assets", leaf);
        else
            AssetDatabase.CreateFolder(parent, leaf);
    }
}
