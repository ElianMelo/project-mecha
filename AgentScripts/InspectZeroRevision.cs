using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class InspectZeroRevision
{
    public static string Rig()
    {
        var text="";
        foreach(var path in new[]{"Assets/_Project/Models/Player/Mecha.fbx","Assets/_Project/Prefabs/Characters/WingZeroVisual.prefab"})
        {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try
            {
                var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();text+=path+" rootScale="+go.transform.lossyScale+" skinScale="+skin.transform.lossyScale+" bounds="+skin.bounds+"\n";
                foreach(var name in new[]{"Bip001","Bip001 L Thigh","Bip001 L Calf","Bip001 L Foot","Bip001 L Toe0"}){var t=go.GetComponentsInChildren<Transform>().First(x=>x.name==name);text+=name+" pos="+t.position+" local="+t.localPosition+" rot="+t.rotation.eulerAngles+"\n";}
                var mesh=new Mesh();skin.BakeMesh(mesh);text+="baked="+mesh.bounds+" vertex="+mesh.vertices[0]+" transformed="+skin.transform.TransformPoint(mesh.vertices[0])+"\n";UnityEngine.Object.DestroyImmediate(mesh);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        return text;
    }
    public static string Effects()
    {
        var materials=AssetDatabase.FindAssets("t:Material",new[]{"Assets/_Project"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>);
        return string.Join("\n",materials.Where(m=>m.shader.name.Contains("Particle")||m.shader.name.Contains("VFX")||m.shader.name.Contains("Water")).Select(m=>AssetDatabase.GetAssetPath(m)+" shader="+m.shader.name+" queue="+m.renderQueue+" ztest="+(m.HasProperty("_ZTest")?m.GetFloat("_ZTest").ToString():"fixed")));
    }
    public static string BakeCoordinates()
    {
        var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Player/Mecha.fbx"));
        try
        {
            var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();var text="raw="+skin.sharedMesh.bounds+" renderer="+skin.transform.position+" rot="+skin.transform.rotation.eulerAngles+" scale="+skin.transform.lossyScale+"\n";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Player/Idle_V2.anim");clip.SampleAnimation(go,.3f);
            foreach(bool useScale in new[]{false,true}){skin.BakeMesh(mesh,useScale);text+="useScale="+useScale+" bounds="+mesh.bounds+" footMin="+mesh.vertices.Min(v=>skin.transform.TransformPoint(v).y)+" noScaleMin="+mesh.vertices.Min(v=>(skin.transform.position+skin.transform.rotation*v).y)+"\n";}
            UnityEngine.Object.DestroyImmediate(mesh);return text;
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    public static string Cameras()
    {
        return string.Join("\n",UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(c=>c.name+" type="+c.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()?.renderType+" stack="+c.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()?.cameraStack?.Count+" mask="+c.cullingMask));
    }
    public static string ProjectileRenderers()
    {
        var text="";
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/_Project/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.Contains("Projectile")||p.Contains("Bolt")))
        {text+=path+"\n";foreach(var r in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Renderer>(true))text+=r.name+" "+r.GetType().Name+" collider="+(r.GetComponent<Collider>()!=null)+"\n";}
        return text;
    }
    public static string FootPoses()
    {
        var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/WingZeroVisual.prefab"));var text="";var baked=new Mesh();
        try
        {
            var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();var weights=skin.sharedMesh.boneWeights;
            foreach(string name in new[]{"Idle_V2","Running","Jump","Attack_A"})
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/WingZeroEW/"+name+".anim");
                foreach(float fraction in new[]{0f,.25f,.5f,.75f})
                {
                    clip.SampleAnimation(go,clip.length*fraction);skin.BakeMesh(baked,true);var vertices=baked.vertices;text+=name+" "+fraction+" ";
                    foreach(string side in new[]{"L","R"}){float floor=Enumerable.Range(0,vertices.Length).Where(i=>skin.bones[weights[i].boneIndex0].name=="Bip001 "+side+" Foot").Min(i=>skin.transform.TransformPoint(vertices[i]).y);text+=side+" sole="+floor.ToString("F3")+" ";}text+="\n";
                }
            }
            return text;
        }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(baked);}
    }
    public static string Configuration()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var zero=AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/_Project/Settings/Characters/Zero.asset");
        var player=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/MechaPlayer.prefab");
        var camera=Camera.main;var data=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        var text="playing="+EditorApplication.isPlaying+" scene="+scene.path+" dirty="+scene.isDirty+" loadedScenes="+UnityEngine.SceneManagement.SceneManager.sceneCount+"\n";
        text+="Zero definition scale="+zero.visualScale+" prefab scale="+player.transform.Find("Zero Visual").localScale+" runReference="+zero.animation.runReferenceSpeed+" movementSpeed="+zero.movement.runSpeed+"\n";
        text+="Base mask="+camera.cullingMask+" stack="+data.cameraStack.Count+"\n";
        foreach(var overlay in data.cameraStack)
        {
            var info=overlay.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            text+=overlay.name+" mask="+overlay.cullingMask+" type="+info.renderType+" clearDepth="+info.clearDepth+"\n";
        }
        var effects=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/_Project"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<GameObject>).SelectMany(g=>g.GetComponentsInChildren<Renderer>(true))
            .Where(r=>r is ParticleSystemRenderer||r is TrailRenderer||r.GetType().Name=="VFXRenderer"||r.sharedMaterials.Any(m=>m!=null&&m.shader.name.Contains("/Particles/"))).ToArray();
        text+="Effect renderer inventory="+effects.Length+" layers="+string.Join(", ",effects.GroupBy(r=>r.gameObject.layer).Select(g=>g.Key+":"+g.Count()))+"\n";
        text+="Unrouted effect renderers="+string.Join(",",effects.Where(r=>r.gameObject.layer!=CombatVfxRendering.Layer).Select(r=>r.name))+"\n";
        text+="Scene missing scripts="+scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
        return text;
    }
}
