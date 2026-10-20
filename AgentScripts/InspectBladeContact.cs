using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Operator inspection of native source motion; does not run tests or change assets.
public static class InspectBladeContact
{
    public static object Motion()
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/TallgeeseVisual.prefab"));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
        var animator=go.GetComponentInChildren<Animator>();
        animator.runtimeAnimatorController=null;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var sword=go.GetComponentInChildren<SwordSwingPresentation>(true).transform;
        var profile=AssetDatabase.LoadAssetAtPath<CharacterAnimationSettings>("Assets/_Project/Settings/Animation/PlayerAnimation.asset");
        var result=new List<object>();
        try
        {
            foreach(string name in new[]{"Slash1","Slash2","Slash3","Heavy1","Heavy2","Heavy3"})
            {
                var data=AssetDatabase.LoadAssetAtPath<AttackPiece>("Assets/_Project/Settings/Attacks/"+name+".asset").action;
                var graph=PlayableGraph.Create("Blade motion inspection");
                try
                {
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var clip=profile.Clip(name);var playable=AnimationClipPlayable.Create(graph,clip);
                    AnimationPlayableOutput.Create(graph,"Native pose",animator).SetSourcePlayable(playable);graph.Play();
                    var samples=new List<object>();Vector3 previous=Vector3.zero;
                    for(int i=0;i<=20;i++)
                    {
                        playable.SetTime(clip.length*i/20f);graph.Evaluate(0);
                        Vector3 tip=sword.TransformPoint(new Vector3(0,1.45f,0));
                        samples.Add(new{time=Math.Round(data.duration*i/20f,3),tip=tip.ToString("F2"),travel=i==0?0:Math.Round((tip-previous).magnitude,3)});
                        previous=tip;
                    }
                    result.Add(new{name,data.impact,data.hitActiveTime,clip=clip.name,samples});
                }
                finally{graph.Destroy();}
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(go);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        return result;
    }
}
