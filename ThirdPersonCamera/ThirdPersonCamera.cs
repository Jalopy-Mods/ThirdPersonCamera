using JaLoader;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using UnityEngine;
using static DG.Tweening.DOTweenModuleUtils;
using Physics = UnityEngine.Physics;

namespace ThirdPersonCamera
{
    public class ThirdPersonCamera : Mod
    {
        public override string ModID => "ThirdPersonCamera";
        public override string ModName => "External Camera";
        public override string ModAuthor => "Leaxx";
        public override string ModDescription => "Adds an external (third person) camera to the Laika, which can be rotated around the car";
        public override string ModVersion => "1.0";
        public override string GitHubLink => "https://github.com/Jalopy-Mods/ThirdPersonCamera";
        public override WhenToInit WhenToInit => WhenToInit.InGame;
        public override List<(string, string, string)> Dependencies => new List<(string, string, string)>()
        {
        };

        public override bool UseAssets => false;

        private GameObject mainCamera;
        private GameObject thirdPersonCamera;
        private bool toggled;

        private MouseLook mouseLook;
        private int oldSens;
        private AudioListener listener;
        private ThirdPersonCameraScript cameraScript;

        public override void SettingsDeclaration()
        {
            base.SettingsDeclaration();

            InstantiateSettings();
            AddKeybind("ToggleKey", "Toggle External Camera Key", KeyCode.C);
            AddKeybind("ResetCameraKey", "Reset the external camera back to its default position", KeyCode.R);
        }

        public override void Start()
        {
            base.Start();

            var car = ModHelper.Instance.laika.transform;
            thirdPersonCamera = new GameObject("ThirdPersonCamera");
            thirdPersonCamera.SetActive(false);
            thirdPersonCamera.transform.SetParent(car);
            thirdPersonCamera.transform.SetParent(null, true);
            cameraScript = thirdPersonCamera.AddComponent<ThirdPersonCameraScript>();

            cameraScript.target = car;
            thirdPersonCamera.AddComponent<FlareLayer>();
            thirdPersonCamera.AddComponent<Camera>();
            thirdPersonCamera.AddComponent<AudioListener>();

            mainCamera = Camera.main.gameObject;
            listener = mainCamera.GetComponent<AudioListener>();
            mouseLook = mainCamera.GetComponent<MouseLook>();
        }

        public override void Update()
        {
            base.Update();

            if (!mouseLook.isSat)
            {
                if (toggled)
                    Toggle();

                return;
            }

            if (Input.GetKeyDown(GetPrimaryKeybind("ToggleKey")))
                Toggle();

            if (Input.GetKeyDown(GetPrimaryKeybind("ResetCameraKey")))
                cameraScript.ResetCamera();
        }

        private void Toggle()
        {
            toggled = !toggled;
            thirdPersonCamera.SetActive(toggled);
            listener.enabled = !toggled;
            mainCamera.GetComponent<Camera>().enabled = !toggled;
            GameObject.Find("BottomEyeLid").SetActive(!toggled);
            GameObject.Find("TopEyeLid").SetActive(!toggled);
        }
    }

    public class ThirdPersonCameraScript : MonoBehaviour
    {
        public Transform target;
        private Vector3 targetPosition;
        private float distance = 6.5f;
        private float smoothSpeed = 1f;
        private float yMinLimit = -40f;
        private float yMaxLimit = 80f;

        private float x = 0.0f;
        private float y = 0.0f;
        private float currentDistance = 15f;
        private float maxDistance = 20f;
        private float minDistance = 6f;

        private Vector3 lastPositionBeforeRaycast;

        private bool paused;

        void Start()
        {
            Vector3 angles = transform.eulerAngles;
            x = angles.y;
            y = angles.x;
            lastPositionBeforeRaycast = transform.position;

            EventsManager.Instance.OnPause += OnPaused;
            EventsManager.Instance.OnUnpause += OnUnpaused;

            ResetCamera();
        }

        private void OnPaused()
        {
            paused = true;
        }

        private void OnUnpaused()
        {
            paused = false;
        }

        void Update()
        {
            if (paused)
                return;

            targetPosition = target.position;
            targetPosition += target.transform.up * 0.75f;
            targetPosition -= target.transform.forward * 0.25f;

            if (Input.mouseScrollDelta.y != 0)
            {
                currentDistance -= Input.mouseScrollDelta.y;
                currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            }
        }

        public void ResetCamera()
        {
            x = -88f;
            y = 0;
        }

        void LateUpdate()
        {
            if (paused)
                return;

            Vector3 desiredPosition = targetPosition - (transform.forward * distance);

            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

            RaycastHit hit;
            if (Physics.Raycast(targetPosition, -transform.forward, out hit, distance))
            {
                distance = hit.distance - 0.5f;
            }
            else
            {
                lastPositionBeforeRaycast = smoothedPosition;
                distance = currentDistance;
            }

            transform.position = Vector3.Lerp(transform.position, lastPositionBeforeRaycast, smoothSpeed);

            x += Input.GetAxis("Mouse X") * 2f;
            y -= Input.GetAxis("Mouse Y") * 2f;

            y = ClampAngle(y, yMinLimit, yMaxLimit);

            Quaternion rotation = Quaternion.Euler(y, x, 0);
            transform.rotation = rotation;
        }

        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F)
                angle += 360F;
            if (angle > 360F)
                angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }
    }
}
