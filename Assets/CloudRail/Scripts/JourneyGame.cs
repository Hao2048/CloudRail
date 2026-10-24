using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CloudRail
{
    public sealed class JourneyGame : MonoBehaviour
    {
        public JourneyStation[] stations;
        public JourneyStar[] stars;
        public JourneyPlayer player;
        public JourneyTrain train;
        public JourneyCamera journeyCamera;
        public JourneyHUD hud;
        public JourneyAudio soundscape;
        public JourneyWorld world;
        public SkyVoyage voyage;
        public string saveFile = "cloudrail_journey_v1.json";
        public int StarCount => collected.Count;
        public int CurrentStation => data.safeStation;
        public bool Onboard { get; private set; }
        public bool Paused { get; private set; }
        public bool MenuOpen => helpOpen || !started || destinationsOpen || endingOpen || restartOpen;
        public bool CanWalk => started && !Paused && !MenuOpen && !Onboard && !endingPlaying;
        public bool CanCollect => CanWalk;
        public bool Completed => data.completed;
        public bool GardenOpen => data.gardenLine;
        public bool ClockOpen => data.clockLine;
        public bool ReduceMotion => data.reduceMotion;
        public bool LowEffects => data.lowEffects;
        public float EndingProgress => Mathf.Clamp01(endingTime/6);
        public float Sensitivity => data.sensitivity;
        public float MusicVolume => data.musicVolume;
        public float EffectsVolume => data.effectsVolume;
        public string SavePath => Path.Combine(Application.persistentDataPath, saveFile);
        public bool EndSequencePlaying => endingPlaying;
        public bool SaveBlocked => saveBlocked;
        JourneySave data = new JourneySave();
        readonly HashSet<string> collected = new HashSet<string>();
        bool started, destinationsOpen, endingOpen, restartOpen, endingPlaying, saveBlocked, helpOpen;
        string notice;
        float endingTime;
        AudioSource audioSource;
        AudioClip collectSound, unlockSound, arriveSound;

        void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;
            collectSound=Tone(880,1320,.22f);unlockSound=Tone(440,880,.48f);arriveSound=Tone(660,550,.38f);
        }
        void Start()
        {
            data=JourneySaveStore.Read(SavePath,out notice,out saveBlocked);
            var known=new HashSet<string>();foreach(var s in stars)known.Add(s.starId);
            foreach(string id in data.stars)if(known.Contains(id))collected.Add(id);
            // Restoring at a station must never bypass a locked route.
            if(!data.gardenLine)data.safeStation=0;
            else if(!data.clockLine)data.safeStation=Mathf.Min(data.safeStation,1);
            started=File.Exists(SavePath)&&!saveBlocked;
            ApplyState();
            hud.InitializeSettings(data.effectsVolume,data.sensitivity,data.musicVolume,data.reduceMotion,data.lowEffects);
            if(data.journeyInTransit&&!saveBlocked){data.journeyInTransit=false;notice="上次旅程尚未完成，已返回出发站安全月台";Save();}
            hud.SetIntro(!started);
            if(!string.IsNullOrEmpty(notice))hud.Toast(notice,8);
        }
        void ApplyState()
        {
            foreach(var s in stars)s.gameObject.SetActive(!collected.Contains(s.starId));
            stations[0].SetSignal(data.gardenLine);stations[1].SetSignal(data.clockLine);stations[2].SetSignal(data.completed);
            train.Initialize(data.safeStation);Onboard=false;
            player.ShowModel();Respawn(false);
        }
        public void Begin()
        {
            started=true;hud.SetIntro(false);Save();
            if(!string.IsNullOrEmpty(notice))hud.Toast(notice,8);
        }
        void Update()
        {
            var kb=Keyboard.current;
            if(kb!=null&&kb.escapeKey.wasPressedThisFrame&&started)
            {
                if(helpOpen){CloseHelp();}
                else if(restartOpen){restartOpen=false;hud.SetRestart(false);}
                else if(destinationsOpen){CloseDestinations();}
                else if(endingOpen){ContinueExploring();}
                else TogglePause();
            }
            if(kb!=null&&kb.hKey.wasPressedThisFrame&&started&&!endingPlaying){if(helpOpen)CloseHelp();else OpenHelp();}
            if(!Paused&&started&&!MenuOpen&&!endingPlaying&&kb!=null)
            {
                if(kb.eKey.wasPressedThisFrame)Interact();
                if(kb.fKey.wasPressedThisFrame&&Onboard&&!train.Moving)OpenDestinations();
            }
            if(endingPlaying)
            {
                endingTime+=Time.deltaTime;
                if(stations[2].towerRing!=null)stations[2].towerRing.Rotate(0,Time.deltaTime*55,0,Space.Self);
                if(endingTime>=6)
                {
                    endingPlaying=false;data.completed=true;Save();endingOpen=true;hud.SetEnding(true);
                }
            }
            else if(data.completed&&stations[2].towerRing!=null)stations[2].towerRing.Rotate(0,Time.deltaTime*8,0,Space.Self);
            hud.SetCurtain(train.TurnaroundFade*.97f);
            RefreshHUD();
        }
        void RefreshHUD()
        {
            string goal;
            if(data.completed)goal=StarCount==9?"九枚星光已集齐，乘车去看看云海吧":"自由探索，寻找剩下的星光";
            else if(endingPlaying)goal="钟塔正在苏醒";
            else if(train.Moving)goal="前往"+stations[train.Destination].displayName;
            else if(CurrentStation==0)goal=data.gardenLine?"上车前往花园岛":StarCount<3?"沿步道寻找星光  "+StarCount+" / 3":"在月台装置激活花园航线";
            else if(CurrentStation==1)goal=data.clockLine?"上车前往星钟岛":StarCount<6?"探索花园，累计找到 6 枚星光  "+StarCount+" / 6":"返回月台，激活钟塔航线";
            else goal="沿步道前往钟塔，点亮云端信标";
            string prompt="WASD 行走   ·   右键拖动观察   ·   H 帮助   ·   Esc 暂停";
            if(Onboard)prompt=train.Moving?train.State+"   ·   C 切换观景镜头":"E 下车   ·   F 选择目的地   ·   C 切换观景镜头";
            else if(NearestInteraction(out bool device))
            {
                if(!device)prompt="E  登上云端列车";
                else if(CurrentStation==2)prompt=data.completed?"E  查看已点亮的钟塔":"E  点亮钟塔";
                else
                {
                    int needed=CurrentStation==0?3:6;bool open=CurrentStation==0?data.gardenLine:data.clockLine;
                    prompt=open?"E  航线已开放":StarCount>=needed?"E  激活下一段航线":"E  查看装置  ·  还需 "+(needed-StarCount)+" 枚星光";
                }
            }
            if(endingPlaying)prompt="星光正在汇聚";
            hud.Refresh(goal,StarCount,train.Moving?stations[train.CurrentStation].displayName+" → "+stations[train.Destination].displayName:stations[CurrentStation].displayName,prompt);
        }
        bool NearestInteraction(out bool device)
        {
            device=false;if(Onboard||!started)return false;
            Vector3 origin=player.transform.position+Vector3.up*.7f;var station=stations[CurrentStation];
            float deviceDistance=Vector3.Distance(origin,station.device.position+Vector3.up*.7f);
            float boardDistance=Vector3.Distance(player.transform.position,station.BoardPoint(train.rig.carriage));
            if(deviceDistance<2f&&deviceDistance<=boardDistance)
            {
                if(Physics.Linecast(origin,station.device.position+Vector3.up*.7f,1<<0,QueryTriggerInteraction.Ignore))return false;
                device=true;return true;
            }
            return !train.Moving&&boardDistance<2f;
        }
        public bool Interact()
        {
            if(!started||Paused||MenuOpen||endingPlaying)return false;
            if(Onboard)return !train.Moving&&Disembark();
            if(!NearestInteraction(out bool device))return false;
            if(device)return ActivateDevice();
            Onboard=true;player.SetSeated(train.rig.carriage);journeyCamera.Snap();
            hud.Toast("已上车。按 F 选择目的地，或按 E 下车");return true;
        }
        bool ActivateDevice()
        {
            if(CurrentStation==2)
            {
                if(data.completed){hud.Toast("钟塔已经点亮，云端航线等待着你");return true;}
                if(!data.clockLine||StarCount<6){hud.Toast("需要先开启钟塔航线，并累计找到 6 枚星光");return false;}
                endingPlaying=true;endingTime=0;stations[2].SetSignal(true);Play(unlockSound);return true;
            }
            int required=CurrentStation==0?3:6;
            if(StarCount<required){hud.Toast("还需找到 "+(required-StarCount)+" 枚星光。解锁不会消耗星光。");return false;}
            bool open=CurrentStation==0?data.gardenLine:data.clockLine;
            if(open){hud.Toast("这段航线已经开放，上车后按 F 选择目的地");return true;}
            if(CurrentStation==0)data.gardenLine=true;else data.clockLine=true;
            stations[CurrentStation].SetSignal(true);if(world!=null)world.PulseRoute(CurrentStation);Save();Play(unlockSound);hud.Toast("航线已点亮！返回列车，继续下一段旅程");return true;
        }
        public bool CanTravel(int from,int to)
        {
            if(from<0||from>2||to<0||to>2||Mathf.Abs(from-to)!=1)return false;
            return Mathf.Min(from,to)==0?data.gardenLine:data.clockLine;
        }
        public void OpenDestinations()
        {
            if(!Onboard||train.Moving||Paused||endingPlaying)return;
            destinationsOpen=true;hud.SetDestinations(true);
        }
        public void CloseDestinations(){destinationsOpen=false;hud.SetDestinations(false);}
        public bool TravelTo(int destination)
        {
            if(!started||Paused||!Onboard||train.Moving||!destinationsOpen)return false;
            if(!CanTravel(CurrentStation,destination)){hud.Toast("这段航线尚未开放");return false;}
            if(!train.Depart(destination))return false;
            data.journeyInTransit=true;Save();CloseDestinations();Play(arriveSound);hud.Toast("下一站，"+stations[destination].displayName);return true;
        }
        public void Arrive(int station)
        {
            data.safeStation=station;data.journeyInTransit=false;Save();Play(arriveSound);
            hud.Toast("已抵达"+stations[station].displayName+"。按 E 下车探索",5);
        }
        bool Disembark()
        {
            var station=stations[CurrentStation];Vector3 point=station.BoardPoint(train.rig.carriage);
            if(!SafeFloor(ref point))point=station.safeSpawn.position;
            if(!SafeFloor(ref point)){hud.Toast("月台暂时无法下车，请稍后再试");return false;}
            Onboard=false;player.Place(point,Quaternion.LookRotation(-station.outward));player.ShowModel();
            journeyCamera.yaw=CurrentStation==1?-90:CurrentStation==2?180:0;journeyCamera.Snap();return true;
        }
        bool SafeFloor(ref Vector3 point)
        {
            if(!Physics.Raycast(point+Vector3.up*2,Vector3.down,out var floor,4,1<<0,QueryTriggerInteraction.Ignore))return false;
            point.y=floor.point.y+.04f;
            return !Physics.CheckCapsule(point+Vector3.up*.35f,point+Vector3.up*1.5f,.25f,1<<0,QueryTriggerInteraction.Ignore);
        }
        public void Respawn(bool toast=true)
        {
            if(Onboard)return;
            var s=stations[CurrentStation];player.Place(s.safeSpawn.position,Quaternion.LookRotation(-s.outward));
            journeyCamera.yaw=CurrentStation==1?-90:CurrentStation==2?180:0;journeyCamera.Snap();
            if(toast)hud.Toast("已返回安全月台，收集进度已保留");
        }
        public bool Collect(JourneyStar star)
        {
            if(!CanCollect||!star.gameObject.activeSelf||collected.Contains(star.starId))return false;
            if(Vector3.Distance(player.transform.position+Vector3.up*.75f,star.transform.position)>1.2f)return false;
            collected.Add(star.starId);
            var echo=Instantiate(star.visual.gameObject,star.visual.position,star.visual.rotation);echo.name="Collected starlight echo";echo.AddComponent<JourneyPickupEcho>();
            if(soundscape!=null)soundscape.StarBurst(star.transform.position);
            star.gameObject.SetActive(false);Play(collectSound);
            if(StarCount==9&&!data.allStars){data.allStars=true;hud.Toast("九枚星光全部集齐，云端旅程留下了你的印记",6);}
            else hud.Toast("找到一枚星光   "+StarCount+" / 9",2);
            Save();return true;
        }
        public void TogglePause()
        {
            if(helpOpen)CloseHelp();
            Paused=!Paused;Time.timeScale=Paused?0:1;hud.SetPause(Paused);
        }
        public void AskRestart(){restartOpen=true;hud.SetRestart(true);}
        public void CancelRestart(){restartOpen=false;hud.SetRestart(false);}
        public void Restart()
        {
            StopAllCoroutines();float volume=data.effectsVolume,sensitivity=data.sensitivity,musicVolume=data.musicVolume;bool reduceMotion=data.reduceMotion,lowEffects=data.lowEffects;
            if(saveBlocked)
            {
                string suffix=".unreadable_"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
                foreach(var path in new[]{SavePath,SavePath+".bak"})if(File.Exists(path))File.Move(path,path+suffix);
            }
            data=new JourneySave {effectsVolume=volume,sensitivity=sensitivity,musicVolume=musicVolume,reduceMotion=reduceMotion,lowEffects=lowEffects};collected.Clear();saveBlocked=false;notice="";
            Onboard=false;Paused=false;helpOpen=false;destinationsOpen=false;endingOpen=false;restartOpen=false;endingPlaying=false;started=true;Time.timeScale=1;
            hud.ClosePanels();ApplyState();Save();hud.Toast("新的旅程开始了，沿步道寻找三枚星光");
        }
        public void RestoreExploration()
        {
            Time.timeScale=1;Paused=false;Onboard=false;destinationsOpen=helpOpen=endingOpen=restartOpen=false;endingPlaying=false;
            hud.ClosePanels();ApplyState();started=true;
        }
        public void OpenHelp(){helpOpen=true;if(hud.helpPanel!=null)hud.helpPanel.SetActive(true);if(!Paused)Time.timeScale=0;}
        public void CloseHelp(){helpOpen=false;if(hud.helpPanel!=null)hud.helpPanel.SetActive(false);Time.timeScale=Paused?0:1;}
        public void SetReduceMotion(bool value){data.reduceMotion=value;Save();}
        public void SetLowEffects(bool value){data.lowEffects=value;Save();}
        public void ContinueExploring(){endingOpen=false;hud.SetEnding(false);}
        public void SetVolume(float value){data.effectsVolume=Mathf.Clamp01(value);if(audioSource!=null)audioSource.volume=data.effectsVolume;Save();}
        public void SetMusicVolume(float value){data.musicVolume=Mathf.Clamp01(value);Save();}
        public void SetSensitivity(float value){data.sensitivity=Mathf.Clamp(value,.4f,2);Save();}
        void Save()
        {
            if(saveBlocked)return;
            data.stars=new List<string>(collected);data.stars.Sort(StringComparer.Ordinal);
            try{JourneySaveStore.Write(SavePath,data);}
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){hud.Toast("进度暂时无法保存，请检查存储空间",5);Debug.LogWarning("Journey save failed: "+e.Message);}
        }
        void Play(AudioClip clip){if(audioSource!=null){audioSource.volume=data.effectsVolume;audioSource.PlayOneShot(clip);}}
        AudioClip Tone(float from,float to,float duration)
        {
            int sampleRate=22050;var samples=new float[Mathf.CeilToInt(sampleRate*duration)];float phase=0;
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/samples.Length;phase+=Mathf.Lerp(from,to,t)*2*Mathf.PI/sampleRate;
                samples[i]=Mathf.Sin(phase)*Mathf.Sin(t*Mathf.PI)*.16f;
            }
            var clip=AudioClip.Create("Cloud chime",samples.Length,1,sampleRate,false);clip.SetData(samples,0);return clip;
        }
        void OnDestroy(){Time.timeScale=1;if(collectSound!=null)Destroy(collectSound);if(unlockSound!=null)Destroy(unlockSound);if(arriveSound!=null)Destroy(arriveSound);}
    }
}
