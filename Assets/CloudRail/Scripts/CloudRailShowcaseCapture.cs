#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace CloudRail
{
    // Editor-only cinematography. Added temporarily in Play mode; never changes the saved scene.
    [DefaultExecutionOrder(10000)]
    public sealed class CloudRailShowcaseCapture : MonoBehaviour
    {
        public int Frame { get; private set; }
        public bool Recording { get; private set; }
        public string OutputDirectory;
        public string Failure { get; private set; }
        const int Fps=30,FramesPerShot=240;
        JourneyGame game;SkyVoyage voyage;CloudTrain rig;Camera camera;
        PropertyInfo distanceProperty;
        Vector3[] rods;
        RenderTexture target;Texture2D pixels;
        System.Diagnostics.Process encoder;
        Stream pipe;float oldDelta,oldAspect,oldFov,oldLead,oldUnfold;
        int oldRate,oldVsync;
        Canvas[] canvases;bool[] canvasStates;
        bool prepared;
        static readonly float[] Starts={270,294,221,0};
        void Prepare()
        {
            if(prepared)return;prepared=true;
            game=FindAnyObjectByType<JourneyGame>();voyage=game.voyage;rig=game.train.rig;camera=Camera.main;
            if(!voyage.Active)voyage.Enter();voyage.enabled=false;rig.enabled=false;
            game.train.enabled=false;game.journeyCamera.enabled=false;
            distanceProperty=typeof(SkyVoyage).GetProperty("Distance");
            rods=new Vector3[rig.connectingRods.Length];for(int i=0;i<rods.Length;i++)rods[i]=rig.connectingRods[i].localPosition;
            canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);canvasStates=new bool[canvases.Length];
            for(int i=0;i<canvases.Length;i++){canvasStates[i]=canvases[i].enabled;canvases[i].enabled=false;}
            oldFov=camera.fieldOfView;oldAspect=camera.aspect;oldLead=voyage.skyway.lead;oldUnfold=voyage.skyway.unfoldLength;
            voyage.skyway.lead=21;voyage.skyway.unfoldLength=7;
        }
        public void Preview(int shot,float progress)
        {
            Prepare();Frame=shot*FramesPerShot+Mathf.RoundToInt(progress*(FramesPerShot-1));Pose();
        }
        void Pose()
        {
            int shot=Mathf.Min(Frame/FramesPerShot,3);float t=(Frame%FramesPerShot)/(float)(FramesPerShot-1);
            float distance=Starts[shot]+t*8*5.2f;distanceProperty.SetValue(voyage,distance);
            rig.path.Evaluate(distance,out var p,out var q);rig.locomotive.SetPositionAndRotation(p,q);
            rig.path.Evaluate(distance-rig.carriageSpacing,out var cp,out var cq);rig.carriage.SetPositionAndRotation(cp,cq);
            float angle=distance/.44f*Mathf.Rad2Deg;
            foreach(var w in rig.wheels)w.localRotation=Quaternion.AngleAxis(angle,Vector3.right);
            for(int i=0;i<rods.Length;i++)rig.connectingRods[i].localPosition=rods[i]+new Vector3(0,.19f*Mathf.Cos(angle*Mathf.Deg2Rad),.19f*Mathf.Sin(angle*Mathf.Deg2Rad));
            var emission=rig.steam.emission;emission.rateOverTime=6;
            Vector3 aim,position;float u=t*t*(3-2*t);
            if(shot==0){aim=p+q*Vector3.forward*6+Vector3.up*2;position=p+new Vector3(Mathf.Lerp(13,10,u),10,-25);camera.fieldOfView=47;}
            else if(shot==1){aim=new Vector3(0,9,0);position=aim+Quaternion.Euler(0,Mathf.Lerp(-5,6,u),0)*Vector3.Lerp(new Vector3(82,49,-105),new Vector3(74,45,-96),u);camera.fieldOfView=45;}
            else if(shot==2){aim=p+q*Vector3.right*4+q*Vector3.forward*2+Vector3.up*4;position=p+new Vector3(-21,12,Mathf.Lerp(-19,-14,u));camera.fieldOfView=50;}
            else{aim=new Vector3(2,9,0);position=aim+Quaternion.Euler(0,Mathf.Lerp(2,-6,u),0)*Vector3.Lerp(new Vector3(69,43,-91),new Vector3(90,57,-117),u);camera.fieldOfView=45;}
            camera.aspect=16f/9;camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(aim-position));
        }
        void Update(){if(prepared)Pose();}
        void LateUpdate(){if(prepared)Pose();}
        public void Record(string directory)
        {
            if(Recording)throw new InvalidOperationException("Already recording");
            Prepare();OutputDirectory=directory;Directory.CreateDirectory(directory);Frame=0;Recording=true;StartCoroutine(Capture());
        }
        IEnumerator Capture()
        {
            oldDelta=Time.captureDeltaTime;oldRate=Application.targetFrameRate;oldVsync=QualitySettings.vSyncCount;
            Time.captureDeltaTime=1f/Fps;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;Application.runInBackground=true;
            target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=1};target.Create();camera.targetTexture=target;
            pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            string ffmpeg=Environment.GetEnvironmentVariable("CLOUDRAIL_FFMPEG");
            if(string.IsNullOrEmpty(ffmpeg))ffmpeg=File.Exists("/opt/homebrew/bin/ffmpeg")?"/opt/homebrew/bin/ffmpeg":"ffmpeg";
            var start=new System.Diagnostics.ProcessStartInfo(ffmpeg){
                Arguments="-y -hide_banner -loglevel error -f rawvideo -pixel_format rgb24 -video_size 1920x1080 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset fast -crf 17 -pix_fmt yuv420p -movflags +faststart \""+Path.Combine(OutputDirectory,"CloudRail_Cinematic_Silent.mp4")+"\"",
                UseShellExecute=false,RedirectStandardInput=true,RedirectStandardError=true,CreateNoWindow=true};
            encoder=System.Diagnostics.Process.Start(start);encoder.ErrorDataReceived+=(sender,e)=>{if(!string.IsNullOrEmpty(e.Data))Failure=e.Data;};encoder.BeginErrorReadLine();pipe=encoder.StandardInput.BaseStream;
            // Warm up the actual scene's particles and post processing before the first encoded frame.
            for(int warm=0;warm<12;warm++)yield return new WaitForEndOfFrame();
            while(Frame<FramesPerShot*4)
            {
                // Settle the live flock around its new train position before encoding a new take.
                if(Frame>0&&Frame%FramesPerShot==0)for(int warm=0;warm<75;warm++)yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                var previous=RenderTexture.active;RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1920,1080),0,0,false);pixels.Apply(false,false);RenderTexture.active=previous;
                byte[] data=pixels.GetRawTextureData();
                try{pipe.Write(data,0,data.Length);}catch(Exception e){Failure=e.Message;break;}
                if(Frame%240==120)File.WriteAllBytes(Path.Combine(OutputDirectory,"Shot_"+(Frame/240+1)+".jpg"),pixels.EncodeToJPG(95));
                Frame++;if(Frame%60==0)File.WriteAllText(Path.Combine(OutputDirectory,"capture-progress.txt"),Frame+" / 960");
            }
            pipe.Close();pipe=null;encoder.WaitForExit();int exit=encoder.ExitCode;encoder.Dispose();encoder=null;
            File.WriteAllText(Path.Combine(OutputDirectory,"capture.json"),"{\"source\":\"Actual Unity scene camera, no synthetic footage\",\"width\":1920,\"height\":1080,\"fps\":30,\"frames\":"+Frame+",\"encoderExit\":"+exit+"}");
            Restore();Recording=false;
        }
        void Restore()
        {
            if(camera!=null){camera.targetTexture=null;camera.fieldOfView=oldFov;camera.aspect=oldAspect;}
            if(target!=null){target.Release();Destroy(target);target=null;}if(pixels!=null){Destroy(pixels);pixels=null;}
            Time.captureDeltaTime=oldDelta;Application.targetFrameRate=oldRate;QualitySettings.vSyncCount=oldVsync;
            if(voyage!=null){voyage.skyway.lead=oldLead;voyage.skyway.unfoldLength=oldUnfold;voyage.enabled=true;}
            if(canvases!=null)for(int i=0;i<canvases.Length;i++)if(canvases[i]!=null)canvases[i].enabled=canvasStates[i];
            prepared=false;
        }
        void OnDestroy(){if(pipe!=null)pipe.Close();if(encoder!=null&&!encoder.HasExited)encoder.Kill();if(prepared)Restore();}
    }
}
#endif
