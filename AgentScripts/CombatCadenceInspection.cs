using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Operator-invoked Edit Mode animation inspection; no gameplay session or tests.
public static class CombatCadenceInspection
{
    public static object Motion()
    {
        var report = new List<object>();
        foreach (var entry in new[]{
            new[]{"Dasher","Assets/_Project/Prefabs/Characters/TallgeeseVisual.prefab","Player"},
            new[]{"Zero","Assets/_Project/Prefabs/Characters/WingZeroVisual.prefab","WingZero"},
            new[]{"Enemy","Assets/_Project/Prefabs/Characters/EvaVisual.prefab","Enemy"}})
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(entry[1]));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            var graph = PlayableGraph.Create("Native reaction inspection");
            try
            {
                var animator=go.GetComponentInChildren<Animator>(true);
                animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;
                var profile=AssetDatabase.LoadAssetAtPath<CharacterAnimationSettings>("Assets/_Project/Settings/Animation/"+entry[2]+"Animation.asset");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var controller=AnimatorControllerPlayable.Create(graph,profile.controller);
                AnimationPlayableOutput.Create(graph,"Pose",animator).SetSourcePlayable(controller); graph.Play();
                var samples=new List<object>();
                controller.SetFloat("PlaybackSpeed",profile.Clip("StandUp").length/.48f);
                controller.Play("StandUp",0,0f);graph.Evaluate(.01f);
                samples.Add(new{phase="stand-up start",normalized=controller.GetCurrentAnimatorStateInfo(0).normalizedTime,
                    head=animator.GetBoneTransform(HumanBodyBones.Head).position.y});
                graph.Evaluate(.22f);
                samples.Add(new{phase="stand-up middle",normalized=controller.GetCurrentAnimatorStateInfo(0).normalizedTime,
                    head=animator.GetBoneTransform(HumanBodyBones.Head).position.y});
                graph.Evaluate(.24f);
                samples.Add(new{phase="stand-up end",normalized=controller.GetCurrentAnimatorStateInfo(0).normalizedTime,
                    head=animator.GetBoneTransform(HumanBodyBones.Head).position.y});
                controller.SetFloat("PlaybackSpeed",profile.Clip("Death").length/1.25f);
                controller.Play("Death",0,0f);graph.Evaluate(.01f);
                samples.Add(new{phase="death start",normalized=controller.GetCurrentAnimatorStateInfo(0).normalizedTime,
                    head=animator.GetBoneTransform(HumanBodyBones.Head).position.y});
                graph.Evaluate(1.2f);
                samples.Add(new{phase="death end",normalized=controller.GetCurrentAnimatorStateInfo(0).normalizedTime,
                    head=animator.GetBoneTransform(HumanBodyBones.Head).position.y});
                report.Add(new{character=entry[0],avatar=animator.avatar.name,valid=animator.avatar.isValid,human=animator.avatar.isHuman,samples});
            }
            finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(go);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        }
        return report;
    }
    public static object ClipPoses()
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/TallgeeseVisual.prefab"));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
        var animator=go.GetComponent<Animator>(); animator.runtimeAnimatorController=null;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var report=new List<object>();
        try
        {
            var profile=AssetDatabase.LoadAssetAtPath<CharacterAnimationSettings>("Assets/_Project/Settings/Animation/PlayerAnimation.asset");
            foreach(string state in new[]{"Idle","Death","Flinch","HitReaction","Knockdown"})
            {
                var graph=PlayableGraph.Create("Clip pose inspection");
                try
                {
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var clip=profile.Clip(state);var playable=AnimationClipPlayable.Create(graph,clip);
                    AnimationPlayableOutput.Create(graph,"Pose",animator).SetSourcePlayable(playable);graph.Play();
                    var samples=new List<object>();
                    foreach(float fraction in new[]{0f,.25f,.5f,.75f,.99f})
                    {
                        playable.SetTime(clip.length*fraction);graph.Evaluate(0f);
                        samples.Add(new{fraction,head=animator.GetBoneTransform(HumanBodyBones.Head).position.y,
                            pelvis=animator.GetBoneTransform(HumanBodyBones.Hips).position.y,feet=animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y});
                    }
                    report.Add(new{state,clip=clip.name,samples});
                }
                finally{graph.Destroy();}
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(go);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        return report;
    }
    public static string RenderSource()
    {
        var preview=new PreviewRenderUtility();var objects=new List<GameObject>();var graphs=new List<PlayableGraph>();
        try
        {
            var profile=AssetDatabase.LoadAssetAtPath<CharacterAnimationSettings>("Assets/_Project/Settings/Animation/PlayerAnimation.asset");
            for(int i=0;i<3;i++)
            {
                var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/TallgeeseVisual.prefab"));objects.Add(go);
                preview.AddSingleGO(go);go.transform.position=new Vector3(i*2.2f,0,0);go.transform.rotation=Quaternion.Euler(0,180,0);
                var animator=go.GetComponent<Animator>();animator.runtimeAnimatorController=null;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                var graph=PlayableGraph.Create("Native source pose");graphs.Add(graph);graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var clip=profile.Clip("Death");var playable=AnimationClipPlayable.Create(graph,clip);
                AnimationPlayableOutput.Create(graph,"Pose",animator).SetSourcePlayable(playable);graph.Play();playable.SetTime(clip.length*(i==2?.99f:i*.5f));graph.Evaluate(0);
            }
            preview.camera.transform.position=new Vector3(2.2f,1.1f,-8f);preview.camera.transform.LookAt(new Vector3(2.2f,1f,0));
            preview.camera.orthographic=true;preview.camera.orthographicSize=1.25f;preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=30f;
            preview.camera.backgroundColor=new Color(.12f,.12f,.14f);preview.camera.clearFlags=CameraClearFlags.SolidColor;
            preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=Quaternion.Euler(40,40,0);preview.ambientColor=new Color(.5f,.5f,.5f);
            preview.BeginStaticPreview(new Rect(0,0,1400,600));preview.Render(true);var texture=preview.EndStaticPreview();
            string path=System.IO.Path.GetFullPath("Logs/CombatCadence/NativeDeathSource.png");System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);return path;
        }
        finally{foreach(var graph in graphs)graph.Destroy();foreach(var go in objects)UnityEngine.Object.DestroyImmediate(go);preview.Cleanup();}
    }
}
