using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CloudRail
{
    [DefaultExecutionOrder(200)]
    public sealed class SkyVoyage : MonoBehaviour
    {
        public JourneyGame game;
        public UnfoldingSkyway skyway;
        public GameObject overlay;
        public TMP_Text caption;
        public bool startInScenicMode=true;
        public float Speed=5.2f;
        public bool Active {get;private set;}
        public float Distance {get;private set;}
        int view;float wheelAngle,orbit,zoom=1;
        bool paused;
        Vector3 cameraVelocity;
        Vector3[] rods;
        void Start()
        {
            rods=new Vector3[game.train.rig.connectingRods.Length];for(int i=0;i<rods.Length;i++)rods[i]=game.train.rig.connectingRods[i].localPosition;
            if(startInScenicMode)Enter();else overlay.SetActive(false);
        }
        public void Enter()
        {
            Time.timeScale=1;paused=false;Active=true;Distance=game.train.rig.path.StationDistance(0);
            game.enabled=false;game.train.enabled=false;game.journeyCamera.enabled=false;
            game.player.gameObject.SetActive(false);game.hud.gameObject.SetActive(false);overlay.SetActive(true);
            cameraVelocity=Vector3.zero;Pose(true);
        }
        public void Explore()
        {
            Active=false;paused=false;Time.timeScale=1;overlay.SetActive(false);
            game.enabled=true;game.train.enabled=true;game.journeyCamera.enabled=true;game.player.gameObject.SetActive(true);game.hud.gameObject.SetActive(true);
            game.RestoreExploration();
        }
        void Update()
        {
            var kb=Keyboard.current;
            if(kb!=null&&kb.tabKey.wasPressedThisFrame){if(Active)Explore();else Enter();}
            if(!Active)return;
            if(kb!=null&&kb.cKey.wasPressedThisFrame)view=(view+1)%3;
            if(kb!=null&&(kb.spaceKey.wasPressedThisFrame||kb.escapeKey.wasPressedThisFrame)){paused=!paused;Time.timeScale=paused?0:1;}
            var mouse=Mouse.current;
            if(mouse!=null)
            {
                if(mouse.rightButton.isPressed)orbit+=mouse.delta.ReadValue().x*.13f;
                zoom=Mathf.Clamp(zoom-mouse.scroll.ReadValue().y*.0018f+(mouse.middleButton.isPressed?mouse.delta.ReadValue().y*.004f:0),.45f,2f);
            }
            Distance+=Speed*Time.deltaTime;wheelAngle+=Speed*Time.deltaTime/.44f*Mathf.Rad2Deg;
            foreach(var wheel in game.train.rig.wheels)wheel.localRotation=Quaternion.AngleAxis(wheelAngle,Vector3.right);
            float phase=wheelAngle*Mathf.Deg2Rad;
            for(int i=0;i<rods.Length;i++)game.train.rig.connectingRods[i].localPosition=rods[i]+new Vector3(0,.19f*Mathf.Cos(phase),.19f*Mathf.Sin(phase));
            if(game.stations[2].towerRing!=null)game.stations[2].towerRing.Rotate(0,Time.deltaTime*10,0);
            var emission=game.train.rig.steam.emission;emission.rateOverTime=paused?1:5;
            caption.text=paused?"云海暂停 · 空格继续":"随光铺开的云端铁道";
        }
        void LateUpdate(){if(Active)Pose(false);}
        void Pose(bool snap)
        {
            var rig=game.train.rig;rig.path.Evaluate(Distance,out var p,out var q);rig.locomotive.SetPositionAndRotation(p,q);
            rig.path.Evaluate(Distance-rig.carriageSpacing,out p,out q);rig.carriage.SetPositionAndRotation(p,q);
            var train=rig.locomotive;Vector3 target=train.position+Vector3.up*2+train.forward*7;
            Vector3 offset=view==0?new Vector3(19,12,-19):view==1?new Vector3(24,8,-5):new Vector3(30,23,-30);
            offset=Quaternion.Euler(0,orbit,0)*offset*zoom;
            Vector3 cameraPosition=train.position+train.rotation*offset;
            if(view==0){target=new Vector3(8,8,3)+train.position*.025f;cameraPosition=target+Quaternion.Euler(0,orbit,0)*new Vector3(64,41,-83)*zoom;}
            var cam=Camera.main.transform;
            cam.position=snap?cameraPosition:Vector3.SmoothDamp(cam.position,cameraPosition,ref cameraVelocity,.65f,float.PositiveInfinity,Time.deltaTime);
            if((target-cam.position).sqrMagnitude>.01f)cam.rotation=snap?Quaternion.LookRotation(target-cam.position):Quaternion.Slerp(cam.rotation,Quaternion.LookRotation(target-cam.position),1-Mathf.Exp(-Time.deltaTime*4));
        }
        void OnDisable(){if(Active)Time.timeScale=1;}
    }
}
