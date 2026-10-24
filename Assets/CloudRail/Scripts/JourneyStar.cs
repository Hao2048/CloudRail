using UnityEngine;

namespace CloudRail
{
    public sealed class JourneyStar : MonoBehaviour
    {
        public string starId;
        public bool optional;
        public Transform visual;
        public JourneyGame game;
        Vector3 origin;
        void Awake() { origin = visual.localPosition; }
        void Update()
        {
            visual.localPosition = origin + Vector3.up * (Mathf.Sin(Time.time * 2f + transform.position.x) * .13f);
            visual.Rotate(0,Time.deltaTime*45f,0,Space.Self);
            if (game.CanCollect && Vector3.Distance(game.player.transform.position + Vector3.up * .75f, transform.position) < 1.2f)
                game.Collect(this);
        }
    }
}
