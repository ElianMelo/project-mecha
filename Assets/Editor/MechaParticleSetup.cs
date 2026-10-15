using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MechaParticleSetup
{
    private const string Folder="Assets/_Project/VFX/Materials";
    public static void Apply()
    {
        MechaPresentationSetup.Folder(Folder);
        var windTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/VFX/Textures/SpeedWind.png");
        var builtin=AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
        var smokeTexture=builtin!=null?builtin.mainTexture:windTexture;
        var smoke=Material("Smoke",smokeTexture,false);var wind=Material("Wind",windTexture,true);var spark=Material("Sparks",windTexture,true);
        var vfx=MechaPresentationSetup.Profile<CombatVfxSettings>("CombatVfx");
        if(vfx.dashSmoke==null)
        {
            vfx.dashSmoke=Burst("DashSmoke",smoke,7,new Vector2(0.24f,0.5f),new Vector2(1.8f,3.4f),new Vector2(0.18f,0.36f),new Color(0.67f,0.7f,0.75f,0.68f));
            vfx.dashSmoke.growth=2.1f;vfx.dashSmoke.spread=0.35f;vfx.dashSmoke.drag=3f;
            vfx.wind=Burst("AttackWind",wind,6,new Vector2(0.08f,0.2f),new Vector2(3f,5f),new Vector2(0.12f,0.28f),new Color(0.85f,0.92f,1f,0.85f));
            vfx.wind.renderMode=ParticleSystemRenderMode.Stretch;vfx.wind.lengthScale=1.4f;vfx.wind.spread=0.4f;
            vfx.hitSparks=Burst("HitSparks",spark,11,new Vector2(0.08f,0.21f),new Vector2(3.5f,7.5f),new Vector2(0.045f,0.09f),new Color(1f,0.85f,0.5f));
            vfx.hitSparks.renderMode=ParticleSystemRenderMode.Stretch;vfx.hitSparks.lengthScale=1.4f;vfx.hitSparks.spread=0.8f;
            vfx.hitSmoke=Burst("HitSmoke",smoke,6,new Vector2(0.16f,0.35f),new Vector2(1f,2.5f),new Vector2(0.12f,0.25f),new Color(0.63f,0.67f,0.75f,0.65f));
            vfx.hitSmoke.growth=2.2f;
            vfx.skillWind=Burst("SkillWind",wind,14,new Vector2(0.12f,0.28f),new Vector2(4f,7f),new Vector2(0.2f,0.45f),new Color(0.65f,0.85f,1f,0.9f));
            vfx.skillWind.renderMode=ParticleSystemRenderMode.Stretch;vfx.skillWind.lengthScale=1.8f;
            foreach(var profile in new[]{vfx.dashSmoke,vfx.wind,vfx.hitSparks,vfx.hitSmoke,vfx.skillWind})EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(vfx);
        }
        if (vfx.dashStreaks == null)
        {
            vfx.dashStreaks=Burst("DashStreaks",wind,3,new Vector2(0.06f,0.13f),new Vector2(5f,9f),new Vector2(0.035f,0.065f),new Color(0.78f,0.9f,1f,0.8f));
            vfx.dashStreaks.renderMode=ParticleSystemRenderMode.Stretch;vfx.dashStreaks.lengthScale=2.2f;vfx.dashStreaks.emissionRadius=0.38f;vfx.dashStreaks.spread=0.1f;
            vfx.dashDebris=Burst("DashDebris",smoke,5,new Vector2(0.13f,0.3f),new Vector2(2f,4.5f),new Vector2(0.035f,0.085f),new Color(0.62f,0.7f,0.78f));
            vfx.dashDebris.gravity=1.1f;vfx.dashDebris.angularSpeed=new Vector2(-280f,280f);vfx.dashDebris.spread=0.6f;vfx.dashDebris.growth=0.5f;
            vfx.slashEdges=Burst("SlashEdges",wind,5,new Vector2(0.06f,0.15f),new Vector2(4.5f,8f),new Vector2(0.035f,0.07f),new Color(0.8f,0.94f,1f,0.92f));
            vfx.slashEdges.renderMode=ParticleSystemRenderMode.Stretch;vfx.slashEdges.lengthScale=2.2f;vfx.slashEdges.emissionRadius=0.22f;vfx.slashEdges.spread=0.65f;
            vfx.hitCore=Burst("HitCore",spark,2,new Vector2(0.045f,0.08f),new Vector2(0.1f,0.4f),new Vector2(0.18f,0.3f),new Color(1f,0.96f,0.8f));
            vfx.hitCore.growth=0.1f;vfx.hitCore.spread=1f;vfx.hitCore.emissionRadius=0.02f;
            vfx.hitDebris=Burst("HitDebris",spark,7,new Vector2(0.12f,0.32f),new Vector2(4f,8f),new Vector2(0.025f,0.055f),new Color(1f,0.55f,0.18f));
            vfx.hitDebris.renderMode=ParticleSystemRenderMode.Stretch;vfx.hitDebris.lengthScale=1.1f;vfx.hitDebris.spread=1.2f;vfx.hitDebris.gravity=0.6f;vfx.hitDebris.trails=true;vfx.hitDebris.growth=0.3f;
            vfx.landingSmoke=Burst("LandingSmoke",smoke,5,new Vector2(0.15f,0.32f),new Vector2(1.4f,2.7f),new Vector2(0.13f,0.26f),new Color(0.65f,0.72f,0.8f,0.7f));
            vfx.landingSmoke.growth=2f;vfx.landingSmoke.drag=3.5f;vfx.landingSmoke.spread=0.3f;
            vfx.dashSmoke.count=9;vfx.dashSmoke.noiseStrength=0.18f;vfx.dashSmoke.endTint=new Color(0.55f,0.65f,0.8f);
            vfx.hitSmoke.noiseStrength=0.22f;vfx.hitSmoke.angularSpeed=new Vector2(-100f,100f);
            foreach (var profile in new[] {vfx.dashStreaks,vfx.dashDebris,vfx.slashEdges,vfx.hitCore,vfx.hitDebris,vfx.landingSmoke,vfx.dashSmoke,vfx.hitSmoke}) EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(vfx);
        }
    }
    private static ParticleEffectSettings Burst(string name,Material mat,int count,Vector2 life,Vector2 speed,Vector2 size,Color color)
    {
        var profile=MechaPresentationSetup.Create<ParticleEffectSettings>(name);profile.material=mat;profile.count=count;
        profile.lifetime=life;profile.speed=speed;profile.size=size;profile.color=color;return profile;
    }
    private static Material Material(string name,Texture texture,bool additive)
    {
        string path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        bool created=mat==null;
        if(created)mat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")){name=name};
        mat.SetTexture("_BaseMap",texture);mat.SetTexture("_MainTex",texture);mat.SetColor("_BaseColor",Color.white);
        mat.SetFloat("_Surface",1f);mat.SetFloat("_Blend",additive?2f:0f);mat.SetFloat("_Cull",0f);
        mat.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);mat.SetInt("_DstBlend",(int)(additive?BlendMode.One:BlendMode.OneMinusSrcAlpha));
        mat.SetInt("_ZWrite",0);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=3000;
        if(created)AssetDatabase.CreateAsset(mat,path);else EditorUtility.SetDirty(mat);
        return mat;
    }
}
