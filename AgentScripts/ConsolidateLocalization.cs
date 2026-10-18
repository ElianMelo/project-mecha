using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Tables;

// One-time content migration through Unity serialization; no test runner or play-session driver.
public static class ConsolidateLocalization
{
    private sealed class Entry
    {
        public StringTableCollection collection; public StringTable table; public long id; public string key, value, tableName, tablePath, sharedPath;
        public string Table => tableName;
    }
    public static object Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var entries = Entries(); var replacements = new Dictionary<string,Entry>(); var removed = new List<Entry>();
        foreach(var group in entries.GroupBy(e=>e.Table+"\n"+e.value))
        {
            var copies=group.Where(e=>e.key.EndsWith(".scene")||e.key.EndsWith(".prefab")).ToArray();
            if(copies.Length<2)continue;
            // Only merge copies of the same presentation site, never coincidentally equal domain terms.
            foreach(var site in copies.GroupBy(e=>Regex.Replace(e.key,@"\.(scene|prefab)$","")))
            {
                var ordered=site.OrderBy(e=>e.key.EndsWith(".scene")?0:1).ToArray();if(ordered.Length<2)continue;
                var canonical=ordered[0];
                foreach(var copy in ordered.Skip(1)){replacements[copy.Table+"/"+copy.id]=canonical;removed.Add(copy);}
            }
        }
        // Shared navigation words describe the same operation across settings screens.
        foreach(var shared in new[]{new[]{"BACK","Settings","navigation.back"},new[]{"DEFAULTS","Settings","navigation.defaults"},new[]{"CANCEL","Input","binding.cancel"},new[]{"RESET","Input","binding.reset"},new[]{"GENERAL","Settings","options.options.general"}})
        {
            MechaLocalizationAuthoring.Capture(shared[1],shared[2],shared[0]);
            var canonical=Entries().First(e=>e.Table==shared[1]&&e.key==shared[2]);
            foreach(var copy in entries.Where(e=>e.value==shared[0]&&!(e.Table==canonical.Table&&e.id==canonical.id)))
            {replacements[copy.Table+"/"+copy.id]=canonical;if(!removed.Contains(copy))removed.Add(copy);}
        }
        foreach(var shared in new[]{
            new[]{"Pilot","Lobby","default.pilot"},new[]{"Mecha Expedition","Lobby","default.lobby"},
            new[]{"MEGA SLASH","Combat","skill.megaSlash"},new[]{"SWORD FIRE","Combat","skill.swordFire"},new[]{"CRITICAL X","Combat","skill.criticalX"},
            new[]{"Dash","Combat","term.Dash"},new[]{"Rocket","Combat","term.Rocket"},new[]{"Mushidon","Combat","term.Mushidon"},
            new[]{"AVAILABLE / CONFIRM TO SELECT","Routing","destination.available"},new[]{"SQUAD INTENT","Routing","squad.heading"},new[]{"SELECT NEXT ENCOUNTER","Routing","encounter.select"},
            new[]{"abc","Input","keyboard.lowercase"},new[]{"CLEARED","Routing","destination.cleared"},new[]{"Clear the encounter to continue.","Routing","encounter.reward"},new[]{"OFF","Settings","option.off"},
            new[]{"Down","Input","direction.down"},new[]{"Left stick","Input","control.leftStick"},new[]{"Right stick","Input","control.rightStick"},new[]{"D-pad","Input","control.dpad"}})
        {
            MechaLocalizationAuthoring.Capture(shared[1],shared[2],shared[0]);var canonical=Entries().First(e=>e.Table==shared[1]&&e.key==shared[2]);
            foreach(var copy in entries.Where(e=>e.value==shared[0]&&!(e.Table==canonical.Table&&e.id==canonical.id)))
            {replacements[copy.Table+"/"+copy.id]=canonical;if(!removed.Contains(copy))removed.Add(copy);}
        }
        // Resolve any merge whose original destination itself became a shared navigation entry.
        foreach(var operation in new[]{"previous","next","up","down","remove"})
        {
            var copies=entries.Where(e=>e.Table=="Combat"&&Regex.IsMatch(e.key,@"^step\.\d+\."+operation+"$")).ToArray();
            if(copies.Length<2)continue;
            MechaLocalizationAuthoring.Capture("Combat","step."+operation,copies[0].value);var canonical=Entries().First(e=>e.Table=="Combat"&&e.key=="step."+operation);
            foreach(var copy in copies){replacements[copy.Table+"/"+copy.id]=canonical;if(!removed.Contains(copy))removed.Add(copy);}
        }
        foreach(var id in replacements.Keys.ToArray())
            while(replacements.TryGetValue(replacements[id].Table+"/"+replacements[id].id,out var next)) replacements[id]=next;
        int rewired=0; var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach(string path in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);int count=scene.GetRootGameObjects().Sum(root=>RewriteRoot(root,replacements));
                if(count>0){rewired+=count;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
            }
            foreach(string path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/_Project"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var root=PrefabUtility.LoadPrefabContents(path);
                try{int count=RewriteRoot(root,replacements);if(count>0){rewired+=count;PrefabUtility.SaveAsPrefabAsset(root,path);}}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            foreach(string path in AssetDatabase.FindAssets("t:ScriptableObject",new[]{"Assets/_Project/Settings"}).Select(AssetDatabase.GUIDToAssetPath))
                rewired+=Rewrite(AssetDatabase.LoadMainAssetAtPath(path),replacements);
            // Runtime lookup literals must follow the same canonical keys as native references.
            int literals=0;
            foreach(string path in Directory.GetFiles("Assets/_Project/Scripts","*.cs",SearchOption.AllDirectories))
            {
                string source=File.ReadAllText(path),updated=source;
                foreach(var copy in removed)
                {
                    var target=replacements[copy.Table+"/"+copy.id];
                    string pattern="(GameText\\.Get\\(\\s*)\""+Regex.Escape(copy.Table)+"\"\\s*,\\s*\""+Regex.Escape(copy.key)+"\"";
                    updated=Regex.Replace(updated,pattern,m=>{literals++;return m.Groups[1].Value+"\""+target.Table+"\", \""+target.key+"\"";});
                }
                if(updated!=source)File.WriteAllText(path,updated);
            }
            foreach(var copy in removed)
            {
                var table=AssetDatabase.LoadAssetAtPath<StringTable>(copy.tablePath);var shared=AssetDatabase.LoadAssetAtPath<SharedTableData>(copy.sharedPath);
                table.RemoveEntry(copy.id);shared.RemoveKey(copy.id);EditorUtility.SetDirty(table);EditorUtility.SetDirty(shared);
            }
            AssetDatabase.SaveAssets();
            var previous=File.Exists("Docs/LocalizationConsolidation.csv")?File.ReadAllLines("Docs/LocalizationConsolidation.csv").Skip(1):Enumerable.Empty<string>();
            File.WriteAllLines("Docs/LocalizationConsolidation.csv",new[]{"PreviousTable,PreviousKey,CanonicalTable,CanonicalKey"}.Concat(previous).Concat(removed.OrderBy(e=>e.Table).ThenBy(e=>e.key).Select(e=>{var t=replacements[e.Table+"/"+e.id];return e.Table+","+e.key+","+t.Table+","+t.key;})).Distinct());
            return new{removed=removed.Count,rewired,literals};
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
    private static List<Entry> Entries()=>LocalizationEditorSettings.GetStringTableCollections().SelectMany(c=>c.StringTables.Where(t=>t.LocaleIdentifier.Code=="en").SelectMany(t=>t.Values.Select(e=>new Entry{collection=c,table=t,id=e.KeyId,key=e.Key,value=e.LocalizedValue,tableName=c.TableCollectionName,tablePath=AssetDatabase.GetAssetPath(t),sharedPath=AssetDatabase.GetAssetPath(c.SharedData)}))).ToList();
    private static int RewriteRoot(GameObject root,Dictionary<string,Entry> replacements)=>root.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c!=null).Sum(c=>Rewrite(c,replacements));
    private static int Rewrite(UnityEngine.Object target,Dictionary<string,Entry> replacements)
    {
        var serialized=new SerializedObject(target);var iterator=serialized.GetIterator();int count=0;
        while(iterator.Next(true))
        {
            if(iterator.propertyType!=SerializedPropertyType.Generic)continue;
            var table=iterator.FindPropertyRelative("m_TableReference.m_TableCollectionName");
            var key=iterator.FindPropertyRelative("m_TableEntryReference.m_KeyId");
            var keyName=iterator.FindPropertyRelative("m_TableEntryReference.m_Key");
            if(table==null||key==null)continue;
            string tableName=table.stringValue;
            if(tableName.StartsWith("GUID:")){var collection=LocalizationEditorSettings.GetStringTableCollections().FirstOrDefault(c=>"GUID:"+c.SharedData.TableCollectionNameGuid.ToString("N")==tableName);if(collection!=null)tableName=collection.TableCollectionName;}
            if(!replacements.TryGetValue(tableName+"/"+key.longValue,out var destination))continue;
            table.stringValue=destination.Table;key.longValue=destination.id;if(keyName!=null)keyName.stringValue="";count++;
        }
        if(count>0){serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(target);}return count;
    }
    public static object AuditDuplicates()
    {
        var groups=Entries().GroupBy(e=>e.value.Trim()).Where(g=>g.Count()>1).ToArray();
        File.WriteAllLines("Docs/LocalizationDuplicateReview.csv",new[]{"Value,Entries,Decision"}.Concat(groups.Select(g=>"\""+g.Key.Replace("\"","\"\"").Replace("\n"," / ").Replace("\r","")+"\",\""+string.Join("; ",g.Select(e=>e.Table+"/"+e.key))+"\",\""+Decision(g.Key)+"\"")));
        return new{groups=groups.Length, entries=Entries().Count, copiedSites=groups.Sum(g=>g.Count(e=>e.key.EndsWith(".scene")||e.key.EndsWith(".prefab")))};
    }
    private static string Decision(string value)
    {
        if(value=="READY")return "Keep: skill availability; squad member state; ready command are different meanings";
        if(value=="GROUND")return "Keep: combo-domain tab and grounded telemetry are different meanings";
        if(value=="Up")return "Keep: aerial combo prefix and binding-direction suffix have different grammar";
        if(value=="UP"||value=="DOWN")return "Keep: combat command notation and route direction identifiers are separate contracts";
        if(value=="<"||value==">")return "Keep: preset paging and step ordering have different control roles; existing symbol artwork is outside this change";
        return "Keep: printable keyboard character and controller-family or diagnostic token have distinct device identities; no translatable prose is duplicated";
    }
}
