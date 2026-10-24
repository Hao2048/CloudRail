using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CloudRail
{
    // Explicit command-line tools use isolated progress; ordinary play never enters this code.
    [DefaultExecutionOrder(-1000)]
    public sealed class JourneyDiagnostics : MonoBehaviour
    {
        [Serializable] sealed class Stage {public string name;public int frames;public float seconds,averageFPS,p95FrameMs,minimumFPS;}
        [Serializable] sealed class Report
        {
            public string state,device,processor,graphics,os,unity,resolution;
            public int memoryMB;public bool editor;public List<Stage> stages=new List<Stage>();
            public string error;public float totalSeconds;
        }
        JourneyGame game;bool benchmark,film;string output;Report report;
        readonly List<float> frameTimes=new List<float>();string stage;float since,started;
        readonly List<string> errors=new List<string>();
        void Awake()
        {
            var args=Environment.GetCommandLineArgs();benchmark=Array.IndexOf(args,"-cloudrail-benchmark")>=0;film=Array.IndexOf(args,"-cloudrail-showcase")>=0;
            if(!benchmark&&!film){enabled=false;return;}
            output=Directory.GetCurrentDirectory()+"/CloudlineDiagnostics";
            int index=Array.IndexOf(args,"-cloudrail-output");if(index>=0&&index+1<args.Length)output=args[index+1];Directory.CreateDirectory(output);
            game=GetComponent<JourneyGame>();game.saveFile=benchmark?"cloudrail_benchmark_verification.json":"cloudrail_film_verification.json";
            var data=new JourneySave{gardenLine=true,clockLine=true,safeStation=0};
            data.stars.AddRange(new[]{"cloud_01","cloud_02","cloud_03","garden_01","garden_02","garden_03"});
            JourneySaveStore.Write(game.SavePath,data);
            Application.runInBackground=true;
            report=new Report{state="running",device=SystemInfo.deviceModel,processor=SystemInfo.processorType,graphics=SystemInfo.graphicsDeviceName,os=SystemInfo.operatingSystem,unity=Application.unityVersion,memoryMB=SystemInfo.systemMemorySize,editor=Application.isEditor};
            Application.logMessageReceived+=Log;
        }
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
        IEnumerator Start()
        {
            if(!benchmark&&!film)yield break;
            Screen.SetResolution(film?1280:1920,film?720:1080,FullScreenMode.Windowed);QualitySettings.vSyncCount=0;
            yield return new WaitForSeconds(3);started=Time.realtimeSinceStartup;
            if(film){yield return Film();yield break;}
            report.resolution=Screen.width+" x "+Screen.height;StartStage("Cloudrise exploration");
            yield return new WaitForSeconds(5);
            Board();game.journeyCamera.trainView=2;game.journeyCamera.Snap();game.OpenDestinations();game.TravelTo(1);StartStage("Cloudrise to Garden viaduct");
            yield return new WaitUntil(()=>!game.train.Moving);game.Interact();game.player.Place(new Vector3(38,.05f,4),Quaternion.identity);game.journeyCamera.yaw=-90;game.journeyCamera.Snap();StartStage("Garden exploration");
            float walkUntil=Time.time+5;
            while(Time.time<walkUntil){game.player.GetComponent<CharacterController>().Move(Vector3.left*Time.deltaTime);yield return null;}
            Board();game.journeyCamera.trainView=0;game.journeyCamera.Snap();game.OpenDestinations();game.TravelTo(2);StartStage("Garden to Starclock viaduct");
            yield return new WaitUntil(()=>!game.train.Moving);game.Interact();game.player.Place(new Vector3(5,.04f,21),Quaternion.identity);game.journeyCamera.yaw=180;game.journeyCamera.Snap();game.Interact();StartStage("Clock awakening");
            yield return new WaitForSeconds(7);game.ContinueExploring();FinishStage();
            report.totalSeconds=Time.realtimeSinceStartup-started;report.state=game.Completed&&errors.Count==0?"passed":"failed";report.error=string.Join("\n",errors);
            File.WriteAllText(Path.Combine(output,"performance.json"),JsonUtility.ToJson(report,true));
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"Standalone_Clock.png"));yield return new WaitForSeconds(.5f);
            if(!Application.isEditor)Application.Quit();
        }
        void Board(){game.player.Place(game.stations[game.CurrentStation].BoardPoint(game.train.rig.carriage),Quaternion.identity);if(!game.Interact()||!game.Onboard)throw new InvalidOperationException("Diagnostic boarding failed");}
        void StartStage(string name){FinishStage();stage=name;since=Time.realtimeSinceStartup;frameTimes.Clear();}
        void FinishStage()
        {
            if(string.IsNullOrEmpty(stage)||frameTimes.Count==0)return;
            var sorted=frameTimes.ToArray();Array.Sort(sorted);float total=0;foreach(float dt in frameTimes)total+=dt;
            report.stages.Add(new Stage{name=stage,frames=frameTimes.Count,seconds=Time.realtimeSinceStartup-since,averageFPS=frameTimes.Count/total,p95FrameMs=sorted[Mathf.Min(sorted.Length-1,(int)(sorted.Length*.95f))]*1000,minimumFPS=1/sorted[sorted.Length-1]});stage=null;
        }
        void Update(){if(benchmark&&!string.IsNullOrEmpty(stage))frameTimes.Add(Time.unscaledDeltaTime);}
        IEnumerator Film()
        {
            Time.captureDeltaTime=1f/30;var view=game.journeyCamera;view.enabled=false;game.hud.gameObject.SetActive(false);
            int frame=0;var camera=Camera.main.transform;
            for(int shot=0;shot<3;shot++)
            {
                if(shot==1){Board();game.OpenDestinations();game.TravelTo(1);}
                if(shot==2)
                {
                    game.train.Initialize(2);game.Arrive(2);if(game.Onboard)game.Interact();game.player.Place(new Vector3(5,.04f,21),Quaternion.identity);game.Interact();
                }
                for(int i=0;i<210;i++)
                {
                    float t=i/209f;Vector3 target,position;
                    if(shot==0){target=new Vector3(-2,1,-18);position=target+Quaternion.Euler(0,Mathf.Lerp(-28,-17,t),0)*new Vector3(-28,23,-34);}
                    else if(shot==1){target=game.train.rig.locomotive.position+Vector3.up*1.7f;position=target-game.train.rig.locomotive.forward*16+game.train.rig.locomotive.right*15+Vector3.up*10;}
                    else {target=new Vector3(0,4.5f,21);position=target+Quaternion.Euler(0,Mathf.Lerp(-15,8,t),0)*new Vector3(15,10,22);}
                    camera.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
                    yield return new WaitForEndOfFrame();
                    var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,"frame_"+(frame++).ToString("D4")+".png"),image.EncodeToPNG());Destroy(image);
                }
            }
            Time.captureDeltaTime=0;File.WriteAllText(Path.Combine(output,"capture.json"),"{\"frames\":"+frame+",\"fps\":30,\"seconds\":21,\"source\":\"Unity standalone actual scene frames\"}");
            if(!Application.isEditor)Application.Quit();
        }
        void OnDestroy(){if(benchmark||film)Application.logMessageReceived-=Log;}
    }
}
