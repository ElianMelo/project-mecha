using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class ConfigureLocalization
{
    public static string FontsAndTerms()
    {
        const string source = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        const string target = "Assets/_Project/Localization/Fonts/EnglishDefault.asset";
        System.IO.Directory.CreateDirectory("Assets/_Project/Localization/Fonts");
        AssetDatabase.Refresh();
        if (AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(target) == null && !AssetDatabase.CopyAsset(source, target)) throw new Exception("Could not copy the default font outside Resources.");
        var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(target);
        var fonts = LocalizationEditorSettings.GetAssetTableCollection("Fonts") ?? LocalizationEditorSettings.CreateAssetTableCollection("Fonts", "Assets/_Project/Localization/Fonts");
        fonts.AddAssetToTable(new LocaleIdentifier("en"), "Default", font);
        EditorUtility.SetDirty(fonts); EditorUtility.SetDirty(fonts.SharedData);
        foreach (var table in fonts.AssetTables) EditorUtility.SetDirty(table);
        LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, fonts);
        foreach (var category in new[]{"Combat", "Routing", "Input", "Settings"})
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(category) ?? LocalizationEditorSettings.CreateStringTableCollection(category, "Assets/_Project/Localization/Strings/" + category);
            var table = collection.StringTables.Single(t => t.LocaleIdentifier.Code == "en");
            string[] terms = category == "Combat" ? Enum.GetNames(typeof(PlayerAttack.ActionKind)).Concat(Enum.GetNames(typeof(PlayerMovement.ImpulseKind))).Concat(new[]{"Hit stun","Action","Recovery","Air","Idle","Run","Walk","Ready","NEUTRAL","STARTUP","ACTIVE","RECOVERY","HIT STOP"}).Distinct().ToArray() : category == "Routing" ? Enum.GetNames(typeof(LevelShape)).Concat(new[]{"UP","DOWN","BOTH","RIGHT","LEFT"}).ToArray() : category == "Settings" ? QualitySettings.names : new[]{"A","B","X","Y","Cross","Circle","Square","Triangle","LB","RB","LT","RT","L1","R1","L2","R2","L3","R3","Start","Select","Menu","View","Options","Share","LS","RS","Left Stick Press","Right Stick Press","Left Shoulder","Right Shoulder","Left Trigger","Right Trigger"};
            foreach (string term in terms) if (table.GetEntry("term." + term) == null) table.AddEntry("term." + term, term);
            collection.SetPreloadTableFlag(true);
            EditorUtility.SetDirty(table); EditorUtility.SetDirty(collection.SharedData); EditorUtility.SetDirty(collection);
        }
        MechaLocalizationAuthoring.Capture("Lobby", "pilot.default", "Pilot");
        MechaLocalizationAuthoring.Capture("Lobby", "pilot.number", "Pilot {0}");
        MechaLocalizationAuthoring.Capture("Input", "command.heavy", "{0} / HEAVY ATTACK");
        MechaLocalizationAuthoring.Capture("Input", "command.direction.dash", "{0} / DASH");
        MechaLocalizationAuthoring.Capture("Input", "command.up", "UP");
        MechaLocalizationAuthoring.Capture("Input", "command.down", "DOWN");
        MechaLocalizationAuthoring.Capture("Input", "command.down.prefix", "DOWN + ");
        MechaLocalizationAuthoring.Capture("Input", "command.dash", "DASH");
        MechaLocalizationAuthoring.Capture("Combat", "combo.air.prefix", "Up  ");
        var inputTable = LocalizationEditorSettings.GetStringTableCollection("Input").StringTables.Single(t => t.LocaleIdentifier.Code == "en");
        inputTable.GetEntry("keyboard.title").Value = "EDIT TEXT";
        EditorUtility.SetDirty(inputTable);
        var controls = new System.Collections.Generic.Dictionary<string,string> {
            {"buttonSouth","A"}, {"buttonNorth","Y"}, {"buttonWest","X"}, {"buttonEast","B"},
            {"leftShoulder","LB"}, {"rightShoulder","RB"}, {"leftTrigger","LT"}, {"rightTrigger","RT"},
            {"leftStickPress","LS"}, {"rightStickPress","RS"}, {"start","START"}, {"select","VIEW"} };
        foreach (var control in controls) MechaLocalizationAuthoring.Capture("Input", "gamepad." + control.Key, control.Value);
        string[] ps = {"CROSS","TRIANGLE","SQUARE","CIRCLE","L1","R1","L2","R2","L3","R3","OPTIONS","SHARE"};
        int controlIndex = 0;
        foreach (var control in controls) MechaLocalizationAuthoring.Capture("Input", "playstation." + control.Key, ps[controlIndex++]);
        var lobby = LocalizationEditorSettings.GetStringTableCollection("Lobby").StringTables.Single(t => t.LocaleIdentifier.Code == "en");
        lobby.GetEntry("request.failed").Value = "Could not complete the lobby request.";
        EditorUtility.SetDirty(lobby);
        AssetDatabase.SaveAssets();
        return "English font asset table and presentation terms configured";
    }
    public static string Settings()
    {
        System.IO.Directory.CreateDirectory("Assets/_Project/Localization/Strings");
        AssetDatabase.Refresh();
        var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/_Project/Localization/LocalizationSettings.asset");
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
        }
        var locales = LocalizationEditorSettings.GetLocales();
        if (locales.Any(l => l.Identifier.Code != "en")) throw new InvalidOperationException("Only English is authorized.");
        var english = locales.FirstOrDefault(l => l.Identifier.Code == "en");
        if (english == null)
        {
            english = Locale.CreateLocale("en");
            AssetDatabase.CreateAsset(english, "Assets/_Project/Localization/English.asset");
            LocalizationEditorSettings.AddLocale(english);
        }
        settings.GetStartupLocaleSelectors().Clear();
        settings.GetStartupLocaleSelectors().Add(new SpecificLocaleSelector { LocaleId = english.Identifier });
        LocalizationSettings.ProjectLocale = english;
        LocalizationSettings.InitializeSynchronously = true;
        EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
        return "English settings configured";
    }
}
