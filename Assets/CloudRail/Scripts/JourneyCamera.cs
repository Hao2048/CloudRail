using UnityEngine;
using UnityEngine.InputSystem;

namespace CloudRail
{
    public sealed class JourneyCamera : MonoBehaviour
    {
        public JourneyGame game;
        public float yaw;
        public float elevation = 28;
        public float walkDistance = 9;
        public int trainView;
        Vector3 velocity;
        float rideZoom=1;
        void LateUpdate()
        {
            if(game.Paused)return;
            if (!game.Paused && !game.MenuOpen)
            {
                var mouse = Mouse.current;
                if(game.Onboard&&mouse!=null)
                    rideZoom=Mathf.Clamp(rideZoom-mouse.scroll.ReadValue().y*.0018f+(mouse.middleButton.isPressed?mouse.delta.ReadValue().y*.004f:0),.5f,2.2f);
                if (!game.Onboard && mouse != null)
                {
                    if (mouse.rightButton.isPressed)
                    {
                        var delta = mouse.delta.ReadValue() * game.Sensitivity;
                        yaw += delta.x * .14f;
                        elevation = Mathf.Clamp(elevation - delta.y * .12f, 14, 60);
                    }
                    walkDistance = Mathf.Clamp(walkDistance - mouse.scroll.ReadValue().y * .012f + (mouse.middleButton.isPressed?mouse.delta.ReadValue().y*.045f:0), 4, 24);
                }
                if (game.Onboard && Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
                    trainView = (trainView + 1) % 3;
            }
            Pose(out var position, out var target);
            if(game.Onboard&&!game.ReduceMotion)position+=Vector3.up*Mathf.Sin(Time.time*1.7f)*.12f;
            transform.position = Protect(Vector3.SmoothDamp(transform.position, position, ref velocity, .25f, float.PositiveInfinity, Time.deltaTime),target);
            if ((target-transform.position).sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target-transform.position), 1f-Mathf.Exp(-Time.deltaTime*9));
        }
        void Pose(out Vector3 position, out Vector3 target)
        {
            if (game.Onboard)
            {
                Transform train = game.train.rig.locomotive;
                target = train.position + Vector3.up * 1.8f;
                if (trainView == 0) position = target - train.forward * 11 + train.right * 9 + Vector3.up * 6;
                else if (trainView == 1) position = target + train.right * 15 + Vector3.up * 4;
                else position = target - train.forward * 23 + train.right * 19 + Vector3.up * 15;
                position=target+(position-target)*rideZoom;
            }
            else
            {
                target = game.player.transform.position + Vector3.up * 1.35f;
                Vector3 offset = Quaternion.Euler(elevation,yaw,0) * Vector3.back * walkDistance;
                position = target + offset;

            }
        }
        Vector3 Protect(Vector3 position,Vector3 target)
        {
            Vector3 offset=position-target;float distance=offset.magnitude;
            if(distance>.001f&&Physics.SphereCast(target,.22f,offset/distance,out var hit,distance,(1<<0)|(1<<8),QueryTriggerInteraction.Collide))
                return target+offset/distance*Mathf.Max(.25f,hit.distance-.2f);
            return position;
        }
        public void Snap()
        {
            velocity=Vector3.zero;Pose(out var p,out var target);p=Protect(p,target);
            transform.SetPositionAndRotation(p,Quaternion.LookRotation(target-p));
        }
    }
}
