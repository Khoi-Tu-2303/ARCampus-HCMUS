using System;

#if OPENXR_1_6_OR_NEWER
using UnityEngine.XR.OpenXR;
#endif

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    public enum XRPlatformType
    {
        [InspectorName("OpenXR: Meta")]
        OpenXRMeta,

        [InspectorName("OpenXR: Android XR")]
        OpenXRAndroidXR,

        [InspectorName("OpenXR: Other")]
        OpenXROther,

        Other,
    }

    public static class XRPlatformUnderstanding
    {
        const string k_RuntimeNameMeta = "Oculus";
        const string k_RuntimeNameAndroidXR = "Android XR";

        public static XRPlatformType CurrentPlatform
        {
            get
            {
                if (!s_Initialized)
                {
                    s_CurrentPlatform = GetCurrentXRPlatform();
                    s_Initialized = true;
                }
                return s_CurrentPlatform;
            }
        }

        static XRPlatformType s_CurrentPlatform = XRPlatformType.Other;

        static bool s_Initialized;

        static XRPlatformType GetCurrentXRPlatform()
        {
            
            if (s_Initialized)
                return s_CurrentPlatform;

#if OPENXR_1_6_OR_NEWER
            try
            {
                var openXRRuntimeName = OpenXRRuntime.name;
                if (string.IsNullOrEmpty(openXRRuntimeName))
                {
                    s_CurrentPlatform = XRPlatformType.Other;
                }
                else
                {
                    switch (openXRRuntimeName)
                    {
                        case k_RuntimeNameMeta:
                            Debug.Log("Meta runtime detected.");
                            s_CurrentPlatform = XRPlatformType.OpenXRMeta;
                            break;
                        case k_RuntimeNameAndroidXR:
                            Debug.Log("Android XR runtime detected.");
                            s_CurrentPlatform = XRPlatformType.OpenXRAndroidXR;
                            break;
                        default:
                            Debug.Log($"Unknown OpenXR runtime detected: \"{openXRRuntimeName}\"");
                            s_CurrentPlatform = XRPlatformType.OpenXROther;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to get OpenXR runtime: {e.Message}");
                s_CurrentPlatform = XRPlatformType.Other;
            }
#else
            s_CurrentPlatform = XRPlatformType.Other;
#endif

            s_Initialized = true;
            return s_CurrentPlatform;
        }
    }
}
