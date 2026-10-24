using UnityEngine;
using UnityEngine.InputSystem;

namespace CloudRail
{
    public sealed class CloudRailCamera : MonoBehaviour
    {
        public Transform train;
        public int view;
        public bool autoOrbit;
        public Vector3 overviewTarget = new Vector3(0, -3f, 0);
        public float yaw = -42f;
        public float elevation = 23f;
        public float distance = 83f;
        Vector3 velocity;

        void LateUpdate()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) view = 0;
                if (kb.digit2Key.wasPressedThisFrame) view = 1;
                if (kb.digit3Key.wasPressedThisFrame) view = 2;
                if (kb.cKey.wasPressedThisFrame) view = (view + 1) % 3;
                if (kb.spaceKey.wasPressedThisFrame) autoOrbit = !autoOrbit;
            }
            if (view == 0 && Mouse.current != null)
            {
                if (Mouse.current.rightButton.isPressed)
                {
                    var delta = Mouse.current.delta.ReadValue();
                    yaw += delta.x * .12f;
                    elevation = Mathf.Clamp(elevation - delta.y * .1f, 12f, 65f);
                }
                distance = Mathf.Clamp(distance - Mouse.current.scroll.ReadValue().y * .025f, 43f, 110f);
            }
            if (view == 0 && autoOrbit) yaw += Time.deltaTime * 2.1f;
            Pose(out var pos, out var target);
            transform.position = Vector3.SmoothDamp(transform.position, pos, ref velocity, .38f);
            var q = Quaternion.LookRotation(target - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, 1f - Mathf.Exp(-Time.deltaTime * 6f));
        }

        void Pose(out Vector3 p, out Vector3 target)
        {
            if (view == 1)
            {
                target = new Vector3(-2f, 3.4f, -6f);
                p = new Vector3(23, 13, -33);
            }
            else if (view == 2 && train != null)
            {
                target = train.position + Vector3.up * 1.8f;
                p = target - train.forward * 12f + train.right * 10f + Vector3.up * 6;
            }
            else
            {
                target = overviewTarget;
                p = target + Quaternion.Euler(elevation, yaw, 0) * (Vector3.back * distance);
            }
        }

        public void Snap()
        {
            Pose(out var p, out var target);
            transform.SetPositionAndRotation(p, Quaternion.LookRotation(target - p));
        }
    }
}
