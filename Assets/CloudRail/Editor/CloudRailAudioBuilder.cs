using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CloudRail.Editor
{
    public static partial class CloudRailBuilder
    {
        static void ConfigureJourneyAudio(JourneyGame game)
        {
            var sound=game.GetComponent<JourneyAudio>();if(sound==null)sound=game.gameObject.AddComponent<JourneyAudio>();
            sound.game=game;game.soundscape=sound;
            string[] names={"Cloudline_Theme","Cloud_Wind","Rail_Rhythm","Departure_Whistle","Clock_Awakening"};
            var clips=new AudioClip[names.Length];
            for(int i=0;i<names.Length;i++)
            {
                string path=Root+"/Audio/"+names[i]+".wav";
                var importer=AssetImporter.GetAtPath(path) as AudioImporter;
                if(importer==null)throw new InvalidOperationException("Missing generated audio: "+path);
                var settings=importer.defaultSampleSettings;
                settings.loadType=i==0?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.7f;
                importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.SaveAndReimport();
                clips[i]=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            sound.theme=clips[0];sound.wind=clips[1];sound.rail=clips[2];sound.whistle=clips[3];sound.awakening=clips[4];
            EditorUtility.SetDirty(sound);EditorUtility.SetDirty(game);
        }
        [MenuItem("Cloud Rail/Add Journey Soundscape")]
        public static void AddJourneySoundscape()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before editing the journey.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!=JourneyScenePath)throw new InvalidOperationException("Open the saved Journey scene first.");
            var game=UnityEngine.Object.FindAnyObjectByType<JourneyGame>();
            if(game==null||game.hud==null)throw new InvalidOperationException("The journey UI is missing.");
            ConfigureJourneyAudio(game);LoadJourneyFont();
            var hud=game.hud;var panel=hud.pausePanel.transform.Find("Content") as RectTransform;
            if(panel==null||panel.Find("Effects label")==null)throw new InvalidOperationException("The existing pause menu was not found.");
            panel.sizeDelta=new Vector2(620,590);
            (panel.Find("Title") as RectTransform).anchoredPosition=new Vector2(0,225);
            if(hud.musicSlider==null)
            {
                Label("Music label",panel,"音乐音量",new Vector2(-175,132),new Vector2(150,36),23,Ink);
                hud.musicSlider=Slider("Music volume",panel,new Vector2(66,132),0,1);
            }
            (panel.Find("Effects label") as RectTransform).anchoredPosition=new Vector2(-175,56);
            (hud.volumeSlider.transform as RectTransform).anchoredPosition=new Vector2(66,56);
            (panel.Find("Sensitivity label") as RectTransform).anchoredPosition=new Vector2(-175,-20);
            (hud.sensitivitySlider.transform as RectTransform).anchoredPosition=new Vector2(66,-20);
            (hud.resumeButton.transform as RectTransform).anchoredPosition=new Vector2(0,-117);
            (hud.restartButton.transform as RectTransform).anchoredPosition=new Vector2(0,-199);
            EditorUtility.SetDirty(hud);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("Journey soundscape saved: original 96-second score, wind, wheels, whistle, clock and independent music setting.");
        }
    }
}
