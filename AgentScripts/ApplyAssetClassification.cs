using System.Linq;
using UnityEditor;

// Compatibility entry points. Native Unity labels are the classification record.
public static class ApplyAssetClassification
{
    public static string Apply()=>MechaAssetLabels.ApplyPrefix("Assets/_Project");
    public static string ApplyPathPrefix(string prefix)=>MechaAssetLabels.ApplyPrefix(prefix);
    public static string ApplyMelee()=>string.Join("\n",new[]{"Assets/_Project/VFX/Melee","Assets/_Project/VFX/DamageSparks","Assets/_Project/Animations/Humanoid","Assets/_Project/Prefabs/Weapons"}.Select(MechaAssetLabels.ApplyPrefix));
    public static string ApplyFireSkills()=>MechaAssetLabels.ApplyPrefix("Assets/_Project/VFX/FireSkills");
    public static string ApplyVfxLibraries(int offset,int count)
    {
        var paths=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/Hovl Studio/")||p.StartsWith("Assets/JMO Assets/")||p.StartsWith("Assets/PixPlays/")||p.StartsWith("Assets/GabrielAguiarProductions/")).ToArray();
        return MechaAssetLabels.Apply(paths,offset,count);
    }
}
