using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace CloudRail
{
    [RequireComponent(typeof(InputSystemUIInputModule))]
    [DefaultExecutionOrder(-200)]
    public sealed class JourneyUIInput : MonoBehaviour
    {
        public InputActionAsset actions;
        void Awake()
        {
            Bind(GetComponent<InputSystemUIInputModule>(),actions);
            actions.FindActionMap("UI",true).Enable();
        }
        public static void Bind(InputSystemUIInputModule module,InputActionAsset asset)
        {
            // Reference the project's persistent action asset, never an editor-created default instance.
            module.actionsAsset=asset;
            module.point=InputActionReference.Create(asset.FindAction("UI/Point",true));
            module.leftClick=InputActionReference.Create(asset.FindAction("UI/Click",true));
            module.rightClick=InputActionReference.Create(asset.FindAction("UI/RightClick",true));
            module.middleClick=InputActionReference.Create(asset.FindAction("UI/MiddleClick",true));
            module.scrollWheel=InputActionReference.Create(asset.FindAction("UI/ScrollWheel",true));
            module.move=InputActionReference.Create(asset.FindAction("UI/Navigate",true));
            module.submit=InputActionReference.Create(asset.FindAction("UI/Submit",true));
            module.cancel=InputActionReference.Create(asset.FindAction("UI/Cancel",true));
            module.trackedDevicePosition=InputActionReference.Create(asset.FindAction("UI/TrackedDevicePosition",true));
            module.trackedDeviceOrientation=InputActionReference.Create(asset.FindAction("UI/TrackedDeviceOrientation",true));
        }
    }
}
