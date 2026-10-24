using System;
using System.IO;
using System.Collections.Generic;
using CloudRail;
using UnityEditor;
using UnityEngine;

public static class VerifyReleasePresentation
{
    [Serializable] class Report {public string state="running",failure;public List<string> passed=new List<string>();}
    static Report report;static JourneyGame game;static int phase,lastFrame;static float until;
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);report.passed.Add(name);}
    public static string Start()
    {
        game=UnityEngine.Object.FindAnyObjectByType<JourneyGame>();if(!game.saveFile.Contains("verification"))throw new Exception("Isolated test save required");
        report=new Report();
        var position=game.player.transform.position;var rotation=game.player.transform.rotation;float yaw=game.journeyCamera.yaw;
        int checks=0;
        foreach(var point in new[]{new Vector3(4,1.04f,-30),new Vector3(38,.04f,4),new Vector3(5,.04f,27)})
        {
            game.player.Place(point,Quaternion.identity);
            for(int angle=0;angle<360;angle+=30)
            {
                game.journeyCamera.yaw=angle;game.journeyCamera.Snap();
                Check(!Physics.CheckSphere(Camera.main.transform.position,.12f,(1<<0)|(1<<8),QueryTriggerInteraction.Collide),"camera clear at "+point+" yaw "+angle);checks++;
            }
        }
        game.player.Place(position,rotation);game.journeyCamera.yaw=yaw;game.journeyCamera.Snap();
        phase=0;lastFrame=-1;until=0;EditorApplication.update+=Tick;return "Presentation checks running";
    }
    static void Tick()
    {
        if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;if(Time.unscaledTime<until)return;
        try
        {
            switch(phase++)
            {
                case 0:
                    var block=new MaterialPropertyBlock();game.world.firstRoute[0].GetPropertyBlock(block);
                    Check(block.GetColor("_EmissionColor").maxColorComponent>0,"restored first route glows");
                    game.world.memorialLamps[0].GetPropertyBlock(block);Check(block.GetColor("_EmissionColor").maxColorComponent>0,"all-star memorial visually lit");
                    Check(game.world.MemorialLit&&game.StarCount==9,"memorial follows nine unique stars");
                    Check(game.world.deviceLabels[0].text.Contains("已开放"),"device expresses state with text");
                    Check(Mathf.Abs(game.world.firstGate[0].localEulerAngles.z-50)<.1f,"open signal uses changed geometry");
                    Check(game.soundscape.WaterfallSource!=null&&game.soundscape.WaterfallSource.spatialBlend==1&&game.soundscape.WaterfallSource.isPlaying,"waterfall plays as spatial audio");
                    game.TogglePause();game.hud.helpButton.onClick.Invoke();
                    Check(game.hud.helpPanel.activeSelf&&game.Paused&&Time.timeScale==0,"handbook opens over pause menu");
                    game.hud.closeHelpButton.onClick.Invoke();Check(!game.hud.helpPanel.activeSelf&&game.Paused&&Time.timeScale==0,"closing handbook preserves pause");
                    game.hud.qualityToggle.isOn=true;game.hud.stableToggle.isOn=false;until=Time.unscaledTime+.25f;break;
                case 1:
                    int on=0;foreach(var cloud in game.world.cloudBanks)if(cloud.activeSelf)on++;
                    Check(on==(game.world.cloudBanks.Length+1)/2&&Application.targetFrameRate==30,"light quality reduces clouds and sets 30 FPS cap");
                    Check(!game.ReduceMotion,"stable-camera toggle changes preference");
                    var data=JourneySaveStore.Read(game.SavePath,out _,out bool blocked);Check(!blocked&&data.lowEffects&&!data.reduceMotion,"new settings persist in save");
                    game.hud.qualityToggle.isOn=false;game.hud.stableToggle.isOn=true;game.TogglePause();
                    until=Time.unscaledTime+.25f;break;
                case 2:
                    Check(Application.targetFrameRate==60&&game.ReduceMotion&&!game.LowEffects,"normal quality and stable camera restored");
                    game.OpenHelp();Check(!game.CanWalk&&Time.timeScale==0,"H-style help blocks movement");game.CloseHelp();
                    Check(game.CanWalk&&Time.timeScale==1,"help returns control without losing progress");
                    report.state="passed";Finish();break;
            }
        }
        catch(Exception e){report.state="failed";report.failure=e.ToString();Finish();}
    }
    static void Finish(){EditorApplication.update-=Tick;File.WriteAllText("Documentation/里程碑04_表现验证.json",JsonUtility.ToJson(report,true));}
}
