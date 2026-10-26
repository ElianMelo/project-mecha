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

// Native mesh particles, scrolling/displaced vendor water materials, ballistic spray and trails.
public static class RisingWaterVariantsAuthoring
{
    public const string Root = "Assets/_Project/VFX/RisingWaterVariants";
    public static readonly string[] Keys = { "sheets", "geysers", "crests", "fins", "ribbons", "fans", "curtains", "bands", "branches", "petals" };
    public static readonly string[] Titles = { "Flowing sheets", "Clustered geysers", "Tidal crests", "Split fountains", "Braided currents", "Surging fans", "Broken curtains", "Ascending streams", "Branching torrents", "Splash crown" };
    private static readonly string[] Descriptions = {
        "Separated hollow sheets, recessed blue folds and detached crest foam.",
        "Unequal curved geysers with staggered caps and open channels.",
        "Opposed curling waves rise around a clear central opening.",
        "Five rising jets with diverging streams and detached foam.",
        "Sinuous currents weave upward between separated front and rear crests.",
        "Asymmetric fans unfold through hollow scalloped water sheets.",
        "Torn flowing curtains split into distinct suspended folds.",
        "Three water streams carry successive broken foam bands upward.",
        "Flared water surges rise with branching streams and detached foam.",
        "Broad curled petals rise around a hollow water crown." };
    private static Transform root;
    private static AnimationClip rise, drain;
    private static List<ParticleSystem> startup, pulse, meshPulse;
    private static Material water, foam, drops, trail;
    private static int candidate, lobeCount;
    private const string Pix = "Assets/PixPlays/";
    private const string BeamPath = Pix + "ElementalBeams/WaterBeam/Version_BuiltIn/WaterBeam.prefab";
    private const string AoePath = Pix + "ElementalAOE/WaterAOE/Version_BuiltIn/WaterAoeVFX.prefab";

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
                if (asset == null) continue;
                string category = asset is MonoScript ? path.Contains("/Editor/") ? "Others" : "Code" : asset is GameObject ? "Prefabs" :
                    asset is ScriptableObject || asset is AnimationClip || asset is RuntimeAnimatorController ? "Authored Data" : "Others";
                var labels = AssetDatabase.GetLabels(asset).Concat(new[] { category, "VFXCreator" }).ToList();
                if (!labels.Contains("Keep") && !labels.Contains("Replace")) labels.Add("Keep");
                AssetDatabase.SetLabels(asset, labels.Distinct().ToArray());
            }
            AssetDatabase.SaveAssets();
            return "Reworked " + (only < 0 ? "10 candidates" : Titles[only]) + "; unchanged action and live S override.";
        }
        finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
    }

    private static void BuildOne(int index, AttackPiece action)
    {
        candidate = index; lobeCount = 0;
        string id = (index + 1).ToString("00") + "-" + Keys[index];
        var profile = Asset<RisingWaterVariantSettings>(Root + "/Profiles/" + id + ".asset");
        profile.previewAction = action; profile.tailSeconds = 0.35f; profile.interruptedTail = 0.22f; profile.dropletsPerPulse = 5;
        root = new GameObject("Rising water " + id).transform; root.gameObject.layer = LayerMask.NameToLayer("TransparentFX");
        rise = new AnimationClip { name = "Rise " + id, frameRate = 60f };
        drain = new AnimationClip { name = "Drain " + id, frameRate = 60f };
        startup = new List<ParticleSystem>(); pulse = new List<ParticleSystem>(); meshPulse = new List<ParticleSystem>();
        PrepareMaterials(id);
        try
        {
            var effect = root.gameObject.AddComponent<RisingWaterVariantEffect>();
            var animator = root.gameObject.AddComponent<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            switch (index)
            {
                case 0:
                    Flow("Rear sheet", GabrielMesh("TsunamiWave02"), new Vector3(-.72f,0,.30f), new Vector3(1.05f,2.8f,.60f), new Vector3(0,16,-5));
                    Flow("Front sheet", GabrielMesh("TsunamiWave03"), new Vector3(.66f,0,-.25f), new Vector3(1.28f,2.6f,.65f), new Vector3(0,-22,5));
                    Stream(-.95f,.15f,20,.10f,-.22f); Stream(.9f,-.2f,24,.12f,.22f);
                    Cap(-.72f,2.25f,.3f,.85f); Cap(.66f,2.1f,-.25f,1f);
                    break;
                case 1:
                    for (int i=0;i<4;i++) { float x=(i-1.5f)*.78f; float h=i%2==0?2.3f:2.85f; Jet(x,i%2*.24f,.42f,h); Cap(x,h-.38f,i%2*.24f,.62f); Stream(x,0,12,.065f,(i-1.5f)*.09f); }
                    break;
                case 2:
                    Flow("Tidal fold left", PixMesh("water_mesh"), new Vector3(-.67f,0,.28f), new Vector3(1.24f,2.9f,.8f), new Vector3(0,25,-6));
                    Flow("Tidal fold right", PixMesh("water_mesh"), new Vector3(.72f,0,-.26f), new Vector3(1.3f,2.65f,.85f), new Vector3(0,190,8));
                    Cap(-.6f,2.2f,.2f,1.25f); Cap(.7f,2.05f,-.25f,1.2f); Stream(-1.05f,.1f,28,.12f,-.5f); Stream(1.05f,-.2f,28,.12f,.5f);
                    break;
                case 3:
                    for(int i=-2;i<=2;i++) { Jet(i*.5f,Mathf.Abs(i)*.10f,.36f,2.85f-Mathf.Abs(i)*.23f,i*-7); Stream(i*.5f,0,20,.12f,i*.24f); if(i%2==0) Cap(i*.5f,2.15f-Mathf.Abs(i)*.15f,0,.85f); }
                    break;
                case 4:
                    Jet(-.62f,.3f,.36f,2.8f); Jet(.60f,-.25f,.38f,2.65f); Jet(0,.08f,.28f,2.9f);
                    Stream(-.7f,.1f,65,.22f,.35f,true); Stream(.7f,-.12f,65,.22f,-.35f,true); Stream(0,.15f,45,.18f,0,true);
                    Cap(-.45f,2.15f,.15f,.8f); Cap(.5f,2.05f,-.15f,.85f);
                    break;
                case 5:
                    Flow("Fan left", GabrielMesh("TsunamiWave03"), new Vector3(-.65f,0,.25f), new Vector3(1.2f,2.65f,.7f), new Vector3(0,20,10));
                    Flow("Fan right", GabrielMesh("TsunamiWave03"), new Vector3(.65f,0,-.2f), new Vector3(1.28f,2.9f,.75f), new Vector3(0,165,-9));
                    Stream(-.9f,.05f,42,.18f,-.65f); Stream(.9f,-.15f,42,.18f,.65f); Cap(-.65f,2.1f,.15f,1.3f); Cap(.65f,2.35f,-.2f,1.3f);
                    break;
                case 6:
                    for(int i=-1;i<=1;i++) { Flow("Torn flow "+i,GabrielMesh("TsunamiWave02"),new Vector3(i*.98f,0,i*.18f),new Vector3(.72f,i==0?2.9f:2.55f,.5f),new Vector3(0,i*20,i*-4)); Stream(i*.98f,i*.18f,18,.08f,i*.15f); Cap(i*.98f,i==0?2.35f:2.0f,i*.18f,.68f); }
                    break;
                case 7:
                    for(int i=-1;i<=1;i++) { Jet(i*.96f,i==0?.2f:-.16f,.53f,i==0?2.9f:2.55f); Stream(i*.96f,-.16f,28,.13f,i*.12f); Band(i*.96f,i); Cap(i*.96f,i==0?2.35f:2.0f,-.16f,.7f); }
                    break;
                case 8:
                    for(int i=-1;i<=1;i++) { Flow("Forked surge "+i,PixMesh("funnel_mesh"),new Vector3(i*.85f,0,i*.1f),new Vector3(.7f,i==0?2.9f:2.55f,.65f),new Vector3(0,i*40,i*-7)); Stream(i*.85f,0,46,.14f,i*.6f); Cap(i*.85f,i==0?2.35f:2.0f,0,.9f); }
                    break;
                case 9:
                    for(int i=0;i<6;i++)
                    {
                        float angle=i*Mathf.PI/3f; float x=Mathf.Cos(angle)*1.05f; float z=Mathf.Sin(angle)*.50f;
                        Flow("Radial crest "+i,GabrielMesh("TsunamiWave02"),new Vector3(x,0,z),new Vector3(.72f,2.4f+(i%3)*.22f,.42f),new Vector3(0,i*60,Mathf.Cos(angle)*-8));
                        if(i%2==0){Cap(x,2.0f+(i%3)*.15f,z,.8f);Stream(x,z,26,.14f,Mathf.Cos(angle)*.5f);}
                    }
                    break;
            }
            Contact();
            animator.runtimeAnimatorController = Controller(id, SaveClip(rise, Root + "/Animation/" + id + "-rise.anim"), SaveClip(drain, Root + "/Animation/" + id + "-drain.anim"));
            var data = new SerializedObject(effect);
            SetArray(data.FindProperty("startupSystems"), startup); SetArray(data.FindProperty("pulseSystems"), pulse); SetArray(data.FindProperty("pulseMeshSystems"), meshPulse); data.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, Root + "/Prefabs/" + id + ".prefab");
            profile.prefab = prefab.GetComponent<RisingWaterVariantEffect>(); EditorUtility.SetDirty(profile);
            var wrapper = new GameObject("Preview " + id);
            try
            {
                var playback = wrapper.AddComponent<RisingWaterVariantPreview>();
                var serialized = new SerializedObject(playback); serialized.FindProperty("settings").objectReferenceValue = profile; serialized.ApplyModifiedPropertiesWithoutUndo();
                string previewPath = Root + "/Previews/" + id + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(wrapper, previewPath);
                string definitionPath = VfxTestingAuthoring.Register(previewPath, "fountain." + Keys[index], (index + 1).ToString("00") + " - " + Titles[index], Descriptions[index], action.action.duration + 0.35f, 0.1f);
                // Automatic capture preserves existing wording; this explicit revision updates only this family's English entries.
                var table = LocalizationEditorSettings.GetStringTableCollection("Diagnostics").GetTable("en") as StringTable;
                table.GetEntry("fountain." + Keys[index]).Value = (index + 1).ToString("00") + " - " + Titles[index];
                table.GetEntry("fountain." + Keys[index] + ".detail").Value = Descriptions[index];
                EditorUtility.SetDirty(table);
                var definition = AssetDatabase.LoadAssetAtPath<VfxPreviewDefinition>(definitionPath);
                definition.prefabControlsPlayback = true; definition.position = Vector3.down; EditorUtility.SetDirty(definition);
            }
            finally { UnityEngine.Object.DestroyImmediate(wrapper); }
        }
        finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }
    }

    private static void PrepareMaterials(string id)
    {
        water = CopyMaterial(Pix+"ElementalBeams/WaterBeam/Version_BuiltIn/Materials/WaterBeamMat.mat",id+"-water");
        water.SetFloat("_UseParticleSystemColor",0); water.SetColor("_Color_1",new Color(.025f,.10f,.48f,1)); water.SetColor("_Color_2",new Color(.035f,.95f,.78f,1));
        water.SetFloat("_AlphaBoost",1.35f); water.SetFloat("_Offset_Strength",.045f);
        water.SetFloat("_Cull",0); water.SetFloat("_BUILTIN_CullMode",0);
        water.SetVector("_Color_Scroll",new Vector4(.04f,-1.1f,0,0));
        foam = CopyMaterial(Pix+"ElementalAOE/WaterAOE/Version_BuiltIn/Materials/WaterAoeSplashMat.mat",id+"-foam");
        foam.SetFloat("_UseParticleSystemColor",0); foam.SetColor("_Color_1",new Color(.08f,.5f,.8f,1)); foam.SetColor("_Color_2",new Color(1.2f,1.4f,1.3f,1));
        foam.SetFloat("_AlphaBoost",1.7f); foam.SetVector("_Mask1_Scroll",new Vector4(.08f,.7f,0,0)); foam.SetFloat("_Offset_Strength",.035f);
        drops = CopyMaterial(Pix+"Components/Components_BuiltIn/SharedMaterials/DropsMat.mat",id+"-drops");
        drops.SetFloat("_UseParticleSystemColor",1); drops.SetFloat("_AlphaBoost",1); drops.SetFloat("_Blend",0); drops.SetFloat("_DstBlend",10); drops.renderQueue=3002;
        trail = CopyMaterial(Pix+"ElementalProjectiles/Waterball/Version_BuiltIn/WaterballProjectile/Materials/WaterBodyTrailMat.mat",id+"-trail");
        trail.SetFloat("_UseParticleSystemColor",0); trail.SetColor("_Color_1",new Color(.025f,.22f,.8f,1)); trail.SetColor("_Color_2",new Color(.035f,.95f,.78f,1)); trail.SetFloat("_Cull",0); trail.SetFloat("_BUILTIN_CullMode",0); trail.SetFloat("_AlphaBoost",1.1f); trail.SetFloat("_Offset_Strength",.025f);
        foreach(var m in new[]{water,foam,drops,trail}) EditorUtility.SetDirty(m);
    }

    private static void Jet(float x,float z,float width,float height,float lean=0)
        => Flow("Upward jet "+lobeCount,PixMesh("capsule_mesh"),new Vector3(x,0,z),new Vector3(width,height,width*.9f),new Vector3(0,0,lean));

    private static ParticleSystem Flow(string name,Mesh mesh,Vector3 position,Vector3 size,Vector3 angles,Material material=null,bool onPulse=false)
    {
        lobeCount++;
        var pivot=Child(root,name); pivot.localPosition=position; pivot.localRotation=Quaternion.Euler(angles);
        var template=AssetDatabase.LoadAssetAtPath<GameObject>(BeamPath).transform.Find("BeamBody/BeamBody").GetComponent<ParticleSystem>();
        var ps=CopySystem(template,pivot,"Flow surface");
        mesh=GroundedMesh(mesh,size,(candidate+1).ToString("00")+"-"+lobeCount);
        ps.transform.localScale=Vector3.one; ps.transform.localPosition=Vector3.zero; ps.transform.localRotation=Quaternion.identity;
        var main=ps.main; main.startSize3D=false; main.startSize=1; main.startSpeed=0; main.startLifetime=onPulse?.42f:1.5f; main.startDelay=onPulse?0:.035f;
        main.startRotation3D=true; main.startRotationX=0; main.startRotationY=0; main.startRotationZ=0; main.startColor=Color.white; main.simulationSpace=ParticleSystemSimulationSpace.Local; main.maxParticles=onPulse?8:2;
        var shape=ps.shape; shape.enabled=false;
        var v=ps.velocityOverLifetime;v.enabled=false;
        var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(onPulse?Array.Empty<ParticleSystem.Burst>():new[]{new ParticleSystem.Burst(0,1)});
        var sol=ps.sizeOverLifetime;sol.enabled=true;sol.separateAxes=true;
        sol.x=new ParticleSystem.MinMaxCurve(1,Curve(0,.45f,.15f,.95f,.8f,1,1,1.05f));
        sol.z=new ParticleSystem.MinMaxCurve(1,Curve(0,.45f,.15f,.95f,.8f,1,1,1.05f));
        sol.y=new ParticleSystem.MinMaxCurve(1,onPulse?Curve(0,.04f,.25f,.8f,.55f,1,1,.45f):Curve(0,.01f,.08f,.32f,.16f,.88f,.23f,1,.8f,1,1,.85f));
        var color=ps.colorOverLifetime;color.enabled=true;color.color=FadeGradient(onPulse?new Color(.65f,1,.9f):Color.white);
        var r=ps.GetComponent<ParticleSystemRenderer>();r.mesh=mesh;r.sharedMaterial=material!=null?material:water;r.renderMode=ParticleSystemRenderMode.Mesh;
        r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
        if(onPulse) meshPulse.Add(ps);else startup.Add(ps);
        string p=AnimationUtility.CalculateTransformPath(ps.transform,root);
        float alpha=r.sharedMaterial.GetFloat("_AlphaBoost");
        Float(rise,p,typeof(ParticleSystemRenderer),"material._AlphaBoost",Scaled(Curve(0,0,.12f,.8f,.28f,1,1.1f,1,1.5f,0,1.6f,0),alpha));
        Float(drain,p,typeof(ParticleSystemRenderer),"material._AlphaBoost",Scaled(Curve(0,.8f,.18f,0,.22f,0),alpha));
        return ps;
    }
    private static void Cap(float x,float y,float z,float width)
        => Flow("Detached crest "+lobeCount,PixMesh("hemisphere_2"),new Vector3(x,y,z),new Vector3(width,.55f,.65f),Vector3.zero,foam,true);

    private static void Stream(float x,float z,float rate,float width,float outward,bool spiral=false)
    {
        var t=Child(root,"Simulated stream "+lobeCount++);t.localPosition=new Vector3(x,.04f,z);t.localRotation=Quaternion.identity;
        var ps=t.gameObject.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.playOnAwake=false;main.loop=false;main.duration=1.25f;main.startLifetime=new ParticleSystem.MinMaxCurve(.42f,.62f);
        main.startSpeed=new ParticleSystem.MinMaxCurve(4.0f,5.2f);main.gravityModifier=.6f;main.startSize=new ParticleSystem.MinMaxCurve(.03f,.065f);main.startColor=new Color(.08f,.6f,.92f,1);main.maxParticles=100;main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Hierarchy;
        var emission=ps.emission;emission.rateOverTime=new ParticleSystem.MinMaxCurve(rate,Curve(0,0,.035f,1,.82f,1,.91f,0,1,0));
        var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=spiral?8:13;shape.radius=spiral?.16f:.1f;shape.rotation=new Vector3(-90,0,0);
        var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;
        velocity.x=new ParticleSystem.MinMaxCurve(1,Curve(0,0,.45f,outward*.45f,1,outward));
        velocity.y=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Constant(0,1,0));
        velocity.z=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Constant(0,1,0));
        if(spiral)
        {
            velocity.orbitalX=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Constant(0,1,0));
            velocity.orbitalY=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Constant(0,1,5.2f));
            velocity.orbitalZ=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Constant(0,1,0));
            velocity.radial=.4f;
        }
        var color=ps.colorOverLifetime;color.enabled=true;color.color=FadeGradient(new Color(.16f,1,.8f));
        var trails=ps.trails;trails.enabled=true;trails.ratio=1;trails.lifetime=.14f;trails.minVertexDistance=.025f;trails.worldSpace=true;trails.dieWithParticles=false;trails.sizeAffectsWidth=false;trails.widthOverTrail=new ParticleSystem.MinMaxCurve(width,Curve(0,0,.2f,1,.7f,.7f,1,0));trails.colorOverTrail=FadeGradient(Color.white);
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=1.5f;renderer.velocityScale=.08f;renderer.sharedMaterial=drops;renderer.trailMaterial=trail;
        ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(131+candidate*53+lobeCount*7);startup.Add(ps);
        var spray=Child(root,"Pulse droplets "+lobeCount++);spray.localPosition=new Vector3(x,.06f,z);spray.localRotation=Quaternion.Euler(0,0,-outward*20);
        var burst=UnityEngine.Object.Instantiate(ps.gameObject,spray,false);burst.name="Ballistic spray";burst.transform.localPosition=Vector3.zero;burst.transform.localRotation=Quaternion.identity;
        var bp=burst.GetComponent<ParticleSystem>();var be=bp.emission;be.rateOverTime=0;be.SetBursts(Array.Empty<ParticleSystem.Burst>());
        var bm=bp.main;bm.startSpeed=new ParticleSystem.MinMaxCurve(4.3f,5.6f);bm.startLifetime=new ParticleSystem.MinMaxCurve(.3f,.5f);
        pulse.Add(bp);
    }

    private static void Band(float x,int stream)
    {
        var ps=Flow("Ascending foam band "+stream,PixMesh("hemisphere_2"),new Vector3(x,.1f,-.22f),new Vector3(.75f,.32f,.55f),Vector3.zero,foam,true);
        var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;
        velocity.x=new ParticleSystem.MinMaxCurve(0);velocity.y=new ParticleSystem.MinMaxCurve(4.7f);velocity.z=new ParticleSystem.MinMaxCurve(0);
        var main=ps.main;main.startLifetime=.5f;
    }

    private static void Contact()
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(AoePath).transform.Find("WaterAoeBody").GetComponent<ParticleSystem>();
        var ps=CopySystem(source,root,"Ground water wake");ps.transform.localPosition=new Vector3(0,.015f,0);ps.transform.localRotation=Quaternion.identity;ps.transform.localScale=new Vector3(1.35f,.65f,.55f);
        var main=ps.main;main.startLifetime=.6f;main.startDelay=0;main.startColor=new Color(.05f,.45f,.85f,.7f);
        var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(.05f,1),new ParticleSystem.Burst(.5f,1),new ParticleSystem.Burst(.94f,1)});
        var material=CopyMaterial(Pix+"ElementalAOE/WaterAOE/Version_BuiltIn/Materials/WaterAoeMat.mat",(candidate+1).ToString("00")+"-wake");
        material.SetFloat("_OffsetStrength",.08f);ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;EditorUtility.SetDirty(material);startup.Add(ps);
    }
    private static ParticleSystem CopySystem(ParticleSystem source,Transform parent,string name)
    {
        var copy=UnityEngine.Object.Instantiate(source.gameObject,parent,false);copy.name=name;
        foreach(Transform child in copy.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        foreach(var script in copy.GetComponents<MonoBehaviour>())UnityEngine.Object.DestroyImmediate(script);
        var ps=copy.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.playOnAwake=false;main.loop=false;main.duration=1.25f;main.startDelay=0;main.scalingMode=ParticleSystemScalingMode.Hierarchy;
        ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(541+candidate*31+lobeCount);return ps;
    }
    private static Gradient FadeGradient(Color color)
    {
        var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(color,0),new GradientColorKey(Color.Lerp(color,Color.white,.7f),.55f),new GradientColorKey(new Color(.13f,.7f,.68f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.9f,.12f),new GradientAlphaKey(.8f,.62f),new GradientAlphaKey(0,1)});return g;
    }
    private static Material CopyMaterial(string source,string name)
    {
        string path=Root+"/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        var template=AssetDatabase.LoadAssetAtPath<Material>(source);if(template==null)throw new InvalidOperationException("Missing water material: "+source);
        if(material==null){material=new Material(template);AssetDatabase.CreateAsset(material,path);}else EditorUtility.CopySerialized(template,material);
        material.name=name;EditorUtility.SetDirty(material);return material;
    }
    private static Mesh GroundedMesh(Mesh source,Vector3 size,string id)
    {
        // Rebase a project-owned authored mesh once; particles then scale from the contact point.
        var mesh=UnityEngine.Object.Instantiate(source);mesh.name=id;
        var b=source.bounds;var origin=new Vector3(b.center.x,b.min.y,b.center.z);
        var factor=new Vector3(size.x/Mathf.Max(.01f,b.size.x),size.y/Mathf.Max(.01f,b.size.y),size.z/Mathf.Max(.01f,b.size.z));
        mesh.vertices=source.vertices.Select(v=>Vector3.Scale(v-origin,factor)).ToArray();mesh.RecalculateBounds();mesh.RecalculateNormals();
        string path=Root+"/Meshes/"+id+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);return old;
    }
    private static Mesh PixMesh(string name)=>AssetDatabase.LoadAllAssetsAtPath(Pix+"Components/Meshes/"+name+".fbx").OfType<Mesh>().First();
    private static Mesh GabrielMesh(string name)=>AssetDatabase.LoadAllAssetsAtPath("Assets/GabrielAguiarProductions/Models/"+name+".fbx").OfType<Mesh>().First();
    private static Texture2D Texture(string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Pix+"Components/Textures/"+name+".png");

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
    private static AnimationCurve Curve(params float[] pairs)
    {
        var curve = new AnimationCurve(Enumerable.Range(0, pairs.Length / 2).Select(i => new Keyframe(pairs[i * 2], pairs[i * 2 + 1])).ToArray());
        for (int i = 0; i < curve.length; i++) { AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto); AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto); }
        return curve;
    }
    private static AnimationCurve Scaled(AnimationCurve curve, float value) => new AnimationCurve(curve.keys.Select(k => new Keyframe(k.time, k.value * value, k.inTangent * value, k.outTangent * value)).ToArray());
    private static void Float(AnimationClip clip, string path, Type type, string property, AnimationCurve curve) => AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, property), curve);
    private static void SetArray(SerializedProperty property, List<ParticleSystem> values)
    { property.arraySize = values.Count; for (int i = 0; i < values.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
    private static AnimationClip SaveClip(AnimationClip clip, string path)
    {
        clip.name = System.IO.Path.GetFileNameWithoutExtension(path);
        var old = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (old == null) { AssetDatabase.CreateAsset(clip, path); return clip; }
        EditorUtility.CopySerialized(clip, old); UnityEngine.Object.DestroyImmediate(clip); EditorUtility.SetDirty(old); return old;
    }
    private static AnimatorController Controller(string id, AnimationClip riseClip, AnimationClip drainClip)
    {
        string path = Root + "/Animation/" + id + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.parameters = new[] { new AnimatorControllerParameter { name = "Drain", type = AnimatorControllerParameterType.Trigger }, new AnimatorControllerParameter { name = "Phase", type = AnimatorControllerParameterType.Float } };
        var machine = controller.layers[0].stateMachine;
        foreach (var transition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(transition);
        foreach (var state in machine.states) machine.RemoveState(state.state);
        var start = machine.AddState("Rise"); start.motion = riseClip; start.writeDefaultValues = false; machine.defaultState = start;
        start.timeParameterActive = true; start.timeParameter = "Phase";
        var end = machine.AddState("Drain"); end.motion = drainClip; end.writeDefaultValues = false;
        var stop = machine.AddAnyStateTransition(end); stop.hasExitTime = false; stop.duration = .03f; stop.canTransitionToSelf = false; stop.AddCondition(AnimatorConditionMode.If, 0, "Drain");
        EditorUtility.SetDirty(controller); return controller;
    }
}
