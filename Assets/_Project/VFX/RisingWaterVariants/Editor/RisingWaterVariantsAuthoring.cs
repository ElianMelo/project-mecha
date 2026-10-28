using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

// Third rising-water pass: cel water shader on authored mesh particles, one shared
// warmup -> action -> recovery timeline locked to the S pulses, and ten different constructions.
public static class RisingWaterVariantsAuthoring
{
    public const string Root = "Assets/_Project/VFX/RisingWaterVariants";
    public static readonly string[] Keys = { "sheets", "geysers", "crests", "fins", "ribbons", "fans", "curtains", "bands", "branches", "petals" };
    public static readonly string[] Titles = { "Geyser pillar", "Pillar ring", "Twin waves", "Vortex spiral", "Spiral ribbons", "Splash crown", "Ring wall", "Tiered fountain", "Jellyfish dome", "Lotus bloom" };
    private static readonly string[] Descriptions = {
        "A layered geyser erupts from a gathering pool, wearing a foam skirt and bursting crests.",
        "Four pillars erupt around the player, one on each pulse, and collapse together.",
        "Two curling waves rise on either side and tip over the player before breaking apart.",
        "Twisted water sheets spin upward around the player and fling tangential spray.",
        "Foam-headed ribbons spiral upward around a thin core; each pulse launches a new set.",
        "A jagged splash crown erupts and re-bursts on each pulse with a droplet fountain.",
        "A hollow water wall rises around the player; foam bands climb it on each pulse.",
        "A central jet stacks a new overflowing tier on each pulse, dripping into the pool.",
        "A jet mushrooms into a pulsing dome that trails dripping tendrils.",
        "Six curled petals rise and open around the player, with a foam heart on each pulse." };

    // Shared action contract (Zero S RisingFountainAction): impact 0.28, 4 pulses every 0.22, 1.25 action, 0.35 tail.
    private const float Impact = .28f, Interval = .22f, ActionEnd = 1.25f, Total = 1.6f, RiseTime = .09f;
    private static float PulseAt(int k) => Impact + k * Interval;

    private static Transform root;
    private static List<ParticleSystem> startup;
    private static List<(ParticleSystem system, int count, int mask)> pulses;
    private static List<ParticleSystemRenderer> renderers;
    private static int candidate, pieceCount;
    private static Material mBody, mSheath, mFoam, mGround, mRipple, mDrop, mTrail, mSheet, mSheetDeep, mCrown, mWall;
    private static Mesh column, skirt, disc, ring, curl, helix, helixInner, petal, dome, wall;
    private static readonly HashSet<string> produced = new HashSet<string>();

