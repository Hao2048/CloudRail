using UnityEngine;

namespace CloudRail
{
    public sealed class SkyRailPath : MonoBehaviour
    {
        public float radiusX = 25f;
        public float radiusZ = 18f;
        public float railHeight = .94f;
        public Vector3[] scenicBezier;
        [SerializeField] float[] stationDistances;
        [SerializeField] Vector3[] samples;
        [SerializeField] float[] distances;
        public float Length => distances == null || distances.Length == 0 ? 0 : distances[distances.Length - 1];

        public bool HasScenicRoute => scenicBezier!=null&&scenicBezier.Length>=4&&(scenicBezier.Length-1)%3==0;
        public float StationDistance(int station) => HasScenicRoute&&stationDistances!=null&&stationDistances.Length==3?stationDistances[Mathf.Clamp(station,0,2)]:Length*(.75f+station*.25f);
        public Vector3 Point(float angle)
        {
            if(!HasScenicRoute)return transform.TransformPoint(new Vector3(radiusX*Mathf.Cos(angle),railHeight,radiusZ*Mathf.Sin(angle)));
            int count=(scenicBezier.Length-1)/3;float u=Mathf.Clamp01(angle/(Mathf.PI*2))*count;int segment=Mathf.Min(Mathf.FloorToInt(u),count-1);float t=u-segment,v=1-t;int i=segment*3;
            return transform.TransformPoint(v*v*v*scenicBezier[i]+3*v*v*t*scenicBezier[i+1]+3*v*t*t*scenicBezier[i+2]+t*t*t*scenicBezier[i+3]);
        }

        public void Bake()
        {
            const int count = 2048;
            samples = new Vector3[count + 1];
            distances = new float[count + 1];
            for (int i = 0; i <= count; i++)
            {
                samples[i] = Point(i * 2f * Mathf.PI / count);
                if (i > 0) distances[i] = distances[i - 1] + Vector3.Distance(samples[i - 1], samples[i]);
            }
            if(HasScenicRoute)
            {
                stationDistances=new float[3];Vector3[] stops={new Vector3(0,railHeight,-35),new Vector3(46,railHeight,0),new Vector3(0,railHeight,35)};
                for(int k=0;k<3;k++){float best=float.PositiveInfinity;for(int i=0;i<count;i++){float delta=(samples[i]-transform.TransformPoint(stops[k])).sqrMagnitude;if(delta<best){best=delta;stationDistances[k]=distances[i];}}}
            }
        }

        public void Evaluate(float distance, out Vector3 position, out Quaternion rotation)
        {
            if (Length <= 0) Bake();
            float d = Mathf.Repeat(distance, Length);
            int lo = 0, hi = distances.Length - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (distances[mid] < d) lo = mid; else hi = mid;
            }
            float t = Mathf.InverseLerp(distances[lo], distances[hi], d);
            position = Vector3.Lerp(samples[lo], samples[hi], t);
            rotation = Quaternion.LookRotation((samples[hi] - samples[lo]).normalized, Vector3.up);
        }
    }
}
