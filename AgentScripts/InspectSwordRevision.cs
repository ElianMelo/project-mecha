using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class InspectSwordRevision
{
    public static string Setup()
    {
        string report="";
        foreach(string path in new[]{"Assets/_Project/VFX/Melee/Materials/Sword7Blue.mat","Assets/Blink/Art/Weapons/LowPoly/FreeSwords/Sword7/Sword7_Material_Blue.mat"})
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);report+=path+" shader="+m.shader.name+"\n";
            for(int i=0;i<m.shader.GetPropertyCount();i++)
            {
                string n=m.shader.GetPropertyName(i);if(!n.ToLower().Contains("emiss")&&!n.Contains("Base")&&!n.Contains("MainTex"))continue;
                var type=m.shader.GetPropertyType(i);report+=n+" "+(type==ShaderPropertyType.Texture?(m.GetTexture(n)!=null?AssetDatabase.GetAssetPath(m.GetTexture(n)):"none"):type==ShaderPropertyType.Color?m.GetColor(n).ToString():type==ShaderPropertyType.Vector?m.GetVector(n).ToString():m.GetFloat(n).ToString())+"\n";
            }
        }
        foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {var d=camera.GetComponent<UniversalAdditionalCameraData>();report+=camera.name+" HDR="+camera.allowHDR+" pp="+d?.renderPostProcessing+" layers="+d?.volumeLayerMask.value+"\n";}
        foreach(var volume in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {if(volume.sharedProfile.TryGet<Bloom>(out var bloom))report+="Bloom "+bloom.active+" intensity="+bloom.intensity.value+" override="+bloom.intensity.overrideState+" threshold="+bloom.threshold.value+" scatter="+bloom.scatter.value+"\n";}
        return report;
    }
    public static string Source()
    {
        string path="Assets/DoubleL/FBX Unity/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_1_InPlace.fbx";
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
        return "avatar="+importer.sourceAvatar?.name+"\n"+string.Join("\n",go.GetComponentsInChildren<Transform>(true).Where(t=>t.name.ToLower().Contains("weapon")||t.name.ToLower().Contains("hand")).Select(t=>t.name+" parent="+t.parent?.name+" position="+t.localPosition+" rot="+t.localRotation.eulerAngles))+"\nweapon curves="+string.Join(",",AnimationUtility.GetCurveBindings(clip).Where(b=>b.path.ToLower().Contains("weapon")).Select(b=>b.path+" "+b.propertyName));
    }
}
