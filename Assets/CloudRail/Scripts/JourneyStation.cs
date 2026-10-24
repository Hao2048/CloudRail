using UnityEngine;

namespace CloudRail
{
    public sealed class JourneyStation : MonoBehaviour
    {
        public int index;
        public string displayName;
        public Transform safeSpawn;
        public Transform device;
        public Vector3 outward;
        public Renderer[] signalParts;
        public Transform towerRing;
        public Vector3 BoardPoint(Transform carriage)
        {
            var p = carriage.position - outward * 2.4f;
            p.y = 1.02f;
            return p;
        }
        public void SetSignal(bool enabled)
        {
            foreach (var r in signalParts)
            {
                if (r == null) continue;
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", enabled ? new Color(1,.79f,.32f) : new Color(.25f,.45f,.5f));
                block.SetColor("_EmissionColor", enabled ? new Color(1,.55f,.12f) * .9f : Color.black);
                r.SetPropertyBlock(block);
            }
        }
    }
}
