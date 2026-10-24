using UnityEngine;

namespace CloudRail
{
    public sealed class CloudTrain : MonoBehaviour
    {
        public SkyRailPath path;
        public Transform locomotive;
        public Transform carriage;
        public Transform[] wheels;
        public Transform[] connectingRods;
        public Transform chimney;
        public ParticleSystem steam;
        public float carriageSpacing = 5.7f;
        public float cruiseSpeed = 4.2f;
        public float acceleration = 1.1f;
        public float braking = 1.5f;
        public float dwellSeconds = 2.8f;
        public float initialProgress = .79f;
        public float DistanceTravelled { get; private set; }
        public float CurrentSpeed { get; private set; }
        public int CompletedStops { get; private set; }
        public string State { get; private set; } = "Cruising";
        float nextStop;
        float dwell;
        float wheelAngle;
        Vector3[] rodPositions;

        void Start()
        {
            DistanceTravelled = path.Length * initialProgress;
            nextStop = path.Length * .75f;
            while (nextStop <= DistanceTravelled + .1f) nextStop += path.Length;
            CurrentSpeed = cruiseSpeed;
            rodPositions = new Vector3[connectingRods.Length];
            for (int i = 0; i < connectingRods.Length; i++) rodPositions[i] = connectingRods[i].localPosition;
            Place();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dwell > 0)
            {
                dwell -= dt;
                CurrentSpeed = 0;
                State = "At station";
                if (dwell <= 0) { nextStop += path.Length; State = "Departing"; }
            }
            else
            {
                float remaining = nextStop - DistanceTravelled;
                float desired = Mathf.Min(cruiseSpeed, Mathf.Sqrt(Mathf.Max(0, 2f * braking * remaining)));
                CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, desired, (desired < CurrentSpeed ? braking : acceleration) * dt);
                float step = Mathf.Min(CurrentSpeed * dt, remaining);
                DistanceTravelled += step;
                wheelAngle += step / .44f * Mathf.Rad2Deg;
                if (remaining < .035f || step >= remaining)
                {
                    DistanceTravelled = nextStop;
                    CurrentSpeed = 0;
                    dwell = dwellSeconds;
                    CompletedStops++;
                    State = "At station";
                }
                else State = desired < cruiseSpeed - .01f ? "Braking" : CurrentSpeed < cruiseSpeed - .01f ? "Departing" : "Cruising";
            }
            Place();
            foreach (Transform w in wheels) w.localRotation = Quaternion.AngleAxis(wheelAngle, Vector3.right);
            float phase = wheelAngle * Mathf.Deg2Rad;
            for (int i = 0; i < connectingRods.Length; i++)
                connectingRods[i].localPosition = rodPositions[i] + new Vector3(0, .19f * Mathf.Cos(phase), .19f * Mathf.Sin(phase));
            if (steam != null)
            {
                var emission = steam.emission;
                emission.rateOverTime = CurrentSpeed < .1f ? 1f : 4.5f;
            }
        }

        void Place()
        {
            path.Evaluate(DistanceTravelled, out var p, out var r);
            locomotive.SetPositionAndRotation(p, r);
            path.Evaluate(DistanceTravelled - carriageSpacing, out p, out r);
            carriage.SetPositionAndRotation(p, r);
        }

        public void PreviewPose()
        {
            DistanceTravelled = path.Length * initialProgress;
            Place();
        }
    }
}