    private static void RemoveStale(params string[] folders)
    {
        foreach (string guid in AssetDatabase.FindAssets("", folders.Select(f => Root + "/" + f).ToArray()))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!AssetDatabase.IsValidFolder(path) && !produced.Contains(path)) AssetDatabase.DeleteAsset(path);
        }
    }

    [MenuItem("Tools/Mecha/VFX/Author rising water candidates")]
    public static void BuildMenu() => BuildAll();
    public static string BuildAll() => Build(-1);
    public static string BuildCandidate(int index)
    {
        if (index < 0 || index >= Keys.Length) throw new ArgumentOutOfRangeException(nameof(index));
        return Build(index);
    }

    private static string Build(int only)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Author in stopped Edit Mode.");
        foreach (string folder in new[] { "Profiles", "Prefabs", "Materials", "Animation", "Previews", "Meshes" }) Folder(Root + "/" + folder);
        var action = AssetDatabase.LoadAssetAtPath<AttackPiece>("Assets/_Project/VFX/ZeroWater/Profiles/RisingFountainAction.asset");
        if (action == null) throw new InvalidOperationException("Zero S action unavailable.");
        // Generated assets are updated in place so their GUIDs (and prefab references) survive repeated builds.
        produced.Clear(); BuildMeshes(); BuildMaterials();
        if (only < 0) RemoveStale("Materials", "Meshes");
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            for (int i = 0; i < 10; i++) if (only < 0 || only == i) BuildOne(i, action);
            foreach (string guid in AssetDatabase.FindAssets("", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null || AssetDatabase.IsValidFolder(path)) continue;
                string category = asset is MonoScript ? path.Contains("/Editor/") ? "Others" : "Code" : asset is GameObject ? "Prefabs" :
                    asset is ScriptableObject || asset is AnimationClip || asset is RuntimeAnimatorController ? "Authored Data" : "Others";
                var labels = AssetDatabase.GetLabels(asset).Concat(new[] { category, "VFXCreator" }).ToList();
                if (!labels.Contains("Keep") && !labels.Contains("Replace")) labels.Add("Replace");
                AssetDatabase.SetLabels(asset, labels.Distinct().ToArray());
            }
            AssetDatabase.SaveAssets();
            // Reload saved prefabs so the editor never serves a stale in-memory copy to Play Mode.
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root + "/Prefabs", Root + "/Previews" }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            return "Rebuilt " + (only < 0 ? "10 candidates" : Titles[only]) + " on the unchanged S action.";
        }
        finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
    }

    private static void BuildOne(int index, AttackPiece action)
    {
        candidate = index; pieceCount = 0;
        string id = (index + 1).ToString("00") + "-" + Keys[index];
        var profile = Asset<RisingWaterVariantSettings>(Root + "/Profiles/" + id + ".asset");
        profile.previewAction = action; profile.tailSeconds = Total - ActionEnd; profile.interruptedTail = 0.24f; EditorUtility.SetDirty(profile);
        root = new GameObject("Rising water " + id).transform; root.gameObject.layer = LayerMask.NameToLayer("TransparentFX");
        startup = new List<ParticleSystem>(); pulses = new List<(ParticleSystem, int, int)>(); renderers = new List<ParticleSystemRenderer>();
        try
        {
            var effect = root.gameObject.AddComponent<RisingWaterVariantEffect>();
            var animator = root.gameObject.AddComponent<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Compose(index);
            animator.runtimeAnimatorController = Controller(id);
            var data = new SerializedObject(effect);
            var start = data.FindProperty("startupSystems"); start.arraySize = startup.Count;
            for (int i = 0; i < startup.Count; i++) start.GetArrayElementAtIndex(i).objectReferenceValue = startup[i];
            var emitters = data.FindProperty("pulseEmitters"); emitters.arraySize = pulses.Count;
            for (int i = 0; i < pulses.Count; i++)
            {
                var e = emitters.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("system").objectReferenceValue = pulses[i].system;
                e.FindPropertyRelative("count").intValue = pulses[i].count;
                e.FindPropertyRelative("pulseMask").intValue = pulses[i].mask;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, Root + "/Prefabs/" + id + ".prefab");
            profile.prefab = prefab.GetComponent<RisingWaterVariantEffect>(); EditorUtility.SetDirty(profile);
            var wrapper = new GameObject("Preview " + id);
            try
            {
                var playback = wrapper.AddComponent<RisingWaterVariantPreview>();
                var serialized = new SerializedObject(playback); serialized.FindProperty("settings").objectReferenceValue = profile; serialized.ApplyModifiedPropertiesWithoutUndo();
                string previewPath = Root + "/Previews/" + id + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(wrapper, previewPath);
                string name = (index + 1).ToString("00") + " - " + Titles[index];
                string definitionPath = VfxTestingAuthoring.Register(previewPath, "fountain." + Keys[index], name, Descriptions[index], Total, 0.1f);
                // Capture preserves existing wording; this explicit revision updates this family's English entries.
                var table = LocalizationEditorSettings.GetStringTableCollection("Diagnostics").GetTable("en") as StringTable;
                table.GetEntry("fountain." + Keys[index]).Value = name;
                table.GetEntry("fountain." + Keys[index] + ".detail").Value = Descriptions[index];
                EditorUtility.SetDirty(table);
                var definition = AssetDatabase.LoadAssetAtPath<VfxPreviewDefinition>(definitionPath);
                definition.prefabControlsPlayback = true; definition.position = Vector3.down; EditorUtility.SetDirty(definition);
            }
            finally { UnityEngine.Object.DestroyImmediate(wrapper); }
        }
        finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }
    }

    // ------------------------------------------------------------------ compositions

    private static void Compose(int index)
    {
        switch (index)
        {
            case 0: // Geyser pillar: deep sheath + caustic body + foam skirt, crest caps on pulses.
            {
                Warmup(1.5f, true);
                Column("Deep sheath", column, mSheath, Vector3.zero, new Vector3(.8f, 3.15f, .8f), .19f, 1.1f, 40);
                Column("Geyser body", column, mBody, Vector3.zero, new Vector3(.6f, 3.05f, .6f), .2f, 1.12f, 0);
                Skirt("Foam skirt", Vector3.zero, new Vector3(1.15f, .8f, 1.15f), .24f, mFoam);
                Crest("Crest cap", new Vector3(0, 2.7f, 0), .72f, 15);
                PulseSpray("Crest spray", new Vector3(0, 2.85f, 0), 32, new Vector2(4.2f, 6.5f), 10, 15);
                Drips("Falling drips", new Vector3(0, 2.6f, 0), .5f);
                Recovery(1.2f);
                break;
            }
            case 1: // Pillar ring: one pillar per pulse around the player.
            {
                Warmup(1.9f, false);
                var spots = new[] { new Vector3(-1.05f, 0, .55f), new Vector3(1.05f, 0, -.45f), new Vector3(1.0f, 0, .6f), new Vector3(-1.0f, 0, -.5f) };
                var heights = new[] { 2.5f, 2.2f, 2.8f, 2.35f };
                for (int k = 0; k < 4; k++)
                {
                    float b = PulseAt(k); int mask = 1 << k;
                    Column("Pillar sheath " + k, column, mSheath, spots[k], new Vector3(.44f, heights[k] * 1.04f, .44f), b, 1.12f, 40, mask);
                    Column("Pillar body " + k, column, mBody, spots[k], new Vector3(.33f, heights[k], .33f), b, 1.15f, 0, mask);
                    Skirt("Pillar skirt " + k, spots[k], new Vector3(.7f, .55f, .7f), b + .02f, mFoam, mask);
                    Crest("Pillar crest " + k, spots[k] + Vector3.up * (heights[k] - .3f), .45f, mask);
                    PulseSpray("Pillar spray " + k, spots[k] + Vector3.up * (heights[k] - .1f), 30, new Vector2(3.5f, 5.5f), 12, mask);
                    Ripple("Pillar ripple " + k, spots[k], .75f, mask);
                    Drips("Pillar drips " + k, spots[k] + Vector3.up * (heights[k] - .3f), .3f, 18);
                }
                Recovery(1.4f);
                break;
            }
            case 2: // Twin waves: curls on both sides tip over the player, then crash.
            {
                Warmup(1.7f, true);
                for (int side = -1; side <= 1; side += 2)
                {
                    var pivot = Child(root, side < 0 ? "Left wave" : "Right wave");
                    pivot.localPosition = new Vector3(side * .95f, 0, 0); pivot.localRotation = Quaternion.Euler(0, side * -90f, 0);
                    var lean = Curve(0, 0, .08f, 0, .12f, -.35f, .72f, -.3f, .78f, -1.8f, 1, -2.4f);
                    var back = Piece("Wave back", curl, mSheetDeep, pivot, new Vector3(0, 0, .18f), Vector3.zero, new Vector3(2.8f, 3.25f, 2.3f), .18f, Total - .18f,
                        Rel(.18f, RiseW(.18f, 1)), Rel(.18f, RiseH(.18f, 1.1f)), Rel(.18f, RiseW(.18f, 1)), Rel(.18f, RiseE(.18f)), 40);
                    Rotate(back, Total - .18f, lean, null, null);
                    var front = Piece("Wave body", curl, mSheet, pivot, Vector3.zero, Vector3.zero, new Vector3(2.6f, 3.1f, 2.2f), .2f, Total - .2f,
                        Rel(.2f, RiseW(.2f, 1)), Rel(.2f, RiseH(.2f, 1.12f)), Rel(.2f, RiseW(.2f, 1)), Rel(.2f, RiseE(.2f)), 0);
                    Rotate(front, Total - .2f, lean, null, null);
                    var lip = Piece("Crest lip", curl, mFoam, pivot, new Vector3(0, .05f, -.03f), Vector3.zero, new Vector3(2.65f, 3.15f, 2.25f), 0, .36f,
                        new[] { 0f, 1, .36f, 1 }, new[] { 0f, .92f, .1f, 1.04f, .36f, 1 }, new[] { 0f, 1, .36f, 1.1f }, new[] { 0f, .6f, .07f, .3f, .36f, 1 }, -30, 15);
                    Rotate(lip, .36f, Curve(0, -.6f, 1, -.6f), null, null);
                    Spray("Lip spray", pivot, new Vector3(0, 2.3f, -.5f), Vector3.zero, mDrop, ParticleSystemShapeType.Box, 0, 0, new Vector3(2.0f, .1f, .1f),
                        Vector2.zero, new Vector2(.05f, .1f), new Vector2(.35f, .55f), 1.3f, new Vector3(0, 3.6f, -2.2f), 0, 0, -60, true, 12, 15);
                }
                Ripple("Center ripple", Vector3.zero, 1.2f, 15);
                Recovery(1.5f);
                break;
            }
            case 3: // Vortex: counter-spinning helix sheets with rising foam rings.
            {
                Warmup(1.6f, true);
                var spin = Curve(0, 3.5f, .15f, 7, .75f, 8, 1, 13);
                for (int i = 0; i < 3; i++)
                {
                    var p = Piece("Helix sheet " + i, helix, mSheet, root, Vector3.zero, new Vector3(0, i * 120, 0), new Vector3(1.05f, 3.05f, 1.05f), .19f + i * .015f, Total - .19f - i * .015f,
                        Rel(.19f, RiseW(.19f, 1)), Rel(.19f, RiseH(.19f, 1.12f)), Rel(.19f, RiseW(.19f, 1)), Rel(.19f, RiseE(.19f)), 0);
                    Rotate(p, Total - .19f, null, spin, null);
                }
                for (int i = 0; i < 2; i++)
                {
                    var p = Piece("Inner counter sheet " + i, helixInner, mSheetDeep, root, Vector3.zero, new Vector3(0, 60 + i * 180, 0), new Vector3(.62f, 2.8f, .62f), .2f, Total - .2f,
                        Rel(.2f, RiseW(.2f, 1)), Rel(.2f, RiseH(.2f, 1.08f)), Rel(.2f, RiseW(.2f, 1)), Rel(.2f, RiseE(.2f)), 40);
                    Rotate(p, Total - .2f, null, Curve(0, -4, 1, -6), null);
                }
                for (int k = 0; k < 4; k++)
                {
                    var band = Piece("Rising foam ring " + k, skirt, mFoam, root, new Vector3(0, .55f + k * .58f, 0), Vector3.zero, new Vector3(1.05f + k * .05f, .38f, 1.05f + k * .05f), 0, .4f,
                        new[] { 0f, .7f, .12f, 1.08f, .4f, 1.25f }, new[] { 0f, .3f, .1f, 1.1f, .4f, .6f }, new[] { 0f, .7f, .12f, 1.08f, .4f, 1.25f }, new[] { 0f, .35f, .08f, .05f, .4f, 1 }, -30, 1 << k);
                    Rotate(band, .4f, null, Curve(0, 12, 1, 8), null);
                }
                Spray("Vortex spray", root, new Vector3(0, 1.4f, 0), new Vector3(-90, 0, 0), mDrop, ParticleSystemShapeType.Circle, 0, .85f, Vector3.one,
                    new Vector2(1.5f, 2.6f), new Vector2(.05f, .1f), new Vector2(.35f, .55f), .7f, new Vector3(0, 2.6f, 0), 0, 5.5f, -60, true, 14, 15);
                Ripple("Vortex ripple", Vector3.zero, 1.1f, 15);
                Drips("Vortex drips", new Vector3(0, 2.6f, 0), .85f);
                Recovery(1.3f);
                break;
            }
            case 4: // Spiral ribbons: trail ribbons orbit upward around a thin deep core.
            {
                Warmup(1.5f, true);
                Column("Deep core", column, mSheath, Vector3.zero, new Vector3(.26f, 2.7f, .26f), .2f, 1.08f, 40);
                var heads = Spray("Ribbon heads", root, new Vector3(0, .15f, 0), new Vector3(-90, 0, 0), mDrop, ParticleSystemShapeType.Circle, 0, .72f, Vector3.one,
                    Vector2.zero, new Vector2(.22f, .26f), new Vector2(.82f, .88f), 0, new Vector3(0, 3.2f, 0), .35f, 6.2f, -50, false, 0, 0);
                Burst(heads, .22f, 3); SpreadArc(heads); Trails(heads, .42f, .34f);
                var later = Spray("Pulse ribbon heads", root, new Vector3(0, .15f, 0), new Vector3(-90, 60, 0), mDrop, ParticleSystemShapeType.Circle, 0, .78f, Vector3.one,
                    Vector2.zero, new Vector2(.17f, .21f), new Vector2(.44f, .5f), 0, new Vector3(0, 4.6f, 0), .45f, 6.8f, -50, false, 3, 14);
                SpreadArc(later); Trails(later, .38f, .26f);
                PulseSpray("Ribbon crown spray", new Vector3(0, 2.55f, 0), 40, new Vector2(2.8f, 4.5f), 10, 15);
                Ripple("Ribbon ripple", Vector3.zero, 1.0f, 15);
                Drips("Ribbon drips", new Vector3(0, 2.4f, 0), .7f, 26);
                Recovery(1.2f);
                break;
            }
            case 5: // Splash crown: flared jagged crown that re-bursts each pulse.
            {
                Warmup(1.7f, true);
                Column("Outer crown", skirt, mSheath, Vector3.zero, new Vector3(1.4f, 2.0f, 1.4f), .19f, 1.18f, 40);
                Column("Crown body", skirt, mCrown, Vector3.zero, new Vector3(1.22f, 2.3f, 1.22f), .2f, 1.2f, 0);
                Piece("Crown rebursts", skirt, mFoam, root, new Vector3(0, .15f, 0), Vector3.zero, new Vector3(1.35f, 1.2f, 1.35f), 0, .4f,
                    new[] { 0f, .55f, .4f, 1.45f }, new[] { 0f, .35f, .14f, 1.2f, .4f, .9f }, new[] { 0f, .55f, .4f, 1.45f }, new[] { 0f, .25f, .1f, .05f, .4f, 1 }, -30, 15);
                PulseSpray("Crown fountain", new Vector3(0, 1.8f, 0), 30, new Vector2(5f, 8f), 22, 15);
                Ripple("Crown ripple", Vector3.zero, 1.3f, 15);
                Drips("Crown drips", new Vector3(0, 2.0f, 0), 1.05f, 36);
                Recovery(1.5f);
                break;
            }
            case 6: // Ring wall: hollow cylinder with foam openings; foam bands climb on pulses.
            {
                Warmup(1.9f, false);
                Column("Inner deep wall", wall, mSheath, Vector3.zero, new Vector3(1.22f, 2.5f, 1.22f), .19f, 1.08f, 40);
                Column("Water wall", wall, mWall, Vector3.zero, new Vector3(1.35f, 2.6f, 1.35f), .2f, 1.1f, 0);
                Skirt("Wall foot foam", Vector3.zero, new Vector3(1.5f, .5f, 1.5f), .24f, mFoam);
                var band = Piece("Climbing foam band", wall, mFoam, root, Vector3.zero, Vector3.zero, new Vector3(1.42f, .5f, 1.42f), 0, .46f,
                    new[] { 0f, 1, .46f, 1.04f }, new[] { 0f, .5f, .1f, 1, .46f, .7f }, new[] { 0f, 1, .46f, 1.04f }, new[] { 0f, .3f, .1f, .1f, .3f, .35f, .46f, 1 }, -30, 15);
                Velocity(band, new Vector3(0, 4.6f, 0));
                Spray("Rim spray", root, new Vector3(0, 2.45f, 0), new Vector3(-90, 0, 0), mDrop, ParticleSystemShapeType.Circle, 0, 1.35f, Vector3.one,
                    new Vector2(.6f, 1.2f), new Vector2(.05f, .1f), new Vector2(.4f, .6f), 1.3f, new Vector3(0, 3.2f, 0), 0, 0, -60, true, 18, 15, 0);
                Drips("Rim drips", new Vector3(0, 2.4f, 0), 1.3f, 40, 0);
                Recovery(1.6f);
                break;
            }
            case 7: // Tiered fountain: each pulse stacks a new overflowing tier on a central jet.
            {
                Warmup(1.6f, true);
                Column("Jet sheath", column, mSheath, Vector3.zero, new Vector3(.38f, 3.3f, .38f), .19f, 1.1f, 40);
                Column("Central jet", column, mBody, Vector3.zero, new Vector3(.28f, 3.25f, .28f), .2f, 1.12f, 0);
                for (int k = 0; k < 4; k++)
                {
                    float b = PulseAt(k), h = .55f + k * .6f, r = 1.0f - k * .14f, life = Total - b;
                    var w = new[] { 0f, .3f, .1f, 1.15f, .18f, 1f, ActionEnd - b, 1f, life, 1.3f };
                    var y = new[] { 0f, .2f, .1f, 1.25f, .2f, 1f, ActionEnd - b, 1f, life, .35f };
                    var e = new[] { 0f, .4f, .08f, 0f, ActionEnd - b - .05f, .05f, life - .15f, .7f, life, 1f };
                    Piece("Tier " + k, skirt, mFoam, root, new Vector3(0, h, 0), Vector3.zero, new Vector3(r, .42f, r), b, life, w, y, w, e, -20 + k, 1 << k);
                    Drips("Tier overflow " + k, new Vector3(0, h + .38f, 0), r * .98f, 30, 0, b + .1f);
                }
                PulseSpray("Jet top spray", new Vector3(0, 3.1f, 0), 22, new Vector2(3.5f, 5.5f), 9, 15);
                Ripple("Fountain ripple", Vector3.zero, 1.15f, 15);
                Recovery(1.3f);
                break;
            }
            case 8: // Jellyfish dome: stalk mushrooms into a pulsing dome with dripping tendrils.
            {
                Warmup(1.4f, true);
                Column("Stalk sheath", column, mSheath, Vector3.zero, new Vector3(.4f, 2.45f, .4f), .19f, 1.08f, 40);
                Column("Stalk", column, mBody, Vector3.zero, new Vector3(.3f, 2.4f, .3f), .2f, 1.1f, 0);
                float db = .27f, dl = Total - db;
                var dw = Rel(db, Keys1(db, db, .15f, db + .1f, 1.14f, db + .16f, .95f, db + .22f, 1f, 0, 1.08f, ActionEnd, 1f, 1.42f, 1.18f, Total, 1.3f));
                var dh = Rel(db, Keys1(db, db, .3f, db + .1f, 1.18f, db + .18f, 1f, 0, 1.06f, ActionEnd, 1f, 1.42f, .7f, Total, .45f));
                var de = Rel(db, new[] { db, .5f, db + .08f, 0f, ActionEnd - .05f, 0f, 1.45f, .65f, Total, 1f });
                Piece("Dome underside", dome, mSheath, root, new Vector3(0, 2.3f, 0), Vector3.zero, new Vector3(1.05f, .62f, 1.05f), db, dl, dw, dh, dw, de, 40);
                Piece("Dome bell", dome, mBody, root, new Vector3(0, 2.33f, 0), Vector3.zero, new Vector3(1.15f, .75f, 1.15f), db, dl, dw, dh, dw, de, 0);
                Piece("Rim foam pulse", ring, mFoam, root, new Vector3(0, 2.36f, 0), Vector3.zero, new Vector3(1.18f, 1, 1.18f), 0, .36f,
                    new[] { 0f, .9f, .36f, 1.35f }, new[] { 0f, 1f, .36f, 1f }, new[] { 0f, .9f, .36f, 1.35f }, new[] { 0f, .1f, .36f, 1 }, -30, 15);
                var tendrils = Spray("Tendrils", root, new Vector3(0, 2.32f, 0), new Vector3(-90, 0, 0), mDrop, ParticleSystemShapeType.Circle, 0, .95f, Vector3.one,
                    new Vector2(.2f, .5f), new Vector2(.05f, .08f), new Vector2(.85f, 1.05f), .45f, Vector3.zero, 0, 0, -50, false, 8, 15, 0);
                Rate(tendrils, 26, .42f, 1.3f); Trails(tendrils, .55f, .11f);
                Ripple("Dome ripple", Vector3.zero, 1.0f, 15);
                Recovery(1.3f);
                break;
            }
            case 9: // Lotus bloom: petals rise, open outward, and fall open further in recovery.
            {
                Warmup(1.6f, true);
                var open = Curve(0, 0, .06f, 0, .14f, -1.3f, .28f, -.05f, .74f, -.05f, .8f, -.9f, 1, -1.1f);
                for (int i = 0; i < 6; i++)
                {
                    float b = .19f + (i % 3) * .015f;
                    var p = Piece("Outer petal " + i, petal, mSheet, root, Vector3.zero, new Vector3(0, i * 60 + 30, 0), new Vector3(1.35f, 2.6f + (i % 2) * .25f, 1.35f), b, Total - b,
                        Rel(b, RiseW(b, 1)), Rel(b, RiseH(b, 1.1f)), Rel(b, RiseW(b, 1)), Rel(b, RiseE(b)), 0);
                    Rotate(p, Total - b, null, null, open);
                }
                for (int i = 0; i < 3; i++)
                {
                    var p = Piece("Inner petal " + i, petal, mSheetDeep, root, Vector3.zero, new Vector3(0, i * 120, 0), new Vector3(.8f, 2.9f, .8f), .2f, Total - .2f,
                        Rel(.2f, RiseW(.2f, 1)), Rel(.2f, RiseH(.2f, 1.08f)), Rel(.2f, RiseW(.2f, 1)), Rel(.2f, RiseE(.2f)), 40);
                    Rotate(p, Total - .2f, null, null, Curve(0, 0, .1f, -.5f, .3f, 0, .8f, 0, 1, -.6f));
                }
                Crest("Foam heart", new Vector3(0, .25f, 0), .6f, 15);
                PulseSpray("Heart spray", new Vector3(0, .5f, 0), 14, new Vector2(5.5f, 8f), 12, 15);
                Ripple("Lotus ripple", Vector3.zero, 1.2f, 15);
                Drips("Petal tip drips", new Vector3(0, 2.2f, 0), 1.25f, 34, 0);
                Recovery(1.4f);
                break;
            }
        }
    }

    // ------------------------------------------------------------------ shared phases

    // Warmup (0 - 0.28): the pool gathers from sparse to full, rings and streaks pull inward, a mound swells.
    private static void Warmup(float radius, bool mound)
    {
        Piece("Gathering pool", disc, mGround, root, new Vector3(0, .02f, 0), Vector3.zero, new Vector3(radius, 1, radius), 0, Total,
            new[] { 0f, .35f, Impact, .85f, Impact + .06f, 1.07f, .45f, 1f, ActionEnd, 1f, Total, 1.22f }, new[] { 0f, 1f, 1f, 1f },
            new[] { 0f, .35f, Impact, .85f, Impact + .06f, 1.07f, .45f, 1f, ActionEnd, 1f, Total, 1.22f },
            new[] { 0f, .75f, Impact - .02f, .12f, .4f, 0f, 1.2f, 0f, Total, 1f }, 200);
        var rings = Piece("Gathering rings", ring, mRipple, root, new Vector3(0, .03f, 0), Vector3.zero, new Vector3(radius * 1.25f, 1, radius * 1.25f), 0, .2f,
            new[] { 0f, 1f, .2f, .3f }, new[] { 0f, 1f, .2f, 1f }, new[] { 0f, 1f, .2f, .3f }, new[] { 0f, .6f, .07f, .12f, .2f, .5f }, 150);
        Burst(rings, .1f, 1);
        var streaks = Spray("Gathering streaks", root, new Vector3(0, .06f, 0), new Vector3(-90, 0, 0), mDrop, ParticleSystemShapeType.Circle, 0, radius * 1.05f, Vector3.one,
            Vector2.zero, new Vector2(.05f, .08f), new Vector2(.18f, .24f), 0, Vector3.zero, -6.5f, 0, -40, true, 0, 0, 0);
        Burst(streaks, .02f, 14); Burst(streaks, .12f, 12);
        if (mound)
            Piece("Pre-rise mound", dome, mBody, root, Vector3.zero, Vector3.zero, new Vector3(.62f, .5f, .62f), .05f, .26f,
                new[] { 0f, .3f, .2f, 1f, .26f, 1.15f }, new[] { 0f, .1f, .17f, .9f, .26f, 1.4f }, new[] { 0f, .3f, .2f, 1f, .26f, 1.15f }, new[] { 0f, .5f, .1f, 0f, .19f, 0f, .26f, 1f }, 10);
    }

    // Recovery (1.25 - 1.6): splash-down droplets and a final widening ripple while bodies erode and sink.
    private static void Recovery(float radius)
    {
        var splash = Spray("Splash down", root, new Vector3(0, .05f, 0), new Vector3(-90, 0, 0), mDrop, ParticleSystemShapeType.Cone, 38, radius * .7f, Vector3.one,
            new Vector2(1.8f, 3.4f), new Vector2(.05f, .1f), new Vector2(.3f, .42f), 1.6f, Vector3.zero, 0, 0, -60, true, 0, 0);
        Burst(splash, 1.3f, 16);
        Piece("Recovery ripple", ring, mRipple, root, new Vector3(0, .03f, 0), Vector3.zero, new Vector3(radius, 1, radius), 1.28f, .32f,
            new[] { 0f, .7f, .32f, 1.5f }, new[] { 0f, 1f, .32f, 1f }, new[] { 0f, .7f, .32f, 1.5f }, new[] { 0f, .05f, .32f, 1f }, 160);
    }

    private static void Ripple(string name, Vector3 at, float radius, int mask)
        => Piece(name, ring, mRipple, root, at + Vector3.up * .035f, Vector3.zero, new Vector3(radius, 1, radius), 0, .55f,
            new[] { 0f, .45f, .2f, 1.3f, .55f, 1.9f }, new[] { 0f, 1f, .55f, 1f }, new[] { 0f, .45f, .2f, 1.3f, .55f, 1.9f }, new[] { 0f, 0f, .2f, .15f, .55f, 1f }, 160, mask);

    private static void Crest(string name, Vector3 at, float radius, int mask)
        => Piece(name, skirt, mFoam, root, at, Vector3.zero, new Vector3(radius, .6f, radius), 0, .38f,
            new[] { 0f, .5f, .12f, 1.1f, .38f, 1.4f }, new[] { 0f, .3f, .1f, 1.15f, .38f, .7f }, new[] { 0f, .5f, .12f, 1.1f, .38f, 1.4f }, new[] { 0f, .12f, .1f, 0f, .38f, 1f }, -30, mask);

    private static void Skirt(string name, Vector3 at, Vector3 size, float b, Material material, int mask = 0)
        => Piece(name, skirt, material, root, at, Vector3.zero, size, b, Total - b,
            Rel(b, RiseW(b, 1.1f)), Rel(b, RiseH(b, 1.3f)), Rel(b, RiseW(b, 1.1f)), Rel(b, new[] { b, .5f, b + .06f, .05f, ActionEnd - .1f, .12f, 1.4f, .8f, Total, 1f }), -20, mask);

    private static void Column(string name, Mesh mesh, Material material, Vector3 at, Vector3 size, float b, float peak, int fudge, int mask = 0)
        => Piece(name, mesh, material, root, at, Vector3.zero, size, b, Total - b,
            Rel(b, RiseW(b, 1)), Rel(b, RiseH(b, peak)), Rel(b, RiseW(b, 1)), Rel(b, RiseE(b)), fudge, mask);

    private static void PulseSpray(string name, Vector3 at, float angle, Vector2 speed, int count, int mask)
        => Spray(name, root, at, new Vector3(-90, 0, 0), mDrop, ParticleSystemShapeType.Cone, angle, .15f, Vector3.one,
            speed, new Vector2(.05f, .12f), new Vector2(.4f, .6f), 1.5f, Vector3.zero, 0, 0, -60, true, count, mask);

    private static void Drips(string name, Vector3 at, float radius, float rate = 30, float edge = 1, float from = 1.12f)
    {
        var ps = Spray(name, root, at, new Vector3(-90, 0, 0), mDrop, ParticleSystemShapeType.Circle, 0, radius, Vector3.one,
            new Vector2(.2f, .6f), new Vector2(.04f, .08f), new Vector2(.32f, .48f), 1.7f, Vector3.zero, 0, 0, -55, true, 0, 0, edge);
        Rate(ps, rate, from, 1.42f);
    }

    // Main-body timeline, absolute seconds: snap up with overshoot onto the impact, beat on every later pulse,
    // hold, then sink and widen during recovery while erosion opens foam-edged holes from the top.
    private static float[] RiseH(float b, float peak)
    {
        var k = new List<float> { b, 0f, b + RiseTime, peak, b + RiseTime + .06f, .93f, b + RiseTime + .12f, 1f };
        Beats(k, 1.05f); k.AddRange(new[] { ActionEnd, 1f, 1.42f, .55f, Total, .15f }); return Clean(k);
    }
    private static float[] RiseW(float b, float scale)
    {
        var k = new List<float> { b, .45f * scale, b + RiseTime, .85f * scale, b + RiseTime + .08f, 1.06f * scale, b + RiseTime + .14f, scale };
        Beats(k, 1.08f * scale, scale); k.AddRange(new[] { ActionEnd, scale, 1.42f, 1.15f * scale, Total, 1.25f * scale }); return Clean(k);
    }
    private static float[] RiseE(float b) => new[] { b, .55f, b + RiseTime - .02f, 0f, ActionEnd - .05f, 0f, 1.45f, .65f, Total, 1f };
    private static void Beats(List<float> k, float beat, float rest = 1f)
    {
        for (int p = 1; p < 4; p++)
        {
            float t = PulseAt(p);
            if (t + .03f <= k[k.Count - 2] + .01f) continue;
            k.AddRange(new[] { t + .03f, beat, t + .1f, rest });
        }
    }
    // Keys with 0 as a time mean "pulse beat": expand them across the remaining pulses.
    private static float[] Keys1(float b, params float[] pairs)
    {
        var k = new List<float>();
        for (int i = 0; i < pairs.Length; i += 2)
        {
            if (pairs[i] == 0 && i > 0) { Beats(k, pairs[i + 1]); continue; }
            k.Add(pairs[i]); k.Add(pairs[i + 1]);
        }
        return Clean(k);
    }
    private static float[] Clean(List<float> k)
    {
        var o = new List<float>();
        for (int i = 0; i < k.Count; i += 2) if (o.Count == 0 || k[i] > o[o.Count - 2] + .005f) { o.Add(k[i]); o.Add(k[i + 1]); }
        return o.ToArray();
    }
    private static float[] Rel(float b, float[] abs)
    {
        var o = new float[abs.Length];
        for (int i = 0; i < abs.Length; i += 2) { o[i] = Mathf.Max(0, abs[i] - b); o[i + 1] = abs[i + 1]; }
        return o;
    }

    // ------------------------------------------------------------------ native particle construction

    // One authored mesh surface carried by a single native mesh particle. Size curves shape it,
    // Custom1.x erodes it through the material, Custom1.y seeds its texture flow.
    private static ParticleSystem Piece(string name, Mesh mesh, Material material, Transform parent, Vector3 position, Vector3 euler, Vector3 size,
        float birth, float life, float[] sx, float[] sy, float[] sz, float[] erosion, int fudge, int pulseMask = 0)
    {
        var t = Child(parent, name + " " + pieceCount++); t.localPosition = position; t.localRotation = Quaternion.Euler(euler);
        var ps = NewSystem(t);
        var main = ps.main; main.startLifetime = life; main.startSpeed = 0; main.startSize3D = true;
        main.startSizeX = size.x; main.startSizeY = size.y; main.startSizeZ = size.z;
        main.startRotation3D = true; main.startRotationX = 0; main.startRotationY = 0; main.startRotationZ = 0;
        main.simulationSpace = ParticleSystemSimulationSpace.Local; main.maxParticles = pulseMask == 0 ? 2 : 6;
        var shape = ps.shape; shape.enabled = false;
        var emission = ps.emission; emission.rateOverTime = 0;
        var sol = ps.sizeOverLifetime; sol.enabled = true; sol.separateAxes = true;
        sol.x = new ParticleSystem.MinMaxCurve(1, Norm(sx, life)); sol.y = new ParticleSystem.MinMaxCurve(1, Norm(sy, life)); sol.z = new ParticleSystem.MinMaxCurve(1, Norm(sz, life));
        Custom(ps, Norm(erosion, life));
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Mesh; r.mesh = mesh; r.alignment = ParticleSystemRenderSpace.Local;
        Finish(r, material, fudge);
        if (pulseMask == 0) Burst(ps, birth, 1); else pulses.Add((ps, 1, pulseMask));
        return ps;
    }

    private static ParticleSystem Spray(string name, Transform parent, Vector3 position, Vector3 euler, Material material, ParticleSystemShapeType type, float angle, float radius, Vector3 boxScale,
        Vector2 speed, Vector2 size, Vector2 life, float gravity, Vector3 velocity, float radial, float orbitY, int fudge, bool stretch, int pulseCount, int pulseMask, float thickness = 1)
    {
        var t = Child(parent, name + " " + pieceCount++); t.localPosition = position; t.localRotation = Quaternion.identity;
        var ps = NewSystem(t);
        var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y); main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        float k = stretch ? 1.8f : 1f; // droplets must read at gameplay distance
        main.startSize = new ParticleSystem.MinMaxCurve(size.x * k, size.y * k); main.gravityModifier = gravity; main.maxParticles = 160;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission; emission.rateOverTime = 0;
        var shape = ps.shape; shape.enabled = true; shape.shapeType = type; shape.angle = angle; shape.radius = Mathf.Max(.01f, radius);
        shape.radiusThickness = thickness; shape.rotation = euler; shape.scale = type == ParticleSystemShapeType.Box ? boxScale : Vector3.one;
        if (velocity != Vector3.zero || radial != 0 || orbitY != 0)
        {
            var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.Local;
            v.x = velocity.x; v.y = velocity.y; v.z = velocity.z; v.orbitalX = 0; v.orbitalY = orbitY; v.orbitalZ = 0; v.radial = radial;
        }
        Custom(ps, Curve(0, 0, .55f, .08f, 1, .95f));
        var r = ps.GetComponent<ParticleSystemRenderer>();
        if (stretch) { r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = .05f; r.lengthScale = 1.3f; }
        else r.renderMode = ParticleSystemRenderMode.Billboard;
        Finish(r, material, fudge);
        if (pulseCount > 0) pulses.Add((ps, pulseCount, pulseMask));
        return ps;
    }

    private static ParticleSystem NewSystem(Transform t)
    {
        var ps = t.gameObject.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.playOnAwake = false; main.loop = false; main.duration = Total; main.startDelay = 0;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy; main.startColor = Color.white;
        ps.useAutoRandomSeed = false; ps.randomSeed = (uint)(977 + candidate * 131 + pieceCount * 17);
        return ps;
    }

    private static void Custom(ParticleSystem ps, AnimationCurve erosion)
    {
        var cd = ps.customData; cd.enabled = true;
        cd.SetMode(ParticleSystemCustomData.Custom1, ParticleSystemCustomDataMode.Vector);
        cd.SetVectorComponentCount(ParticleSystemCustomData.Custom1, 2);
        cd.SetVector(ParticleSystemCustomData.Custom1, 0, new ParticleSystem.MinMaxCurve(1, erosion));
        cd.SetVector(ParticleSystemCustomData.Custom1, 1, new ParticleSystem.MinMaxCurve(0, 1));
    }

    private static void Finish(ParticleSystemRenderer r, Material material, int fudge)
    {
        r.sharedMaterial = material; r.sortingFudge = fudge;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        r.SetActiveVertexStreams(new List<ParticleSystemVertexStream> {
            ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Normal, ParticleSystemVertexStream.Color,
            ParticleSystemVertexStream.UV, ParticleSystemVertexStream.Custom1XY });
        renderers.Add(r);
    }

    private static void Burst(ParticleSystem ps, float time, int count)
    {
        var emission = ps.emission; var list = new ParticleSystem.Burst[emission.burstCount]; emission.GetBursts(list);
        emission.SetBursts(list.Concat(new[] { new ParticleSystem.Burst(time, (short)count) }).ToArray());
        if (!startup.Contains(ps)) startup.Add(ps);
    }
    private static void Rate(ParticleSystem ps, float rate, float from, float to)
    {
        var emission = ps.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(rate, Linear(0, 0, from / Total - .001f, 0, from / Total, 1, to / Total, 1, to / Total + .001f, 0, 1, 0));
        if (!startup.Contains(ps)) startup.Add(ps);
    }
    private static void SpreadArc(ParticleSystem ps) { var shape = ps.shape; shape.arcMode = ParticleSystemShapeMultiModeValue.BurstSpread; shape.radiusThickness = 0; }
    private static void Trails(ParticleSystem ps, float lifetime, float width)
    {
        var trails = ps.trails; trails.enabled = true; trails.ratio = 1; trails.lifetime = lifetime; trails.minVertexDistance = .03f;
        trails.worldSpace = true; trails.dieWithParticles = false; trails.sizeAffectsWidth = false; trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(width, Curve(0, .2f, .15f, 1, .7f, .6f, 1, 0));
        var r = ps.GetComponent<ParticleSystemRenderer>(); r.trailMaterial = mTrail;
    }
    private static void Velocity(ParticleSystem ps, Vector3 velocity)
    {
        var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.Local;
        v.x = velocity.x; v.y = velocity.y; v.z = velocity.z; v.orbitalX = 0; v.orbitalY = 0; v.orbitalZ = 0; v.radial = 0;
    }
    // Angular velocity curves in radians/second over normalized lifetime; all axes share Curve mode.
    private static void Rotate(ParticleSystem ps, float life, AnimationCurve x, AnimationCurve y, AnimationCurve z)
    {
        var rol = ps.rotationOverLifetime; rol.enabled = true; rol.separateAxes = true;
        rol.x = new ParticleSystem.MinMaxCurve(1, x ?? Curve(0, 0, 1, 0)); rol.y = new ParticleSystem.MinMaxCurve(1, y ?? Curve(0, 0, 1, 0)); rol.z = new ParticleSystem.MinMaxCurve(1, z ?? Curve(0, 0, 1, 0));
    }

    // ------------------------------------------------------------------ authored meshes (editor-time assets)

    private static void BuildMeshes()
    {
        column = Revolve("column", 32, 24, v => 1 - .2f * v, v => v, 360, .12f, 2);
        wall = Revolve("wall", 48, 20, v => 1 + .04f * v, v => v, 360, .05f, 4);
        skirt = Revolve("skirt", 40, 16, v => .4f + .6f * Mathf.Pow(v, 1.4f), v => v, 360, .08f, 3);
        helix = Revolve("helix", 20, 40, v => .55f + .45f * v, v => v, 85, .9f, 1);
        helixInner = Revolve("helix-inner", 16, 36, v => .7f + .3f * v, v => v, 70, -.7f, 1);
        petal = Revolve("petal", 16, 24, v => .15f + .85f * Mathf.Pow(v, 1.4f), v => Mathf.Sin(v * Mathf.PI * .5f), 46, 0, 1);
        dome = Revolve("dome", 36, 14, v => Mathf.Cos(v * Mathf.PI * .5f) * (1 + .12f * (1 - v) * (1 - v)), v => Mathf.Sin(v * Mathf.PI * .5f), 360, 0, 2);
        disc = Annulus("disc", 0, 3);
        ring = Annulus("ring", .72f, 4);
        curl = Curl("curl");
    }

    // Surface of revolution: u around (repeated on closed loops), v up the profile.
    private static Mesh Revolve(string name, int seg, int rows, Func<float, float> radius, Func<float, float> height, float arc, float twistTurns, float uRepeat)
    {
        bool closed = arc >= 359.9f;
        var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
        for (int j = 0; j <= rows; j++)
        {
            float v = j / (float)rows;
            for (int i = 0; i <= seg; i++)
            {
                float u = i / (float)seg;
                float a = arc * Mathf.Deg2Rad * (closed ? u : u - .5f) + twistTurns * Mathf.PI * 2 * v;
                verts.Add(new Vector3(Mathf.Cos(a) * radius(v), height(v), Mathf.Sin(a) * radius(v)));
                uvs.Add(new Vector2(closed ? u * uRepeat : u, v));
            }
        }
        Grid(tris, seg, rows);
        return Save(name, verts, uvs, tris);
    }

    // Flat pool/ripple: v = 1 at the inner edge so foam and light sit toward the center.
    private static Mesh Annulus(string name, float inner, float uRepeat)
    {
        int seg = 56, rows = 10;
        var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
        for (int j = 0; j <= rows; j++)
        {
            float v = j / (float)rows, r = Mathf.Lerp(1, inner, v);
            for (int i = 0; i <= seg; i++)
            {
                float u = i / (float)seg, a = u * Mathf.PI * 2;
                verts.Add(new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r)); uvs.Add(new Vector2(u * uRepeat, v));
            }
        }
        Grid(tris, seg, rows);
        return Save(name, verts, uvs, tris);
    }

    // Curling wave sheet: length along x, rises from the ground and curls toward -z; ends wrap toward -z.
    private static Mesh Curl(string name)
    {
        int seg = 28, rows = 28;
        var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
        for (int j = 0; j <= rows; j++)
        {
            float v = j / (float)rows, theta = v * Mathf.PI * 1.05f, rho = .5f * (1 - .4f * v);
            for (int i = 0; i <= seg; i++)
            {
                float u = i / (float)seg, x = u - .5f, end = 4 * x * x;
                float y = (.5f - rho * Mathf.Cos(theta)) * (1 - .3f * end);
                float z = -rho * Mathf.Sin(theta) - .3f * end;
                verts.Add(new Vector3(x, y / .82f, z)); uvs.Add(new Vector2(u, v));
            }
        }
        Grid(tris, seg, rows);
        return Save(name, verts, uvs, tris);
    }

    private static void Grid(List<int> tris, int seg, int rows)
    {
        for (int j = 0; j < rows; j++)
            for (int i = 0; i < seg; i++)
            {
                int a = j * (seg + 1) + i, b = a + 1, c = a + seg + 1, d = c + 1;
                tris.AddRange(new[] { a, c, b, b, c, d });
            }
    }

    private static Mesh Save(string name, List<Vector3> verts, List<Vector2> uvs, List<int> tris)
    {
        var mesh = new Mesh { name = name }; mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
        string path = Root + "/Meshes/" + name + ".asset"; produced.Add(path); var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (old == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        EditorUtility.CopySerialized(mesh, old); UnityEngine.Object.DestroyImmediate(mesh); EditorUtility.SetDirty(old); return old;
    }

    // ------------------------------------------------------------------ materials

    private static readonly Color Deep = new Color(.03f, .10f, .45f), Body = new Color(.05f, .42f, .95f), Light = new Color(.35f, .95f, 1f), Foam = new Color(1.12f, 1.22f, 1.28f);

    private static void BuildMaterials()
    {
        mBody = Mat("water-body", m => { });
        mSheet = Mat("water-sheet", m => { m.SetFloat("_SideCut", .1f); m.SetVector("_Bands", new Vector4(.45f, .9f, .55f, .12f)); m.SetVector("_FoamLines", new Vector4(.92f, 1, 0, 0)); });
        mSheath = Mat("water-deep", m =>
        {
            m.SetColor("_DeepColor", new Color(.02f, .06f, .32f)); m.SetColor("_BodyColor", new Color(.04f, .24f, .7f)); m.SetColor("_LightColor", new Color(.1f, .62f, .9f));
            m.SetVector("_ToneAlpha", new Vector4(.35f, .6f, .72f, .9f)); m.SetVector("_Bands", new Vector4(.5f, .9f, .6f, .3f));
            m.SetVector("_CausticFlow", new Vector4(1.2f, .8f, 0, -.55f)); m.SetVector("_FoamLines", new Vector4(.8f, .4f, 0, 0));
            m.SetFloat("_TopFoam", .03f); m.SetFloat("_TopTear", .25f); m.SetFloat("_BackShade", .3f);
        });
        mSheetDeep = Mat("water-sheet-deep", m => { m.CopyPropertiesFromMaterial(mSheath); m.SetFloat("_SideCut", .12f); });
        mCrown = Mat("water-crown", m => { m.SetFloat("_ErodeTop", .5f); m.SetFloat("_Erode", .1f); m.SetFloat("_EdgeFoam", .12f); m.SetFloat("_TopTear", .6f); m.SetFloat("_TopFoam", .08f); m.SetVector("_TearTile", new Vector4(1.1f, .12f, 0, 0)); });
        mWall = Mat("water-wall", m =>
        {
            m.SetFloat("_ErodeTop", .25f); m.SetFloat("_Erode", .2f); m.SetFloat("_EdgeFoam", .1f);
            m.SetVector("_ToneAlpha", new Vector4(.4f, .82f, .92f, 1)); m.SetVector("_ErodeFlow", new Vector4(.5f, .3f, 0, -.1f));
        });
        mFoam = Mat("water-foam", m =>
        {
            m.SetVector("_Bands", new Vector4(.15f, .42f, .5f, .5f)); m.SetVector("_FoamLines", new Vector4(.3f, 1, 0, 0));
            m.SetVector("_ToneAlpha", new Vector4(.7f, .95f, 1, 1)); m.SetFloat("_TopFoam", .1f); m.SetFloat("_TopTear", .55f); m.SetFloat("_EdgeFoam", .14f);
            m.SetFloat("_ErodeTop", .3f); m.SetFloat("_Erode", .12f); m.SetVector("_ErodeFlow", new Vector4(.3f, .22f, 0, -.12f));
            m.SetFloat("_BottomFade", .12f);
        });
        mGround = Mat("water-pool", m =>
        {
            m.SetVector("_Bands", new Vector4(.42f, .78f, .35f, 0)); m.SetVector("_ToneAlpha", new Vector4(.45f, .8f, .9f, 1));
            m.SetVector("_CausticFlow", new Vector4(1, .7f, 0, .35f)); m.SetVector("_FoamFlow", new Vector4(1, .6f, 0, .5f)); m.SetVector("_FoamLines", new Vector4(.45f, 1, 0, 0));
            m.SetVector("_ErodeFlow", new Vector4(.3f, .25f, 0, .08f)); m.SetFloat("_ErodeTop", .5f); m.SetFloat("_TopFoam", .1f);
            m.SetFloat("_BottomFade", .25f); m.SetFloat("_BackShade", 0); m.SetVector("_Wobble", Vector4.zero); m.SetFloat("_TopTear", 0);
        });
        mRipple = Mat("water-ripple", m =>
        {
            m.CopyPropertiesFromMaterial(mGround);
            m.SetVector("_Bands", new Vector4(.3f, .6f, .3f, 0)); m.SetVector("_FoamLines", new Vector4(.2f, 1, 0, 0));
            m.SetFloat("_TopFoam", .28f); m.SetFloat("_EdgeFoam", .1f); m.SetFloat("_BottomFade", .3f);
        });
        mDrop = Mat("water-drop", m =>
        {
            m.SetTexture("_ShapeTex", Tex("Glow_02")); m.SetFloat("_UseShape", 1);
            m.SetColor("_BodyColor", new Color(.2f, .75f, 1f)); m.SetColor("_LightColor", new Color(.8f, .98f, 1f));
            m.SetVector("_Bands", new Vector4(.2f, .55f, 0, 0)); m.SetVector("_ToneAlpha", new Vector4(1, 1, 1, 1)); m.SetVector("_FoamLines", Vector4.zero);
            m.SetFloat("_TopFoam", .22f); m.SetFloat("_TopTear", 0); m.SetFloat("_BottomFade", 0); m.SetFloat("_BackShade", 0); m.SetVector("_Wobble", Vector4.zero);
        });
        mTrail = Mat("water-trail", m =>
        {
            m.SetVector("_Bands", new Vector4(.35f, .7f, .3f, 0)); m.SetVector("_FoamFlow", new Vector4(3, 1, -1.5f, 0)); m.SetVector("_FoamLines", new Vector4(.5f, 1, 0, 0));
            m.SetVector("_CausticFlow", new Vector4(2, 1, -1, 0)); m.SetFloat("_ErodeTop", 0); m.SetFloat("_TopFoam", .15f); m.SetFloat("_TopTear", 0); m.SetFloat("_BottomFade", .1f);
            m.SetFloat("_BackShade", 0); m.SetVector("_Wobble", Vector4.zero);
        });
    }

    private static Material Mat(string name, Action<Material> configure)
    {
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Shaders/AnimeWater.shader");
        if (shader == null) throw new InvalidOperationException("Anime water shader missing.");
        string path = Root + "/Materials/" + name + ".mat"; produced.Add(path);
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        else material.shader = shader;
        material.SetColor("_DeepColor", Deep); material.SetColor("_BodyColor", Body); material.SetColor("_LightColor", Light); material.SetColor("_FoamColor", Foam);
        material.SetTexture("_CausticTex", Tex("Water02")); material.SetTexture("_FoamTex", Tex("Water01")); material.SetTexture("_ErodeTex", Tex("Voronoi01"));
        // Shared cel defaults: mostly blue body, deep caustic blobs, light/foam only on lit rims.
        material.SetVector("_Bands", new Vector4(.45f, .82f, .55f, .25f)); material.SetVector("_FoamLines", new Vector4(.86f, 1, 0, 0));
        material.SetVector("_FoamFlow", new Vector4(1.2f, .8f, .04f, -1.4f)); material.SetVector("_ErodeFlow", new Vector4(.32f, .24f, 0, -.12f)); // Voronoi01 holds ~15 cells per tile
        material.SetFloat("_TopTear", .3f); material.SetFloat("_TopFoam", .06f); material.SetFloat("_EdgeFoam", .1f);
        configure(material); EditorUtility.SetDirty(material); return material;
    }
    private static Texture2D Tex(string name)
    {
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GabrielAguiarProductions/Textures/" + name + ".png");
        if (t == null) throw new InvalidOperationException("Missing texture " + name); return t;
    }

    // ------------------------------------------------------------------ animation & utilities

    // Rise keeps recovery at zero; Drain (interruption) erodes every surface through the material property.
    private static AnimatorController Controller(string id)
    {
        var rise = new AnimationClip { frameRate = 60 }; var drain = new AnimationClip { frameRate = 60 };
        foreach (var r in renderers)
        {
            string p = AnimationUtility.CalculateTransformPath(r.transform, root);
            AnimationUtility.SetEditorCurve(rise, EditorCurveBinding.FloatCurve(p, typeof(ParticleSystemRenderer), "material._Recover"), Linear(0, 0, 1, 0));
            AnimationUtility.SetEditorCurve(drain, EditorCurveBinding.FloatCurve(p, typeof(ParticleSystemRenderer), "material._Recover"), Linear(0, 0, .2f, 1));
        }
        var riseClip = SaveClip(rise, Root + "/Animation/" + id + "-rise.anim");
        var drainClip = SaveClip(drain, Root + "/Animation/" + id + "-drain.anim");
        string path = Root + "/Animation/" + id + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.parameters = new[] { new AnimatorControllerParameter { name = "Drain", type = AnimatorControllerParameterType.Trigger }, new AnimatorControllerParameter { name = "Phase", type = AnimatorControllerParameterType.Float } };
        var machine = controller.layers[0].stateMachine;
        foreach (var transition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(transition);
        foreach (var state in machine.states) machine.RemoveState(state.state);
        var start = machine.AddState("Rise"); start.motion = riseClip; start.writeDefaultValues = false; machine.defaultState = start;
        start.timeParameterActive = true; start.timeParameter = "Phase";
        var end = machine.AddState("Drain"); end.motion = drainClip; end.writeDefaultValues = false;
        var stop = machine.AddAnyStateTransition(end); stop.hasExitTime = false; stop.duration = .02f; stop.canTransitionToSelf = false; stop.AddCondition(AnimatorConditionMode.If, 0, "Drain");
        EditorUtility.SetDirty(controller); return controller;
    }

    private static AnimationCurve Norm(float[] pairs, float life)
    {
        var scaled = new float[pairs.Length];
        for (int i = 0; i < pairs.Length; i += 2) { scaled[i] = Mathf.Clamp01(pairs[i] / life); scaled[i + 1] = pairs[i + 1]; }
        return Curve(scaled);
    }
    private static AnimationCurve Curve(params float[] pairs)
    {
        var keys = new List<Keyframe>();
        for (int i = 0; i < pairs.Length; i += 2) if (keys.Count == 0 || pairs[i] > keys[keys.Count - 1].time + 1e-4f) keys.Add(new Keyframe(pairs[i], pairs[i + 1]));
        var curve = new AnimationCurve(keys.ToArray());
        for (int i = 0; i < curve.length; i++) { AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto); AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto); }
        return curve;
    }
    private static AnimationCurve Linear(params float[] pairs)
    {
        var curve = Curve(pairs);
        for (int i = 0; i < curve.length; i++) { AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear); }
        return curve;
    }
    private static T Asset<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path); if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
    }
    private static Transform Child(Transform parent, string name)
    {
        var t = new GameObject(name).transform; t.SetParent(parent, false); t.gameObject.layer = parent.gameObject.layer; return t;
    }
    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int split = path.LastIndexOf('/'); Folder(path.Substring(0, split)); AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
    }
    private static AnimationClip SaveClip(AnimationClip clip, string path)
    {
        clip.name = System.IO.Path.GetFileNameWithoutExtension(path);
        var old = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (old == null) { AssetDatabase.CreateAsset(clip, path); return clip; }
        EditorUtility.CopySerialized(clip, old); UnityEngine.Object.DestroyImmediate(clip); EditorUtility.SetDirty(old); return old;
    }
}
