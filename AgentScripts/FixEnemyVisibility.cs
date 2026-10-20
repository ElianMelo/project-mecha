using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Native Avatar authoring: build in model units; the prefab supplies world scale once.
public static class FixEnemyVisibility
{
    public static Avatar Build()
    {
        var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/EvaVisual.prefab"));
        try
        {
            go.name="Enemy Visual";go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            go.transform.localScale=Vector3.one;
            var animator=go.GetComponent<Animator>();animator.runtimeAnimatorController=null;animator.avatar=null;
            var setup=typeof(Editor).Assembly.GetType("UnityEditor.AvatarSetupTool");
            object[] args={go,new HumanBone[0],new SkeletonBone[0],false};
            setup.GetMethod("SetupHumanSkeleton",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);
            var description=new HumanDescription{human=(HumanBone[])args[1],skeleton=(SkeletonBone[])args[2],hasTranslationDoF=(bool)args[3],
                upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.05f,legStretch=.05f};
            var avatar=AvatarBuilder.BuildHumanAvatar(go,description);avatar.name="EnemyHumanoid";
            if(!avatar.isValid||!avatar.isHuman)throw new InvalidOperationException("Invalid native enemy Avatar");
            return avatar;
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    public static object Preview()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Preview in Play Mode");
        var avatar=Build();
        foreach(var enemy in EnemyController.ActiveEnemies)
        {
            var animator=enemy.GetComponentInChildren<Animator>();animator.avatar=avatar;animator.Rebind();animator.Update(0f);
        }
        return EnemyController.ActiveEnemies.Select(e=>new{e.name,actor=e.transform.position.ToString(),
            hips=e.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Hips).position.ToString(),
            bounds=e.GetComponentInChildren<SkinnedMeshRenderer>().bounds.ToString()}).ToArray();
    }
    public static string Save()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Save after Play Mode");
        var avatar=Build();var saved=AssetDatabase.LoadAssetAtPath<Avatar>("Assets/_Project/Animations/Humanoid/EnemyHumanoid.asset");
        EditorUtility.CopySerialized(avatar,saved);UnityEngine.Object.DestroyImmediate(avatar);
        EditorUtility.SetDirty(saved);AssetDatabase.SaveAssetIfDirty(saved);
        return "Native enemy Avatar rebuilt at unit model scale; existing GUID, prefab scales and source mesh retained";
    }
}
