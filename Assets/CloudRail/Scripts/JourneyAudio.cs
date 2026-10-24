using UnityEngine;

namespace CloudRail
{
    // One scene-owned soundscape; source gain follows the saved settings every frame.
    public sealed class JourneyAudio : MonoBehaviour
    {
        public JourneyGame game;
        public AudioClip theme,wind,rail,whistle,awakening;
        AudioSource musicSource,windSource,railSource,whistleSource,awakeningSource,waterfallSource;
        AudioSource[] worldSources;
        bool wasMoving,wasEnding,worldPaused;
        public AudioSource WaterfallSource => waterfallSource;
        public AudioSource MusicSource => musicSource;
        public AudioSource RailSource => railSource;
        public AudioSource WhistleSource => whistleSource;
        public int DeparturesHeard { get; private set; }
        public int AwakeningsHeard { get; private set; }
        AudioSource Source(string label,AudioClip clip,bool loop,Transform parent)
        {
            var go=new GameObject(label);go.transform.SetParent(parent,false);
            var source=go.AddComponent<AudioSource>();source.clip=clip;source.loop=loop;
            source.playOnAwake=false;source.volume=0;source.dopplerLevel=0;
            source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=8;source.maxDistance=65;
            if(loop&&clip!=null)source.Play();return source;
        }
        void Start()
        {
            musicSource=Source("Original score · 96 second loop",theme,true,transform);
            windSource=Source("Cloud wind",wind,true,transform);
            railSource=Source("Wheels on the viaduct",rail,true,game.train.rig.locomotive);
            whistleSource=Source("Departure whistle",whistle,false,game.train.rig.locomotive);
            awakeningSource=Source("Clock awakening",awakening,false,transform);
            if(game.world!=null&&game.world.waterfallClip!=null)
            {waterfallSource=Source("Waterfall at the island edge",game.world.waterfallClip,true,game.world.waterfallOrigin);waterfallSource.spatialBlend=1;waterfallSource.minDistance=3;waterfallSource.maxDistance=38;}
            worldSources=new[]{windSource,railSource,whistleSource,awakeningSource};
            musicSource.priority=64;railSource.priority=100;
            wasMoving=game.train.Moving;wasEnding=game.EndSequencePlaying;
        }
        void LateUpdate()
        {
            if(musicSource==null)return;
            float dt=Time.unscaledDeltaTime;
            float moving=game.voyage!=null&&game.voyage.Active?Mathf.Clamp01(game.voyage.Speed/5.2f):Mathf.Clamp01(game.train.Speed/game.train.cruiseSpeed);
            musicSource.volume=Mathf.MoveTowards(musicSource.volume,game.MusicVolume*(game.Paused?.22f:game.EndSequencePlaying?.38f:.7f),dt*.25f);
            if(waterfallSource!=null)waterfallSource.volume=game.EffectsVolume*.32f;
            windSource.volume=game.EffectsVolume*(game.Onboard?.045f:.035f);
            railSource.spatialBlend=game.Onboard?0:1;
            whistleSource.spatialBlend=game.Onboard?0:.85f;
            railSource.volume=game.EffectsVolume*moving*.28f;
            railSource.pitch=Mathf.Lerp(.65f,1.12f,moving);
            whistleSource.volume=game.EffectsVolume*.25f;
            awakeningSource.volume=game.EffectsVolume*.42f;
            if((game.Paused||Time.timeScale==0)!=worldPaused)
            {
                worldPaused=game.Paused||Time.timeScale==0;
                foreach(var source in worldSources)
                    if(worldPaused)source.Pause();else source.UnPause();
                if(waterfallSource!=null){if(worldPaused)waterfallSource.Pause();else waterfallSource.UnPause();}
            }
            if(game.train.Moving&&!wasMoving&&!game.Paused){whistleSource.Play();DeparturesHeard++;}
            if(game.EndSequencePlaying&&!wasEnding&&!game.Paused){awakeningSource.Play();AwakeningsHeard++;}
            wasMoving=game.train.Moving;wasEnding=game.EndSequencePlaying;
        }
        public void StarBurst(Vector3 point)
        {
            var go=new GameObject("Starlight collected");go.transform.position=point;
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=false;main.duration=.12f;main.startLifetime=new ParticleSystem.MinMaxCurve(.45f,.85f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(.7f,1.5f);main.startSize=new ParticleSystem.MinMaxCurve(.055f,.13f);
            main.startColor=new Color(1,.8f,.37f,.9f);main.gravityModifier=-.12f;main.maxParticles=24;
            var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,18)});
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.12f;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial=game.train.rig.steam.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            ps.Play();Destroy(go,2);
        }
    }
}
