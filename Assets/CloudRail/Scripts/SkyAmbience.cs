using UnityEngine;

namespace CloudRail
{
    public sealed class SkyAmbience : MonoBehaviour
    {
        public Transform[] clouds;
        public Transform[] floatingIslets;
        public Transform orrery;
        Vector3[] cloudOrigins, islandOrigins;
        void Start()
        {
            cloudOrigins = new Vector3[clouds.Length];
            for (int i = 0; i < clouds.Length; i++) cloudOrigins[i] = clouds[i].position;
            islandOrigins = new Vector3[floatingIslets.Length];
            for (int i = 0; i < floatingIslets.Length; i++) islandOrigins[i] = floatingIslets[i].position;
        }
        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < clouds.Length; i++)
                clouds[i].position = cloudOrigins[i] + new Vector3(Mathf.Sin(t * .035f + i) * 1.7f, Mathf.Sin(t * .06f + i) * .17f, 0);
            for (int i = 0; i < floatingIslets.Length; i++)
                floatingIslets[i].position = islandOrigins[i] + Vector3.up * Mathf.Sin(t * .32f + i * 1.6f) * .16f;
            if (orrery != null) orrery.Rotate(0, Time.deltaTime * 8f, 0, Space.Self);
        }
    }
}
