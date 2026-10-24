using UnityEngine;

namespace CloudRail
{
    public sealed class JourneyTrain : MonoBehaviour
    {
        public CloudTrain rig;
        public JourneyGame game;
        public float cruiseSpeed = 2.7f;
        public float acceleration = 1.1f;
        public float braking = 1.4f;
        public float Distance { get; private set; }
        public float Speed { get; private set; }
        public int CurrentStation { get; private set; }
        public int Destination { get; private set; }
        public bool Moving { get; private set; }
        public string State { get; private set; } = "停站";
        public float TurnaroundFade { get; private set; }
        public int Arrivals { get; private set; }
        float targetDistance, wheelAngle, prepare;
        int direction=1, nextDirection=1;
        bool initialized;
        Vector3[] rodOrigins;
        float StopDistance(int station) => rig.path.StationDistance(station);

        public void Initialize(int station)
        {
            if(rodOrigins==null)
            {
                rodOrigins=new Vector3[rig.connectingRods.Length];
                for(int i=0;i<rodOrigins.Length;i++)rodOrigins[i]=rig.connectingRods[i].localPosition;
            }
            initialized=true;Moving=false;Speed=0;CurrentStation=station;Destination=station;
            Distance=StopDistance(station);direction=1;prepare=0;TurnaroundFade=0;State="停站";Pose();
        }
        public bool Depart(int destination)
        {
            if(!initialized||Moving||Mathf.Abs(destination-CurrentStation)!=1||!game.Onboard||!game.CanTravel(CurrentStation,destination))return false;
            Destination=destination;targetDistance=StopDistance(destination);
            nextDirection=destination>CurrentStation?1:-1;
            Moving=true;prepare=.9f;State="发车准备";return true;
        }
        void Update()
        {
            if(!initialized)return;
            float dt=Time.deltaTime;
            if(Moving)
            {
                if(prepare>0)
                {
                    prepare-=dt;
                    TurnaroundFade=Mathf.Sin(Mathf.Clamp01(prepare/.9f)*Mathf.PI);
                    if(prepare<.45f)direction=nextDirection;
                    if(prepare<=0){TurnaroundFade=0;State="行驶";}
                }
                else
                {
                    float remaining=Mathf.Abs(targetDistance-Distance);
                    float desired=Mathf.Min(cruiseSpeed,Mathf.Sqrt(Mathf.Max(0,2*braking*remaining)));
                    Speed=Mathf.MoveTowards(Speed,desired,(desired<Speed?braking:acceleration)*dt);
                    float step=Mathf.Min(Speed*dt,remaining);
                    Distance+=step*direction;wheelAngle+=step/.44f*Mathf.Rad2Deg;
                    State=desired<cruiseSpeed-.01f?"进站减速":Speed<cruiseSpeed-.01f?"缓缓驶离":"行驶";
                    if(remaining<.025f||step>=remaining)
                    {
                        Distance=targetDistance;Speed=0;Moving=false;CurrentStation=Destination;State="停站";Arrivals++;
                        Pose();game.Arrive(CurrentStation);
                    }
                }
            }
            Pose();
            foreach(var w in rig.wheels)w.localRotation=Quaternion.AngleAxis(wheelAngle,Vector3.right);
            float phase=wheelAngle*Mathf.Deg2Rad;
            for(int i=0;i<rodOrigins.Length;i++)rig.connectingRods[i].localPosition=rodOrigins[i]+new Vector3(0,.19f*Mathf.Cos(phase),.19f*Mathf.Sin(phase));
            var emission=rig.steam.emission;emission.rateOverTime=Speed>.1f?4.5f:1f;
        }
        void Pose()
        {
            rig.path.Evaluate(Distance,out var p,out var q);
            rig.locomotive.SetPositionAndRotation(p,direction>0?q:q*Quaternion.Euler(0,180,0));
            rig.path.Evaluate(Distance-direction*rig.carriageSpacing,out p,out q);
            rig.carriage.SetPositionAndRotation(p,direction>0?q:q*Quaternion.Euler(0,180,0));
        }
    }
}
