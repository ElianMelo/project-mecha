using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Localization;

// Adding a language is a user-approved project decision, never an automatic operation.
public sealed class MechaLocalizationBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;
    public void OnPreprocessBuild(BuildReport report)
    {
        var locales = LocalizationEditorSettings.GetLocales();
        if (locales.Count != 1 || locales[0].Identifier.Code != "en")
            throw new BuildFailedException("Only English is approved. Adding another language requires the user's explicit request and direct approval, then an update to the approved locale policy.");
        foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
            foreach (var table in collection.StringTables)
                foreach (var key in collection.SharedData.Entries)
                    if (string.IsNullOrWhiteSpace(table.GetEntry(key.Id)?.LocalizedValue))
                        throw new BuildFailedException("Missing localization: " + collection.TableCollectionName + "/" + key.Key);
        var fonts = LocalizationEditorSettings.GetAssetTableCollection("Fonts");
        if (fonts == null || !fonts.AssetTables.Any(t => t.LocaleIdentifier.Code == "en" && !string.IsNullOrEmpty(t.GetEntry("Default")?.Guid)))
            throw new BuildFailedException("English default font localization is missing.");
    }
}
