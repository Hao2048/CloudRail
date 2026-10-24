using TMPro;
using UnityEngine;

namespace CloudRail
{
    public sealed class JourneyWorld : MonoBehaviour
    {
        public JourneyGame game;
        public Renderer[] firstRoute,secondRoute,memorialLamps,clockLights;
        public Transform[] firstGate,secondGate;
        public TMP_Text[] deviceLabels;
        public TMP_Text memorialLabel;
        public Light clockGlow;
        public ParticleSystem petals,waterMist;
        public GameObject[] cloudBanks;
        public AudioClip waterfallClip;
        public Transform waterfallOrigin;
        public bool MemorialLit => game.StarCount==9;
        public int FirstRoutePulses { get; private set; }
        public int SecondRoutePulses { get; private set; }
        float pulse0=-100,pulse1=-100;
        bool route0,route1,memorial,low;
        bool initialized;
        MaterialPropertyBlock block;
        void OnEnable(){block=new MaterialPropertyBlock();}
        void Start(){Refresh(true);}
        public void PulseRoute(int index)
        {
            if(index==0){pulse0=Time.time;FirstRoutePulses++;}else{pulse1=Time.time;SecondRoutePulses++;}
        }
        void Tint(Renderer[] renderers,bool on,float strength=1)
        {
            if(renderers==null)return;
            if(block==null)block=new MaterialPropertyBlock();
            foreach(var r in renderers)
            {
                if(r==null)continue;
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor",on?new Color(1,.82f,.36f):new Color(.19f,.35f,.4f));
                block.SetColor("_EmissionColor",on?new Color(1,.59f,.17f)*strength:Color.black);
                r.SetPropertyBlock(block);
            }
        }
        void Gate(Transform[] gates,bool on)
        {
            if(gates==null)return;
            for(int i=0;i<gates.Length;i++)gates[i].localRotation=Quaternion.Euler(0,0,(on?50:7)*(i%2==0?1:-1));
        }
        void Refresh(bool force=false)
        {
            bool a=game.GardenOpen,b=game.ClockOpen,c=MemorialLit;
            if(force||route0!=a){Tint(firstRoute,a,.45f);Gate(firstGate,a);route0=a;}
            if(force||route1!=b){Tint(secondRoute,b,.45f);Gate(secondGate,b);route1=b;}
            if(force||memorial!=c){Tint(memorialLamps,c,1.5f);memorial=c;if(memorialLabel!=null)memorialLabel.text=c?"九星旅人\n云端纪念灯已点亮":"旅人纪念灯\n集齐九枚星光";}
            if(force||low!=game.LowEffects)
            {
                low=game.LowEffects;
                if(cloudBanks!=null)for(int i=0;i<cloudBanks.Length;i++)cloudBanks[i].SetActive(!low||i%2==0);
                if(petals!=null){var emission=petals.emission;emission.rateOverTime=low?2:7;}
                if(waterMist!=null){var emission=waterMist.emission;emission.rateOverTime=low?2:5;}
                Application.targetFrameRate=low?30:60;
            }
            if(deviceLabels!=null&&deviceLabels.Length==3)
            {
                deviceLabels[0].text=a?"航线已开放\n前往花园岛":"星光航线装置\n需要三枚星光";
                deviceLabels[1].text=b?"航线已开放\n前往星钟岛":"星光航线装置\n需要六枚星光";
                deviceLabels[2].text=game.Completed?"云端信标已点亮":"钟塔星光装置\n沿步道抵达后按 E";
            }
            initialized=true;
        }
        void Pulse(Renderer[] lamps,float since)
        {
            if(since<0||since>6)return;
            if(block==null)block=new MaterialPropertyBlock();
            // A brief wave travels toward the newly unlocked station; settled lights stay constant.
            for(int i=0;i<lamps.Length;i++)
            {
                float progress=(float)(i/2)/Mathf.Max(1,lamps.Length/2-1);
                float wave=Mathf.Exp(-Mathf.Pow((since/4-progress)*7,2))*2.5f;
                var r=lamps[i];r.GetPropertyBlock(block);block.SetColor("_EmissionColor",new Color(1,.59f,.17f)*(.45f+wave));r.SetPropertyBlock(block);
            }
        }
        void Update()
        {
            if(!initialized)return;Refresh();Pulse(firstRoute,Time.time-pulse0);Pulse(secondRoute,Time.time-pulse1);
            float power=game.Completed?1:game.EndSequencePlaying?Mathf.SmoothStep(0,1,game.EndingProgress):0;
            Tint(clockLights,power>0,power*2.4f);
            if(clockGlow!=null)clockGlow.intensity=power*3;
        }
    }
}
