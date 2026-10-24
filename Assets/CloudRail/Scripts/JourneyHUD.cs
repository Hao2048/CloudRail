using TMPro;
using UnityEngine;

namespace CloudRail
{
    public sealed class JourneyHUD : MonoBehaviour
    {
        public JourneyGame game;
        public TMP_Text objective,starCount,stationName,prompt,toast;
        public GameObject introPanel,pausePanel,destinationsPanel,endingPanel,restartPanel;
        public UnityEngine.UI.Button beginButton,resumeButton,restartButton,confirmRestartButton,cancelRestartButton,continueButton,cancelDestinationsButton;
        public UnityEngine.UI.Button[] destinationButtons;
        public TMP_Text[] destinationLabels;
        public UnityEngine.UI.Slider volumeSlider,sensitivitySlider,musicSlider;
        public GameObject helpPanel;
        public UnityEngine.UI.Button helpButton,closeHelpButton,quitButton;
        public UnityEngine.UI.Toggle stableToggle,qualityToggle;
        public CanvasGroup curtain;
        float toastUntil;
        readonly int[] destinationIndices={-1,-1};
        void Awake()
        {
            if(helpButton!=null)helpButton.onClick.AddListener(game.OpenHelp);
            if(closeHelpButton!=null)closeHelpButton.onClick.AddListener(game.CloseHelp);
            if(quitButton!=null)quitButton.onClick.AddListener(Application.Quit);
            if(stableToggle!=null)stableToggle.onValueChanged.AddListener(game.SetReduceMotion);
            if(qualityToggle!=null)qualityToggle.onValueChanged.AddListener(game.SetLowEffects);
            beginButton.onClick.AddListener(game.Begin);resumeButton.onClick.AddListener(game.TogglePause);
            restartButton.onClick.AddListener(game.AskRestart);confirmRestartButton.onClick.AddListener(game.Restart);
            cancelRestartButton.onClick.AddListener(game.CancelRestart);continueButton.onClick.AddListener(game.ContinueExploring);
            cancelDestinationsButton.onClick.AddListener(game.CloseDestinations);
            for(int i=0;i<destinationButtons.Length;i++){int index=i;destinationButtons[i].onClick.AddListener(()=>game.TravelTo(destinationIndices[index]));}
            if(musicSlider!=null)musicSlider.onValueChanged.AddListener(game.SetMusicVolume);
            volumeSlider.onValueChanged.AddListener(game.SetVolume);sensitivitySlider.onValueChanged.AddListener(game.SetSensitivity);
        }
        void Update(){toast.gameObject.SetActive(Time.unscaledTime<toastUntil);}
        public void Refresh(string goal,int count,string station,string hint)
        {
            if(objective.text!=goal)objective.text=goal;
            string stars="星光  "+count+" / 9";if(starCount.text!=stars)starCount.text=stars;
            if(stationName.text!=station)stationName.text=station;if(prompt.text!=hint)prompt.text=hint;
        }
        public void Toast(string text,float seconds=4){toast.text=text;toastUntil=Time.unscaledTime+seconds;toast.gameObject.SetActive(true);}
        public void SetIntro(bool value)=>introPanel.SetActive(value);
        public void SetPause(bool value)=>pausePanel.SetActive(value);
        public void SetEnding(bool value)=>endingPanel.SetActive(value);
        public void SetRestart(bool value)=>restartPanel.SetActive(value);
        public void SetCurtain(float alpha){curtain.alpha=alpha;curtain.blocksRaycasts=alpha>.05f;}
        public void InitializeSettings(float volume,float sensitivity,float music=.35f,bool stable=true,bool low=false){if(stableToggle!=null)stableToggle.SetIsOnWithoutNotify(stable);if(qualityToggle!=null)qualityToggle.SetIsOnWithoutNotify(low);if(musicSlider!=null)musicSlider.SetValueWithoutNotify(music);volumeSlider.SetValueWithoutNotify(volume);sensitivitySlider.SetValueWithoutNotify(sensitivity);}
        public void SetDestinations(bool value)
        {
            destinationsPanel.SetActive(value);if(!value)return;
            for(int i=0;i<2;i++)
            {
                int destination=game.CurrentStation+(i==0?-1:1);destinationIndices[i]=destination;
                bool valid=destination>=0&&destination<game.stations.Length;
                destinationButtons[i].gameObject.SetActive(valid);if(!valid)continue;
                bool unlocked=game.CanTravel(game.CurrentStation,destination);destinationButtons[i].interactable=unlocked;
                destinationLabels[i].text=game.stations[destination].displayName+(unlocked?"   ·   确认发车":"   ·   需累计 "+(Mathf.Min(game.CurrentStation,destination)==0?3:6)+" 枚星光并激活装置");
            }
        }
        public void ClosePanels(){if(helpPanel!=null)helpPanel.SetActive(false);introPanel.SetActive(false);pausePanel.SetActive(false);endingPanel.SetActive(false);restartPanel.SetActive(false);destinationsPanel.SetActive(false);}
    }
}
