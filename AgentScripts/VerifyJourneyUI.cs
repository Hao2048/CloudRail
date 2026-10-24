using System;
using System.IO;
using CloudRail;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifyJourneyUI
{
    static Mouse mouse,previous;
    static JourneyGame game;
    static Vector2 position;
    static int phase,lastFrame;
    public static string Start()
    {
        game=UnityEngine.Object.FindAnyObjectByType<JourneyGame>();
        if(!game.saveFile.Contains("verification"))throw new Exception("Use the test save slot.");
        if(!game.Paused)game.TogglePause();
        var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");EditorWindow.GetWindow(type).Focus();
        previous=Mouse.current;mouse=InputSystem.AddDevice<Mouse>("Journey UI verification");
        position=RectTransformUtility.WorldToScreenPoint(null,game.hud.resumeButton.transform.position);
        phase=0;lastFrame=-1;EditorApplication.update+=Tick;return "Pointer verification started";
    }
    static void Tick()
    {
        if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
        phase++;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=position,buttons=(ushort)(phase>=3&&phase<=5?1:0)});
        if(phase<10)return;
        EditorApplication.update-=Tick;
        bool passed=!game.Paused;
        File.WriteAllText("Documentation/里程碑02_鼠标验证.json","{\"resumeButtonViaPointer\":"+(passed?"true":"false")+",\"pointActionEnabled\":"+(UnityEngine.EventSystems.EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().point.action.enabled?"true":"false")+"}");
        InputSystem.RemoveDevice(mouse);if(previous!=null)previous.MakeCurrent();
    }
}
