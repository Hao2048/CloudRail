using System;
using System.Collections.Generic;
using System.IO;
using CloudRail;
using UnityEditor;
using UnityEngine;

public static class VerifyJourneyAudio
{
    [Serializable] class Report {public string state="running";public List<string> passed=new List<string>();public string failure;public int departures,awakenings;public float restoredMusic,restoredEffects;}
    static Report report;static JourneyGame game;static JourneyAudio audio;
    static int phase,frame;static float until,deadline,distance;static int musicSample,railSample;
    static string PathName="Documentation/里程碑03_运行验证.json";
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);report.passed.Add(name);}
    static void Save(){File.WriteAllText(PathName,JsonUtility.ToJson(report,true));}
    public static string Start()
    {
        game=UnityEngine.Object.FindAnyObjectByType<JourneyGame>();audio=game.soundscape;
        if(!Application.isPlaying||!game.saveFile.Contains("audio_verification"))throw new Exception("Use the isolated audio verification save in Play mode.");
        report=new Report();phase=0;frame=-1;deadline=Time.realtimeSinceStartup+70;until=Time.realtimeSinceStartup+2;
        EditorApplication.update+=Tick;return "Soundscape test running";
    }
    static void Wait(float seconds){until=Time.realtimeSinceStartup+seconds;phase++;}
    static void Tick()
    {
        if(Time.frameCount==frame)return;frame=Time.frameCount;
        try
        {
            if(Time.realtimeSinceStartup>deadline)throw new Exception("Soundscape test timeout");
            if(Time.realtimeSinceStartup<until)return;
            switch(phase)
            {
                case 0:
                    Check(audio!=null&&UnityEngine.Object.FindObjectsByType<JourneyAudio>().Length==1,"single scene soundscape");
                    Check(audio.theme.length>95.9f&&audio.MusicSource.loop&&audio.MusicSource.isPlaying,"96 second looping score plays");
                    Check(audio.MusicSource.clip.loadType==AudioClipLoadType.Streaming,"score streams instead of decompressing full track");
                    Check(audio.RailSource.volume==0,"stationary wheels are silent");
                    musicSample=audio.MusicSource.timeSamples;
                    var star=Array.Find(game.stars,s=>s.starId=="garden_04");game.player.Place(star.transform.position-Vector3.up*.75f,Quaternion.identity);
                    Wait(.2f);break;
                case 1:
                    Check(game.StarCount==7,"walking into optional star collects normally");
                    Check(GameObject.Find("Starlight collected")!=null,"collection emits gold particle burst");
                    Check(audio.MusicSource.timeSamples!=musicSample,"music DSP playback advances");
                    game.TogglePause();game.hud.musicSlider.value=.18f;game.hud.volumeSlider.value=.31f;
                    Check(Mathf.Abs(game.MusicVolume-.18f)<.001f&&Mathf.Abs(game.EffectsVolume-.31f)<.001f,"independent slider callbacks update settings");
                    game.TogglePause();game.player.Place(game.stations[1].BoardPoint(game.train.rig.carriage),Quaternion.identity);
                    Check(game.Interact()&&game.Onboard,"normal boarding accepted");game.OpenDestinations();Check(game.TravelTo(2),"normal departure accepted");
                    Wait(4);break;
                case 2:
                    Check(game.train.Speed>1&&audio.RailSource.volume>0,"wheel gain rises with actual train speed");
                    Check(audio.DeparturesHeard==1,"one whistle per departure");
                    game.TogglePause();distance=game.train.Distance;railSample=audio.RailSource.timeSamples;
                    Wait(1);break;
                case 3:
                    Check(Mathf.Abs(game.train.Distance-distance)<.001f,"pause freezes train movement");
                    Check(!audio.RailSource.isPlaying,"pause holds wheel playback position");
                    Check(audio.MusicSource.isPlaying&&audio.MusicSource.volume<.06f,"paused music remains quietly audible");
                    railSample=audio.RailSource.timeSamples;game.hud.volumeSlider.value=0;Wait(.3f);break;
                case 4:
                    Check(audio.RailSource.timeSamples==railSample,"paused wheel sample remains fixed");
                    Check(audio.RailSource.volume==0&&audio.WhistleSource.volume==0,"effects slider mutes active world sources");
                    game.hud.volumeSlider.value=.31f;game.TogglePause();Wait(.3f);break;
                case 5:
                    Check(audio.RailSource.isPlaying,"resume continues wheel loop");
                    Wait(0);break;
                case 6:
                    if(game.train.Moving)return;
                    Check(game.CurrentStation==2&&audio.RailSource.volume<.001f,"arrival silences wheels");
                    Check(game.Interact()&&!game.Onboard,"normal disembark accepted");
                    game.player.Place(game.stations[2].device.position+Vector3.right*.95f,Quaternion.identity);
                    Check(game.Interact()&&game.EndSequencePlaying,"clock awakening activated");Wait(.4f);break;
                case 7:
                    Check(audio.AwakeningsHeard==1,"clock fanfare plays once");Wait(6);break;
                case 8:
                    Check(game.Completed,"clock completes with music attached");
                    var data=JourneySaveStore.Read(game.SavePath,out string notice,out bool blocked);
                    Check(!blocked&&Mathf.Abs(data.musicVolume-.18f)<.001f&&Mathf.Abs(data.effectsVolume-.31f)<.001f,"both sound settings saved");
                    report.restoredMusic=data.musicVolume;report.restoredEffects=data.effectsVolume;
                    report.departures=audio.DeparturesHeard;report.awakenings=audio.AwakeningsHeard;report.state="passed";Save();
                    EditorApplication.update-=Tick;break;
            }
        }
        catch(Exception e){report.state="failed";report.failure=e.ToString();Save();EditorApplication.update-=Tick;Debug.LogError(e);}
    }
}
