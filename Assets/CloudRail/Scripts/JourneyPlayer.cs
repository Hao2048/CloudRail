using UnityEngine;
using UnityEngine.InputSystem;

namespace CloudRail
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class JourneyPlayer : MonoBehaviour
    {
        public JourneyGame game;
        public Transform model;
        public float moveSpeed = 3.5f;
        public bool Walking { get; private set; }
        CharacterController controller;
        float verticalVelocity;
        Transform view;
        Vector3 modelOrigin;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            view = Camera.main.transform;
            modelOrigin = model.localPosition;
        }
        void Update()
        {
            Walking = false;
            if (!game.CanWalk || !controller.enabled) return;
            var kb = Keyboard.current;
            Vector2 input = Vector2.zero;
            if (kb != null)
            {
                input.x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
                input.y = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
            }
            input = Vector2.ClampMagnitude(input, 1);
            Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;
            Vector3 move = forward * input.y + right * input.x;
            if (move.sqrMagnitude > .01f)
            {
                Walking = true;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 1f - Mathf.Exp(-Time.deltaTime * 12));
            }
            if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
            verticalVelocity = Mathf.Max(verticalVelocity - 20f * Time.deltaTime, -25f);
            controller.Move((move * moveSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);
            model.localPosition = modelOrigin + Vector3.up * (game.ReduceMotion?0:Walking ? Mathf.Sin(Time.time * 13f) * .025f : Mathf.Sin(Time.time * 2) * .008f);
            if (transform.position.y < -5f) game.Respawn();
        }
        public void Place(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(position, rotation);
            verticalVelocity = 0;
            controller.enabled = true;
        }
        public void SetSeated(Transform carriage)
        {
            controller.enabled = false;
            transform.SetParent(carriage, false);
            transform.localPosition = Vector3.up * 1.2f;
            model.gameObject.SetActive(false);
        }
        public void ShowModel() { model.gameObject.SetActive(true); }
    }
}
