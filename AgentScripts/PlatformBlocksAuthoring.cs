using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

// Explicit native cube/quad prefab and native normal-map import authoring.
public static class PlatformBlocksAuthoring
{
    public const string Root="Assets/_Project/VFX/SceneSupport/PlatformBlocks";
    static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;string parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path));}
    static void Save(UnityEngine.Object asset){EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);}
    static Material Material(string name,Texture2D atlas,Texture2D normal,Sprite sprite)
    {
        string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",atlas!=null?new Color(.75f,.75f,.75f,1):new Color(.08f,.105f,.13f,1));
        m.SetFloat("_Metallic",atlas!=null?.25f:.5f);m.SetFloat("_Smoothness",.32f);
        m.SetFloat("_Surface",0);m.SetFloat("_Cull",2);m.SetFloat("_ReceiveShadows",0);m.EnableKeyword("_RECEIVE_SHADOWS_OFF");
        if(atlas!=null)
        {
            m.SetTexture("_BaseMap",atlas);m.SetTexture("_BumpMap",normal);m.SetFloat("_BumpScale",.75f);m.EnableKeyword("_NORMALMAP");
            var rect=sprite.rect;var scale=new Vector2(rect.width/atlas.width,rect.height/atlas.height);var offset=new Vector2(rect.x/atlas.width,rect.y/atlas.height);
            m.SetTextureScale("_BaseMap",scale);m.SetTextureOffset("_BaseMap",offset);
            m.SetTexture("_EmissionMap",atlas);m.SetColor("_EmissionColor",new Color(.18f,.18f,.18f));m.EnableKeyword("_EMISSION");m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
            m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.1f);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;
        }
        Save(m);return m;
    }
    static GameObject Prefab(string name,Material face,Material body)
    {
        string path=Root+"/Prefabs/"+name+".prefab";var root=new GameObject(name);
        try
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name="Metal Block";cube.transform.SetParent(root.transform,false);
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());cube.GetComponent<MeshRenderer>().sharedMaterial=body;
            var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Textured Front";quad.transform.SetParent(root.transform,false);quad.transform.localPosition=new Vector3(0,0,-.502f);
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());quad.GetComponent<MeshRenderer>().sharedMaterial=face;
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>()){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
            return PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    static void PrefabFaces(ScenarioSettings settings)
    {
        string path="Assets/_Project/Prefabs/Assemblies/ArenaPlatforms.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach(var face in root.GetComponentsInChildren<SpriteRenderer>(true).Where(s=>s.sprite!=null&&s.sprite.texture==settings.platformAtlas).ToArray())
            {
                var parent=face.transform.parent;var collider=parent.GetComponent<Collider>();if(collider==null)continue;
                bool floor=face.sprite==settings.floorSprite;var bounds=collider.bounds;
                MechaScenarioSetup.Face(parent,face.name,face.sprite,new Vector3(bounds.center.x,bounds.max.y,0),bounds.size.x,floor?settings.floorFaceHeight:settings.floatingFaceHeight,settings,floor?settings.floorBlockPrefab:settings.floatingBlockPrefab);
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static object Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/MainScene.unity"||scene.isDirty)throw new InvalidOperationException("Preserve unrelated scene changes");
        Folder(Root+"/Textures");Folder(Root+"/Materials");Folder(Root+"/Prefabs");
        var settings=AssetDatabase.LoadAssetAtPath<ScenarioSettings>("Assets/_Project/Settings/World/Scenario.asset");
        string normalPath=Root+"/Textures/IndustrialPlatformNormals.png";
        if(!System.IO.File.Exists(normalPath)&&!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(settings.platformAtlas),normalPath))throw new InvalidOperationException("Cannot copy source atlas");
        var importer=(TextureImporter)AssetImporter.GetAtPath(normalPath);importer.textureType=TextureImporterType.NormalMap;importer.convertToNormalmap=true;importer.sRGBTexture=false;
        importer.alphaSource=TextureImporterAlphaSource.None;importer.alphaIsTransparency=false;importer.heightmapScale=.035f;importer.normalmapFilter=TextureImporterNormalFilter.Sobel;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        var body=Material("PlatformMetalEdges",null,null,null);var floor=Material("FloorBlockFront",settings.platformAtlas,normal,settings.floorSprite);var floating=Material("FloatingBlockFront",settings.platformAtlas,normal,settings.floatingSprite);
        settings.floorBlockPrefab=Prefab("FloorBlock",floor,body);settings.floatingBlockPrefab=Prefab("FloatingBlock",floating,body);settings.platformVisualDepth=.6f;settings.platformFrontDepth=.1f;Save(settings);
        PrefabFaces(settings);MechaScenarioSetup.ApplyPlatforms(settings);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        var paths=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith(Root+"/")).ToArray();
        foreach(var path in paths){var asset=AssetDatabase.LoadMainAssetAtPath(path);if(asset==null||AssetDatabase.IsValidFolder(path))continue;var labels=AssetDatabase.GetLabels(asset).Where(x=>x!="Keep").Append("Replace").Distinct().ToArray();AssetDatabase.SetLabels(asset,labels);}
        MechaAssetLabels.Apply(paths,0,paths.Length);
        return new{normal=normalPath,normalSize=new{normal.width,normal.height},prefabs=new[]{AssetDatabase.GetAssetPath(settings.floorBlockPrefab),AssetDatabase.GetAssetPath(settings.floatingBlockPrefab)},frontNormal=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(settings.floorBlockPrefab)).transform.Find("Textured Front").GetComponent<MeshFilter>().sharedMesh.normals[0].ToString(),sceneDirty=scene.isDirty};
    }
}
