using System;
using System.Collections.Generic;
using System.IO;
using CloudRail;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifyJourneyRelease
{
    sealed class Step
    {
        public string name;
        public Func<bool> tick;
        public double timeout;
    }
    [Serializable] sealed class Report
    {
        public string state="running";
        public string current;
        public string failure;
        public int stars,station,arrivals;
        public bool completed;
        public List<string> passed=new List<string>();
    }
    static readonly List<Step> steps=new List<Step>();
    static JourneyGame game;
    static Keyboard virtualKeyboard,previousKeyboard;
    static Report report;
    static int index,lastFrame;
    static double since;
    static int round=1;
    static string Output=>"Documentation/里程碑04_流程验证_"+round+".json";

    public static string Start()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Run in Play mode.");
        game=UnityEngine.Object.FindAnyObjectByType<JourneyGame>();
        if(!game.saveFile.Contains("verification"))throw new InvalidOperationException("Refusing to test against a player save.");
        EditorApplication.update-=Tick;steps.Clear();report=new Report();index=0;lastFrame=-1;
        previousKeyboard=Keyboard.current;virtualKeyboard=InputSystem.AddDevice<Keyboard>("JourneyVerificationKeyboard");
        game.Restart();
        Action("Locked activator rejects insufficient starlight",()=>Check(!game.Interact()&&!game.GardenOpen&&game.StarCount==0,"Activator opened too early"));
        Move("First cloud star",-1,-30);
        Action("Duplicate pickup cannot count twice",()=>Check(game.StarCount==1&&!game.Collect(game.stars[0]),"Duplicate star accepted"));
        Move("Platform east",7,-30);Move("Ramp entrance",7.7f,-28.8f);Move("Walk down accessible ramp",9,-24);
        Move("Outside station wing",10,-18);Move("South of fountain",4,-18);Move("Third cloud star",4,-15);
        Action("World instructions and stable camera exist",()=>Check(game.world!=null&&game.hud.helpPanel!=null&&game.ReduceMotion&&game.world.firstRoute.Length==60,"Release presentation missing"));
        Action("Three required stars collected",()=>Check(game.StarCount==3,"Cloud stars were not all reachable"));
        Action("Help pauses walking and restores operation",()=>{game.OpenHelp();Check(game.MenuOpen&&!game.CanWalk&&Time.timeScale==0,"Help failed to pause");game.CloseHelp();Check(game.CanWalk&&Time.timeScale==1,"Help failed to resume");});
        Move("Return south of fountain",4,-18);Move("Return east of wing",10,-18);Move("Return ramp foot",9,-24);Move("Return ramp top",7.7f,-28.8f);Move("Return platform",7,-30);Move("Cloudrise route device",5,-31.2f);
        Action("First route opens without consuming stars",()=>Check(game.Interact()&&game.GardenOpen&&game.StarCount==3,"First route activation failed"));
        Board("Board Cloudrise train");
        Action("Adjacent destinations only",()=>Check(!game.CanTravel(0,2)&&!game.CanTravel(1,2),"Locked or skipped route accepted"));
        Depart("Depart for Garden",1);
        Action("Disembarking blocked while moving",()=>Check(!game.Interact()&&game.Onboard,"Moving train allowed disembark"));
        Vector3 pausedPosition=Vector3.zero;
        Action("Pause during journey",()=>{game.TogglePause();pausedPosition=game.train.rig.locomotive.position;});
        double pauseStart=0;
        steps.Add(new Step{name="Pause holds train and player",timeout=5,tick=()=>{if(pauseStart==0)pauseStart=EditorApplication.timeSinceStartup;if(EditorApplication.timeSinceStartup-pauseStart<.6)return false;Check(Vector3.Distance(pausedPosition,game.train.rig.locomotive.position)<.001f,"Train moved during pause");game.TogglePause();return true;}});
        Action("Mid-trip save resumes at departure station",()=>{var d=JourneySaveStore.Read(game.SavePath,out _,out bool blocked);Check(!blocked&&d.safeStation==0&&d.stars.Count==3,"Mid-trip save unsafe");});
        Arrive("Arrive Garden",1);
        Action("Safe Garden disembark",()=>Check(game.Interact()&&!game.Onboard,"Garden disembark failed"));
        Move("Garden platform ramp",43.5f,3);Move("Garden accessible approach",39,3);Move("Garden star one",39,-3);
        Move("Garden northern path",38,4);Move("Garden star two",34,4);Move("Garden western path",26,4);Move("Garden southern path",26,-2);Move("Garden star three",28,-2);
        Action("Six cumulative stars reached",()=>Check(game.StarCount==6,"Garden main route failed"));
        Move("Return around fountain south",28,-5);Move("Return garden southeast",38,-5);Move("Return garden northeast",39,3);Move("Return platform via ramp",43.5f,3);Move("Garden route device",43,-1.5f);
        Action("Second route opens without consuming stars",()=>Check(game.Interact()&&game.ClockOpen&&game.StarCount==6,"Second route activation failed"));
        Board("Board Garden train");Depart("Depart for Starclock",2);Arrive("Arrive Starclock",2);
        Action("Safe Starclock disembark",()=>Check(game.Interact()&&!game.Onboard,"Starclock disembark failed"));
        Move("Clock ramp entrance",5,31.5f);Move("Clock ramp foot",5,27);Move("Clock activation device",5,21);
        Action("Clock ending starts with six stars",()=>Check(game.Interact()&&game.EndSequencePlaying,"Clock ending did not start"));
        steps.Add(new Step{name="Clock ending completes",timeout=12,tick=()=>game.Completed});
        Action("Continue exploring after ending",()=>game.hud.continueButton.onClick.Invoke());
        Move("Return clock ramp foot",5,27);Move("Return clock ramp top",5,31.5f);
        Board("Reboard after ending");Depart("Return to Garden",1);Arrive("Return Garden arrival",1);
        Action("Reverse-direction safe disembark",()=>Check(game.Interact()&&!game.Onboard,"Reverse arrival disembark failed"));
        Move("Optional route approach",43.5f,3);Move("Optional route entry",39,3);Move("Optional north path",38,7);Move("Optional northern star",30,9);Move("Optional pavilion star",27,6);Move("Optional west path",23,3);Move("Optional southern star",23,-5);
        Action("All nine stars and ending persist",()=>
        {
            Check(game.StarCount==9&&game.Completed&&game.world.MemorialLit,"All-star memorial failed");
            var d=JourneySaveStore.Read(game.SavePath,out _,out bool blocked);Check(!blocked&&d.stars.Count==9&&d.completed&&d.safeStation==1,"Progress did not persist");
            VerifyBackup();
        });
        since=EditorApplication.timeSinceStartup;Write();EditorApplication.update+=Tick;
        return "Journey verification started with synthetic keyboard input in an isolated save slot.";
    }
    static void Action(string name,Action action)=>steps.Add(new Step{name=name,timeout=3,tick=()=>{Release();action();return true;}});
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Move(string name,float x,float z)
    {
        steps.Add(new Step{name=name,timeout=35,tick=()=>
        {
            Vector3 delta=new Vector3(x,game.player.transform.position.y,z)-game.player.transform.position;
            if(delta.magnitude<.32f){Release();return true;}
            Check(game.CanWalk,"Walking input unexpectedly locked");
            game.journeyCamera.yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;game.journeyCamera.Snap();
            InputSystem.QueueStateEvent(virtualKeyboard,new KeyboardState(Key.W));return false;
        }});
    }
    static void Board(string name)
    {
        steps.Add(new Step{name=name,timeout=15,tick=()=>
        {
            Vector3 target=game.stations[game.CurrentStation].BoardPoint(game.train.rig.carriage);
            Vector3 delta=target-game.player.transform.position;delta.y=0;
            if(delta.magnitude<.45f){Release();Check(game.Interact()&&game.Onboard,"Board action failed");return true;}
            game.journeyCamera.yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;game.journeyCamera.Snap();InputSystem.QueueStateEvent(virtualKeyboard,new KeyboardState(Key.W));return false;
        }});
    }
    static void Depart(string name,int destination)=>Action(name,()=>{game.OpenDestinations();Check(game.TravelTo(destination)&&game.train.Moving,"Departure rejected");});
    static void Arrive(string name,int station)=>steps.Add(new Step{name=name,timeout=40,tick=()=>!game.train.Moving&&game.CurrentStation==station});
    static void Release(){if(virtualKeyboard!=null)InputSystem.QueueStateEvent(virtualKeyboard,new KeyboardState());}
    static void Tick()
    {
        if(!EditorApplication.isPlaying){Fail("Play mode stopped");return;}
        if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
        try
        {
            if(index>=steps.Count){report.state="passed";Finish();return;}
            Step s=steps[index];report.current=s.name;
            if(s.tick())
            {
                report.passed.Add(s.name);index++;since=EditorApplication.timeSinceStartup;Write();
            }
            else if(EditorApplication.timeSinceStartup-since>s.timeout)throw new Exception("Timed out at "+s.name+". Player position "+game.player.transform.position);
        }
        catch(Exception ex){Fail(ex.ToString());}
    }
    static void VerifyBackup()
    {
        string folder="Documentation/.journey-save-tests";Directory.CreateDirectory(folder);string path=Path.Combine(folder,"backup.json");
        if(File.Exists(path))File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");
        var first=new JourneySave();first.stars.Add("cloud_01");JourneySaveStore.Write(path,first);
        var second=new JourneySave();second.stars.Add("cloud_01");second.stars.Add("cloud_02");JourneySaveStore.Write(path,second);
        File.WriteAllText(path,"invalid json");var recovered=JourneySaveStore.Read(path,out string notice,out bool blocked);
        Check(!blocked&&recovered.stars.Count==1&&!string.IsNullOrEmpty(notice),"Backup recovery failed");
        File.WriteAllText(path+".bak","invalid json");JourneySaveStore.Read(path,out _,out blocked);Check(blocked,"Two corrupt saves should block silent overwrites");
    }
    static void Write()
    {
        report.stars=game.StarCount;report.station=game.CurrentStation;report.completed=game.Completed;report.arrivals=game.train.Arrivals;
        File.WriteAllText(Output,JsonUtility.ToJson(report,true));
    }
    static void Fail(string reason){report.state="failed";report.failure=reason;Finish();}
    static void Finish()
    {
        EditorApplication.update-=Tick;Release();if(game.Paused)game.TogglePause();
        if(virtualKeyboard!=null){InputSystem.RemoveDevice(virtualKeyboard);virtualKeyboard=null;}
        if(previousKeyboard!=null)previousKeyboard.MakeCurrent();Write();
        if(report.state=="passed"&&round<3){round++;EditorApplication.delayCall+=()=>Start();}
    }
}
