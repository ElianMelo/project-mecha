using System;
using System.Linq;
using FishNet.Object;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Native authoring, run through the official Pipeline. Never edits Unity YAML.
public static class BuildPvpLobby
{
    private static TMP_FontAsset font;
    private static Sprite sprite;
    private static UiFeelSettings feel;
    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before authoring.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainScene.unity") throw new InvalidOperationException("Open MainScene first.");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Localization/Fonts/EnglishDefault.asset");
        sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        feel = MechaPresentationSetup.Profile<UiFeelSettings>("UiFeel");
        Wording();
        string profilePath = "Assets/_Project/Settings/Network/PvpMatch.asset";
        var profile = AssetDatabase.LoadAssetAtPath<PvpMatchSettings>(profilePath);
        if (profile == null) { profile = ScriptableObject.CreateInstance<PvpMatchSettings>(); AssetDatabase.CreateAsset(profile, profilePath); }
        var spawner = Object.FindFirstObjectByType<EnemySpawner>();
        var match = MechaPresentationSetup.Ensure<PvpMatchDirector>(spawner.gameObject);
        Assign(match, "settings", profile); Serialize(spawner.gameObject);
        Lobby(); Options(); HealthBars(); Scoreboard();
        MechaLocalizationAuthoring.LocalizeScene(scene);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "PVP lobby, match settings, health bars and standings authored through Unity APIs.";
    }
    public static string LabelContent()
    {
        string[] paths={
            "Assets/_Project/Scripts/System/PvpMatchSettings.cs", "Assets/_Project/Scripts/System/PvpMatchDirector.cs",
            "Assets/_Project/Scripts/Combat/CombatTargetRules.cs", "Assets/_Project/Scripts/Interface/PvpScoreboard.cs",
            "Assets/_Project/Prefabs/Assemblies/PvpLobbyInterface.prefab", "Assets/_Project/Prefabs/Interface/PvpScoreboard.prefab",
            "Assets/_Project/Settings/Network/PvpMatch.asset"};
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach(string path in paths)
            {
                var asset=AssetDatabase.LoadMainAssetAtPath(path);
                string category=path.EndsWith(".cs")?"Code":path.EndsWith(".prefab")?"Prefabs":"Authored Data";
                AssetDatabase.SetLabels(asset,AssetDatabase.GetLabels(asset).Concat(new[]{category}).Distinct().ToArray());
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.SaveAssets(); return "Labeled the seven added native assets; other labels and unknown provenance retained.";
    }
    private static void Wording()
    {
        string[,] values = {
            {"pvp.title", "MECHA  /  PVP ARENA"}, {"pvp.subtitle", "CHOOSE YOUR RULES  /  READY UP  /  COMPETE"},
            {"pvp.lobby", "PVP LOBBY"}, {"pvp.free", "Free-for-All"}, {"pvp.team", "Team"},
            {"team.blue", "Blue"}, {"team.red", "Red"}, {"pvp.create", "YOUR MATCH"},
            {"team.selected", "{0} · Selected"},
            {"pvp.defaultName", "Mecha Arena"}, {"pvp.localHost", "LOCAL HOST"},
            {"pvp.rules", "5 minutes · 5 second respawn\nAssists: deal at least 20% of maximum health"},
            {"pvp.instructions", "At least 2 pilots. Team matches need Blue and Red. Every pilot must be ready."},
            {"pvp.freeClock", "Free-for-All  ·  {0}"}, {"pvp.teamClock", "Team  ·  {0}"},
            {"pvp.waiting", "Waiting for pilots…"}, {"pvp.cancelled", "Match cancelled: pilots did not connect."},
            {"pvp.teamScore", "Blue {0}  ·  Red {1}"}, {"pvp.draw", "MATCH DRAW"}, {"pvp.winner", "{0} WINS"},
            {"pvp.ranking", "Kills ↓  ·  Deaths ↑  ·  Assists ↓"}, {"pvp.finished", "Match finished · Open the menu to leave"},
            {"pvp.respawn", "Respawn in {0}s"}, {"pvp.disconnected", "{0} (left)"},
            {"score.pilot", "PILOT"}, {"score.kills", "K"}, {"score.deaths", "D"}, {"score.assists", "A"}
        };
        for (int i = 0; i < values.GetLength(0); i++) MechaLocalizationAuthoring.Capture("Lobby", values[i,0], values[i,1]);
    }
    private static void Lobby()
    {
        var coop = Object.FindObjectsByType<MechaLobbyPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => !x.IsPvp);
        var existing = Object.FindObjectsByType<MechaLobbyPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x.IsPvp);
        var root = existing != null ? existing.gameObject : Object.Instantiate(coop.gameObject);
        root.name = "Mecha PVP Lobby UI";
        if (existing == null && PrefabUtility.IsPartOfPrefabInstance(root)) PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        var ui = root.GetComponent<MechaLobbyPanel>();
        var data = new SerializedObject(ui); data.FindProperty("pvp").boolValue = true; data.ApplyModifiedPropertiesWithoutUndo();
        Assign(ui, "service", Object.FindFirstObjectByType<MechaLobbyService>());
        var panel = root.transform.Find("Lobby Panel");
        Bind(panel.Find("Title").GetComponent<TMP_Text>(), "pvp.title");
        Bind(panel.Find("Subtitle").GetComponent<TMP_Text>(), "pvp.subtitle");
        var create = panel.Find("Browser/Create Card");
        Bind(create.Find("Create Title").GetComponent<TMP_Text>(), "pvp.create");
        Place(create.Find("Create"), 24, 352, 488, 44);
        Place(create.Find("Local Host"), 24, 408, 236, 36); Place(create.Find("Local Join"), 276, 408, 236, 36);
        Bind(create.Find("Local Host").GetComponentInChildren<TMP_Text>(true), "pvp.localHost");
        Place(create.Find("Help"), 24, 454, 488, 42); Bind(create.Find("Help").GetComponent<TMP_Text>(), "pvp.rules");
        foreach (var hint in create.Find("Help").GetComponents<ControlNavigationHint>()) Object.DestroyImmediate(hint);
        create.Find("Help").GetComponent<TMP_Text>().fontSize = 15;
        var mode = Button(create, "PVP Mode", "pvp.free", 24, 256, 488, 40);
        var blue = Button(create, "Blue Team", "team.blue", 24, 304, 236, 36);
        var red = Button(create, "Red Team", "team.red", 276, 304, 236, 36);
        var room = panel.Find("Waiting Room");
        var roomMode = Button(room, "PVP Mode", "pvp.free", 40, 372, 280, 40);
        Place(room.Find("Members"), 342, 370, 818, 44); room.Find("Members").GetComponent<TMP_Text>().fontSize = 16;
        var roomBlue = Button(room, "Blue Team", "team.blue", 40, 426, 350, 50);
        var roomRed = Button(room, "Red Team", "team.red", 410, 426, 350, 50);
        Assign(ui, "mode", mode); Assign(ui, "blueTeam", blue); Assign(ui, "redTeam", red);
        Assign(ui, "roomMode", roomMode); Assign(ui, "roomBlueTeam", roomBlue); Assign(ui, "roomRedTeam", roomRed);
        var labelData = new SerializedObject(ui); var labelArray = labelData.FindProperty("ruleLabels"); labelArray.arraySize = 6;
        int labelIndex = 0;
        // Dynamic mode/selection labels use GameText at runtime; remove static string setters.
        foreach (var button in new[] {mode,blue,red,roomMode,roomBlue,roomRed})
        {
            var label = button.GetComponentInChildren<TMP_Text>(true);
            foreach (var binding in label.GetComponents<UnityEngine.Localization.Components.LocalizeStringEvent>()) Object.DestroyImmediate(binding);
            labelArray.GetArrayElementAtIndex(labelIndex++).objectReferenceValue = label;
        }
        labelData.ApplyModifiedPropertiesWithoutUndo();
        MechaUiFeelSetup.ApplyLobby(ui);
        panel.gameObject.SetActive(false);
        SaveAssembly(root, "Assets/_Project/Prefabs/Assemblies/PvpLobbyInterface.prefab");
    }
    private static void Options()
    {
        const string path = "Assets/_Project/Prefabs/Interface/GameOptions.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(path);
        try { ConfigureOptions(prefab.GetComponent<GameOptionsMenu>(), false); PrefabUtility.SaveAsPrefabAsset(prefab, path); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        var sceneMenu = Object.FindFirstObjectByType<GameOptionsMenu>();
        ConfigureOptions(sceneMenu, true);
        Assign(sceneMenu, "pvpPanel", Object.FindObjectsByType<MechaLobbyPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => x.IsPvp));
        PrefabUtility.RecordPrefabInstancePropertyModifications(sceneMenu); EditorUtility.SetDirty(sceneMenu);
    }
    private static void ConfigureOptions(GameOptionsMenu menu, bool inScene)
    {
        var data = new SerializedObject(menu);
        var lobby = (Button)data.FindProperty("lobby").objectReferenceValue;
        var button = lobby.transform.parent.Find("PVP Lobby")?.GetComponent<Button>();
        if (button == null) { button = Object.Instantiate(lobby, lobby.transform.parent); button.name = "PVP Lobby"; }
        foreach (var duplicate in lobby.transform.parent.GetComponentsInChildren<Button>(true).Where(b => b.name == "PVP Lobby" && b != button).ToArray()) Object.DestroyImmediate(duplicate.gameObject);
        Bind(button.GetComponentInChildren<TMP_Text>(true), "pvp.lobby");
        var card = lobby.transform.parent;
        var controls = (Button)data.FindProperty("controlsButton").objectReferenceValue;
        var buttons = new[] { (Button)data.FindProperty("resume").objectReferenceValue, lobby, button,
            (Button)data.FindProperty("audioButton").objectReferenceValue, (Button)data.FindProperty("videoButton").objectReferenceValue,
            (Button)data.FindProperty("generalButton").objectReferenceValue, controls };
        for (int i = 0; i < buttons.Length; i++)
        {
            Place(buttons[i].transform, 40, 122 + i * 62, 500, 52);
            buttons[i].navigation = new Navigation {mode = Navigation.Mode.Explicit, selectOnUp = buttons[(i + buttons.Length - 1) % buttons.Length], selectOnDown = buttons[(i + 1) % buttons.Length]};
        }
        if (card.Find("Help") != null) Place(card.Find("Help"), 40, 566, 500, 48);
        Assign(menu, "pvpButton", button);
        MechaUiFeelSetup.Button(button, feel);
        if (!inScene) Assign(menu, "pvpPanel", null);
    }
    private static void HealthBars()
    {
        string path = "Assets/_Project/Prefabs/MechaPlayer.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var hud = root.GetComponentInChildren<ActorWorldHud>(true);
            var style = (SquadPresentationSettings)new SerializedObject(hud).FindProperty("settings").objectReferenceValue;
            var canvas = hud.GetComponentInChildren<Canvas>(true).transform;
            var rect = Rect(canvas, "PVP Health", Vector2.one * 0.5f, Vector2.zero, style.healthBarSize * 100f);
            rect.pivot = Vector2.one * 0.5f;
            var border = MechaPresentationSetup.Ensure<Image>(rect.gameObject);
            border.sprite = sprite; border.color = Color.black; border.type = Image.Type.Sliced; border.fillCenter = false; border.raycastTarget = false;
            var fillRect = Rect(rect, "Health Fill", Vector2.one * 0.5f, Vector2.zero, rect.sizeDelta - new Vector2(4,4));
            fillRect.pivot = Vector2.one * 0.5f;
            var fill = MechaPresentationSetup.Ensure<Image>(fillRect.gameObject);
            fill.sprite = sprite; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.color = style.enemyHealth; fill.raycastTarget = false;
            Assign(hud, "bar", rect); Assign(hud, "fill", fill);
            Serialize(root); PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void Scoreboard()
    {
        var chat = Object.FindFirstObjectByType<MechaChat>();
        var chatData = new SerializedObject(chat);
        var layout = (ChatSettings)chatData.FindProperty("settings").objectReferenceValue;
        var root = GameObject.Find("PVP Standings") ?? new GameObject("PVP Standings", typeof(RectTransform));
        var canvas = MechaPresentationSetup.Ensure<Canvas>(root); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 90;
        var scaler = MechaPresentationSetup.Ensure<CanvasScaler>(root);
        var chatScaler = chat.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = chatScaler.uiScaleMode; scaler.referenceResolution = chatScaler.referenceResolution; scaler.matchWidthOrHeight = chatScaler.matchWidthOrHeight;
        var presenter = MechaPresentationSetup.Ensure<PvpScoreboard>(root);
        var panel = Rect(root.transform, "Standings", layout.anchor, layout.offset + Vector2.down * (layout.size.y + 12), new Vector2(layout.size.x, 280));
        panel.pivot = Vector2.one;
        var background = MechaPresentationSetup.Ensure<Image>(panel.gameObject); background.sprite = sprite; background.type = Image.Type.Sliced; background.color = new Color(0.025f,0.045f,0.075f,0.94f); background.raycastTarget = false;
        Assign(presenter,"panel",panel.gameObject);
        Assign(presenter,"clock",Label(panel,"Clock",null,10,8,layout.size.x-20,24,17));
        Assign(presenter,"summary",Label(panel,"Summary",null,10,36,layout.size.x-20,24,13));
        Label(panel,"Pilot Header","score.pilot",34,65,160,22,12);
        Label(panel,"Kills Header","score.kills",204,65,28,22,12);
        Label(panel,"Deaths Header","score.deaths",238,65,28,22,12);
        Label(panel,"Assists Header","score.assists",272,65,28,22,12);
        var data = new SerializedObject(presenter); var rows = data.FindProperty("rows"); rows.arraySize = 8;
        for (int i=0;i<8;i++)
        {
            var row = Rect(panel,"Pilot " + i, new Vector2(0,1),new Vector2(0,-89-i*20),new Vector2(layout.size.x,20));
            var entry = rows.GetArrayElementAtIndex(i); entry.FindPropertyRelative("root").objectReferenceValue = row.gameObject;
            entry.FindPropertyRelative("rank").objectReferenceValue = Label(row,"Rank",null,10,0,22,20,13);
            entry.FindPropertyRelative("pilot").objectReferenceValue = Label(row,"Pilot",null,34,0,164,20,13);
            entry.FindPropertyRelative("kills").objectReferenceValue = Label(row,"Kills",null,204,0,28,20,13);
            entry.FindPropertyRelative("deaths").objectReferenceValue = Label(row,"Deaths",null,238,0,28,20,13);
            entry.FindPropertyRelative("assists").objectReferenceValue = Label(row,"Assists",null,272,0,28,20,13);
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        Assign(presenter,"respawn",Label(panel,"Respawn",null,10,252,layout.size.x-20,22,13));
        panel.gameObject.SetActive(false);
        SaveAssembly(root,"Assets/_Project/Prefabs/Interface/PvpScoreboard.prefab");
    }
    private static Button Button(Transform parent,string name,string key,float x,float y,float w,float h)
    {
        var rect=Rect(parent,name,new Vector2(0,1),new Vector2(x,-y),new Vector2(w,h));
        var image=MechaPresentationSetup.Ensure<Image>(rect.gameObject); image.sprite=sprite; image.type=Image.Type.Sliced; image.color=new Color(0.12f,0.2f,0.3f);
        var button=MechaPresentationSetup.Ensure<Button>(rect.gameObject); button.targetGraphic=image;
        var text=Label(rect,"Label",key,8,0,w-16,h,17); text.alignment=TextAlignmentOptions.Center;
        MechaUiFeelSetup.Button(button,feel); return button;
    }
    private static TMP_Text Label(Transform parent,string name,string key,float x,float y,float w,float h,float size)
    {
        var rect=Rect(parent,name,new Vector2(0,1),new Vector2(x,-y),new Vector2(w,h));
        var text=MechaPresentationSetup.Ensure<TextMeshProUGUI>(rect.gameObject); text.font=font; text.fontSize=size; text.richText=false; text.raycastTarget=false; text.color=Color.white;
        text.textWrappingMode=TextWrappingModes.NoWrap; text.overflowMode=TextOverflowModes.Ellipsis;
        if(key!=null) Bind(text,key); else { text.text=""; Font(text); }
        return text;
    }
    private static void Bind(TMP_Text text,string key)
    {
        foreach(var old in text.GetComponents<UnityEngine.Localization.Components.LocalizeStringEvent>()) Object.DestroyImmediate(old);
        var binding=text.gameObject.AddComponent<UnityEngine.Localization.Components.LocalizeStringEvent>();
        binding.StringReference=new LocalizedString("Lobby",key);
        var setter=(UnityEngine.Events.UnityAction<string>)Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction<string>),text,"set_text");
        UnityEditor.Events.UnityEventTools.AddPersistentListener(binding.OnUpdateString,setter);
        binding.OnUpdateString.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
        binding.RefreshString(); Font(text); EditorUtility.SetDirty(binding);
    }
    private static void Font(TMP_Text text)
    {
        var fontBinding=MechaPresentationSetup.Ensure<LocalizeTmpFontEvent>(text.gameObject);
        fontBinding.AssetReference=new LocalizedTmpFont{TableReference="Fonts",TableEntryReference="Default"};
        if(fontBinding.OnUpdateAsset.GetPersistentEventCount()==0)
        {
            var setter=(UnityEngine.Events.UnityAction<TMP_FontAsset>)Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction<TMP_FontAsset>),text,"set_font");
            UnityEditor.Events.UnityEventTools.AddPersistentListener(fontBinding.OnUpdateAsset,setter);
            fontBinding.OnUpdateAsset.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
        }
        EditorUtility.SetDirty(fontBinding);
    }
    private static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size)
    {
        var child=parent.Find(name); var rect=child!=null?(RectTransform)child:new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent,false); rect.anchorMin=rect.anchorMax=anchor; rect.pivot=new Vector2(0,1); rect.anchoredPosition=position; rect.sizeDelta=size; return rect;
    }
    private static void Place(Transform target,float x,float y,float w,float h)
    {
        var rect=(RectTransform)target; rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(w,h);
    }
    private static void Assign(Component target,string field,Object value) => MechaPresentationSetup.Assign(target,field,value);
    private static void Serialize(GameObject root)
    {
        var method=typeof(NetworkObject).GetMethod("ReserializeEditorSetValues",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        foreach(var item in root.GetComponentsInChildren<NetworkObject>(true)) { method.Invoke(item,new object[]{true,true}); EditorUtility.SetDirty(item); }
    }
    private static void SaveAssembly(GameObject root,string path)
    {
        if(PrefabUtility.IsPartOfPrefabInstance(root)) PrefabUtility.ApplyPrefabInstance(root,InteractionMode.AutomatedAction);
        else PrefabUtility.SaveAsPrefabAssetAndConnect(root,path,InteractionMode.AutomatedAction);
    }
}
