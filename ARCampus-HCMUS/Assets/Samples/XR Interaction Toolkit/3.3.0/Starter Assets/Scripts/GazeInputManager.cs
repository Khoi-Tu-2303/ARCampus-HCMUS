using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    public class GazeInputManager : MonoBehaviour
    {
        
        const string k_EyeGazeLayoutName = "EyeGaze";

        [SerializeField]
        [Tooltip("Enable fallback to head tracking if eye tracking is unavailable.")]
        bool m_FallbackIfEyeTrackingUnavailable = true;

        public bool fallbackIfEyeTrackingUnavailable
        {
            get => m_FallbackIfEyeTrackingUnavailable;
            set => m_FallbackIfEyeTrackingUnavailable = value;
        }


        bool m_EyeTrackingDeviceFound;

        protected void Awake()
        {
            
            var inputDeviceList = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.EyeTracking, inputDeviceList);
            if (inputDeviceList.Count > 0)
            {
                Debug.Log("Eye tracking device found!", this);
                m_EyeTrackingDeviceFound = true;
                return;
            }

            foreach (var device in InputSystem.InputSystem.devices)
            {
                if (device.layout == k_EyeGazeLayoutName)
                {
                    Debug.Log("Eye gaze device found!", this);
                    m_EyeTrackingDeviceFound = true;
                    return;
                }
            }

            Debug.LogWarning($"Could not find a device that supports eye tracking on Awake. {this} has subscribed to device connected events and will activate the GameObject when an eye tracking device is connected.", this);

            InputDevices.deviceConnected += OnDeviceConnected;
            InputSystem.InputSystem.onDeviceChange += OnDeviceChange;

            gameObject.SetActive(m_FallbackIfEyeTrackingUnavailable);
        }

        protected void OnDestroy()
        {
            InputDevices.deviceConnected -= OnDeviceConnected;
            InputSystem.InputSystem.onDeviceChange -= OnDeviceChange;
        }

        void OnDeviceConnected(InputDevice inputDevice)
        {
            if (m_EyeTrackingDeviceFound || !inputDevice.characteristics.HasFlag(InputDeviceCharacteristics.EyeTracking))
                return;

            Debug.Log("Eye tracking device found!", this);
            m_EyeTrackingDeviceFound = true;
            gameObject.SetActive(true);
        }

        void OnDeviceChange(InputSystem.InputDevice device, InputDeviceChange change)
        {
            if (m_EyeTrackingDeviceFound || change != InputDeviceChange.Added)
                return;

            if (device.layout == k_EyeGazeLayoutName)
            {
                Debug.Log("Eye gaze device found!", this);
                m_EyeTrackingDeviceFound = true;
                gameObject.SetActive(true);
            }
        }
    }
}
