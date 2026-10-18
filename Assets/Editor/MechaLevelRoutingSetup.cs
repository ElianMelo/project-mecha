using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Native scene/prefab authoring for a physical routing table, collider interaction and 3D text.
public static class MechaLevelRoutingSetup
{
    private const string Prefabs = "Assets/_Project/Prefabs/Routing";
    private static TMP_FontAsset font;
    private static LevelRouteSettings settings;
    [MenuItem("Tools/Mecha/Apply Level Routing")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring routing.");
        var main = SceneManager.GetActiveScene();
        if (main.path != "Assets/Scenes/MainScene.unity") throw new InvalidOperationException("Open MainScene first.");
        var director = UnityEngine.Object.FindFirstObjectByType<LevelRouteDirector>();
        if (director == null) throw new InvalidOperationException("MainScene needs its existing route director.");
        settings = MechaPresentationSetup.Profile<LevelRouteSettings>("LevelRouting");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        BuildScene();
        SceneManager.SetActiveScene(main);
        var old = GameObject.Find("Level Routing");
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
        var controller = GameObject.Find("Level Routing Scene Controller") ?? new GameObject("Level Routing Scene Controller");
        MechaPresentationSetup.Ensure<RouteMapMenu>(controller);
        foreach (string obsolete in new[] { Prefabs + "/LevelRoutingInterface.prefab", Prefabs + "/RouteNodeControl.prefab" })
            if (AssetDatabase.LoadMainAssetAtPath(obsolete) != null) AssetDatabase.DeleteAsset(obsolete);
        var buildScenes = EditorBuildSettings.scenes.ToList();
        if (!buildScenes.Any(x => x.path == settings.routingScenePath)) buildScenes.Add(new EditorBuildSettingsScene(settings.routingScenePath,true));
        else buildScenes.First(x => x.path == settings.routingScenePath).enabled = true;
        EditorBuildSettings.scenes = buildScenes.ToArray();
        EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(main); EditorSceneManager.SaveScene(main);
        Debug.Log("Routing now uses LevelRouting.unity: direct camera, physical world nodes, PhysicsRaycaster and 3D text. No routing Canvas or RenderTexture.");
    }
    private static void BuildScene()
    {
        var loaded = SceneManager.GetSceneByPath(settings.routingScenePath);
        if (loaded.isLoaded) throw new InvalidOperationException("Close LevelRouting before reauthoring it.");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var table = (GameObject)PrefabUtility.InstantiatePrefab(settings.boardPrefab,scene);
            table.name = "Expedition Table"; table.transform.position = Vector3.zero;
            var camera = table.GetComponentInChildren<Camera>(); camera.name = "Routing Camera";
            camera.orthographic = false; camera.fieldOfView = 42f; camera.targetTexture = null; camera.aspect = 16f/9f;
            camera.transform.position = new Vector3(5.5f,33f,-17f); camera.transform.LookAt(new Vector3(5.5f,0f,12.75f));
            camera.tag = "MainCamera"; camera.depth = 100; camera.farClipPlane = 250f;
            var raycaster = MechaPresentationSetup.Ensure<PhysicsRaycaster>(camera.gameObject); raycaster.eventMask = 1 << settings.boardLayer;
            var root = new GameObject("Routing Scene");
            var board = root.AddComponent<RouteMapBoard>();
            var content = new GameObject("Encounter Nodes and Paths"); content.transform.SetParent(root.transform,false);
            Assign(board,"view",camera); Assign(board,"content",content.transform); Assign(board,"nodePrefab",NodePrefab());
            var terminal = new GameObject("Encounter Briefing Terminal"); terminal.transform.position = new Vector3(20f,3f,13f); terminal.transform.rotation = camera.transform.rotation;
            Cube(terminal.transform,"Terminal Housing",new Vector3(0,0,0.2f),new Vector3(11.8f,19.6f,0.35f),Material("TableSurface"));
            Cube(terminal.transform,"Terminal Top Trim",new Vector3(0,9.7f,0),new Vector3(11.8f,0.12f,0.45f),Material("TableTrim"));
            Assign(board,"title",Text(terminal.transform,"Expedition","EXPEDITION / 01",new Vector3(-5.2f,9,0),new Vector2(10.4f,1.1f),8f));
            Assign(board,"previewTitle",Text(terminal.transform,"Encounter Title","SELECT AN ENCOUNTER",new Vector3(-5.2f,7.5f,0),new Vector2(10.4f,1.8f),8f));
            Assign(board,"previewBody",Text(terminal.transform,"Encounter Description","Hover or select a miniature to inspect the encounter.",new Vector3(-5.2f,5.1f,0),new Vector2(10.4f,6.2f),7f));
            Assign(board,"previewState",Text(terminal.transform,"Destination State","AVAILABLE / CONFIRM TO SELECT",new Vector3(-5.2f,-1.1f,0),new Vector2(10.4f,0.9f),5.5f));
            Assign(board,"roster",Text(terminal.transform,"Squad Intent","SQUAD INTENT",new Vector3(-5.2f,-2.3f,0),new Vector2(10.4f,3.3f),5.2f));
            var countdown = Text(terminal.transform,"Countdown","SELECT NEXT ENCOUNTER",new Vector3(-5.2f,-5.8f,0),new Vector2(10.4f,1.2f),6f);
            Assign(board,"countdown",countdown);
            var countdownAnimator = countdown.gameObject.AddComponent<Animator>();
            countdownAnimator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Project/Animations/Routing/RouteCountdown.controller");
            countdownAnimator.updateMode = AnimatorUpdateMode.UnscaledTime; Assign(board,"countdownAnimator",countdownAnimator);
            Assign(board,"help",Text(terminal.transform,"Controls","D-PAD / STICK / ARROWS  Scout\nCONFIRM / CLICK  Select\nSTART / ESC  Options",new Vector3(-5.2f,-7.3f,0),new Vector2(10.4f,2f),5.5f));
            // Authored example geometry makes the scene useful to open and edit without starting a match.
            var catalogue = Resources.LoadAll<RouteEncounterDefinition>("RouteEncounters").OrderBy(x=>x.encounterId).ToArray();
            var preview = LevelRouteGraph.Generate(settings,catalogue,104729,true);
            foreach (var node in preview)
            {
                var definition = catalogue.First(x=>x.encounterId==node.encounterId);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(definition.mapModel,scene);
                model.transform.SetParent(content.transform,false);
                model.transform.localPosition = RouteMapBoard.MapPosition(settings, node);
                model.name = "Editor Preview / " + definition.displayName;
            }
            foreach (var node in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true))) node.gameObject.layer=settings.boardLayer;
            MechaRouteDepthSetup.Configure(board, table.transform, terminal.transform, settings);
            EditorSceneManager.SaveScene(scene,settings.routingScenePath);
            AssetDatabase.SetLabels(AssetDatabase.LoadMainAssetAtPath(settings.routingScenePath),new[]{"Scenes"});
        }
        finally { EditorSceneManager.CloseScene(scene,true); }
    }
    private static RouteMapNodeView NodePrefab()
    {
        const string path = Prefabs + "/WorldRouteNode.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var root = existing != null ? PrefabUtility.LoadPrefabContents(path) : new GameObject("World Route Node");
        var collider = MechaPresentationSetup.Ensure<BoxCollider>(root); collider.center = new Vector3(0,1,0); collider.size = new Vector3(4.8f,3,4.4f);
        var node = MechaPresentationSetup.Ensure<RouteMapNodeView>(root);
        var ring = Sprite(root.transform,"Availability Ring",Vector3.up*0.04f,3.6f,settings.focusSprite); ring.color=settings.reachableColor;
        var focus = Sprite(root.transform,"Selected Ring",Vector3.up*0.06f,4.3f,settings.focusSprite); focus.color=Color.white; focus.enabled=false;
        Assign(node,"ring",ring); Assign(node,"focusRing",focus);
        var label=Text(root.transform,"Encounter Name","ENCOUNTER",new Vector3(-2.8f,1.1f,-1.8f),new Vector2(5.6f,0.8f),5.2f); label.alignment=TextAlignmentOptions.Center;
        var state=Text(root.transform,"State","SCOUT AHEAD",new Vector3(-2.8f,0.65f,-2.4f),new Vector2(5.6f,0.65f),3.2f); state.alignment=TextAlignmentOptions.Center;
        var votes=Text(root.transform,"Pilot Numbers","",new Vector3(-2.8f,3.1f,0),new Vector2(5.6f,0.8f),5.2f); votes.alignment=TextAlignmentOptions.Center;
        Assign(node,"label",label);Assign(node,"state",state);Assign(node,"votes",votes);
        var pips = new UnityEngine.Object[4];
        for(int i=0;i<4;i++){var pip=Sprite(root.transform,"Pilot Vote "+i,new Vector3((i-1.5f)*0.7f,0.09f,-1.1f),2.5f,settings.pathSprite);pip.enabled=false;pips[i]=pip;}
        var data=new SerializedObject(node);var array=data.FindProperty("votePips");array.arraySize=4;for(int i=0;i<4;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=pips[i];data.ApplyModifiedPropertiesWithoutUndo();
        foreach(var transform in root.GetComponentsInChildren<Transform>())transform.gameObject.layer=settings.boardLayer;
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);
        if(existing!=null)PrefabUtility.UnloadPrefabContents(root);else UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.SetLabels(prefab,new[]{"Prefabs"});return prefab.GetComponent<RouteMapNodeView>();
    }
    private static SpriteRenderer Sprite(Transform parent,string name,Vector3 position,float scale,Sprite image)
    {
        var child=parent.Find(name);var root=child!=null?child.gameObject:new GameObject(name);root.transform.SetParent(parent,false);root.transform.localPosition=position;root.transform.localRotation=Quaternion.Euler(90,0,0);root.transform.localScale=Vector3.one*scale;
        var sprite=MechaPresentationSetup.Ensure<SpriteRenderer>(root);sprite.sprite=image;sprite.sharedMaterial=settings.pathMaterial;return sprite;
    }
    private static TMP_Text Text(Transform parent,string name,string content,Vector3 position,Vector2 size,float fontSize)
    {
        var child=parent.Find(name);var root=child!=null?child.gameObject:new GameObject(name);root.transform.SetParent(parent,false);root.transform.localPosition=position;
        var text=MechaPresentationSetup.Ensure<TextMeshPro>(root);text.font=font;text.fontSize=fontSize;text.text=content;text.color=new Color(0.9f,0.96f,1f);
        text.rectTransform.pivot=new Vector2(0,1);text.rectTransform.sizeDelta=size;text.textWrappingMode=TextWrappingModes.Normal;text.overflowMode=TextOverflowModes.Ellipsis;
        return text;
    }
    private static Material Material(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Routing/"+name+".mat");
    private static void Cube(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
    }
    private static void Assign(Component target,string field,UnityEngine.Object value)=>MechaPresentationSetup.Assign(target,field,value);
}

