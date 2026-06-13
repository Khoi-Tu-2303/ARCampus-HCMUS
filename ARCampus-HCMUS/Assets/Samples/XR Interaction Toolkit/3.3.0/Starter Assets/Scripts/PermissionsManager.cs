using System;
using System.Collections.Generic;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using UnityEngine.Events;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    [DefaultExecutionOrder(-9999)]
    public class PermissionsManager : MonoBehaviour
    {
        const string k_DefaultPermissionId = "com.oculus.permission.USE_SCENE";

        [SerializeField, Tooltip("Enables or disables the processing of permissions on Awake. If disabled, permissions will not be processed until the ProcessPermissions method is called.")]
        bool m_ProcessPermissionsOnAwake = true;

        [SerializeField, Tooltip("The system permissions to request when this component starts.")]
        List<PermissionRequestGroup> m_PermissionGroups = new List<PermissionRequestGroup>();

        PermissionRequestGroup m_CurrentPlatformPermissionGroup = new PermissionRequestGroup();

        [Serializable]
        class PermissionRequestGroup
        {
            [Tooltip("The platform type for which these permissions is intended for.")]
            public XRPlatformType platformType;
            public List<PermissionRequest> permissions;
        }

        [Serializable]
        class PermissionRequest
        {
            [Tooltip("The Android system permission to request when this component starts.")]
            public string permissionId = k_DefaultPermissionId;

            [Tooltip("Whether to request permission from the operating system.")]
            public bool enabled = true;

            [HideInInspector]
            public bool requested = false;

            [HideInInspector]
            public bool responseReceived = false;

            [HideInInspector]
            public bool granted = false;

            public UnityEvent<string> onPermissionGranted;

            public UnityEvent<string> onPermissionDenied;
        }

        void Awake()
        {
            if (m_ProcessPermissionsOnAwake)
                ProcessPermissions();
        }

        public void ProcessPermissions()
        {
#if UNITY_ANDROID
            
            var currentPlatform = XRPlatformUnderstanding.CurrentPlatform;
            m_CurrentPlatformPermissionGroup = m_PermissionGroups.Find(g => g.platformType == currentPlatform);
            if (m_CurrentPlatformPermissionGroup == null)
            {
                
                
                return;
            }

            var permissionIds = new List<string>();

            
            
            for (var i = 0; i < m_CurrentPlatformPermissionGroup.permissions.Count; i++)
            {
                var permission = m_CurrentPlatformPermissionGroup.permissions[i];
                if (!permission.enabled)
                    continue;

                
                if (!Permission.HasUserAuthorizedPermission(permission.permissionId) && !permission.requested)
                {
                    permissionIds.Add(permission.permissionId);
                    permission.requested = true;
                }
                else
                {
                    Debug.Log($"User has permission for: {permission.permissionId}", this);
                }
            }

            
            if (permissionIds.Count > 0)
            {
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionDenied += OnPermissionDenied;
                callbacks.PermissionGranted += OnPermissionGranted;

                Permission.RequestUserPermissions(permissionIds.ToArray(), callbacks);
            }
#endif 
        }

        void OnPermissionGranted(string permissionStr)
        {
            
            var permission = m_CurrentPlatformPermissionGroup.permissions.Find(p => p.permissionId == permissionStr);
            if (permission == null)
            {
                Debug.LogWarning($"Permission granted callback received for an unexpected permission request, permission ID {permissionStr}", this);
                return;
            }

            
            permission.granted = true;
            permission.responseReceived = true;

            Debug.Log($"User granted permission for: {permissionStr}", this);
            permission.onPermissionGranted.Invoke(permissionStr);
        }

        void OnPermissionDenied(string permissionStr)
        {
            
            var permission = m_CurrentPlatformPermissionGroup.permissions.Find(p => p.permissionId == permissionStr);
            if (permission == null)
            {
                Debug.LogWarning($"Permission denied callback received for an unexpected permission request, permission ID {permissionStr}", this);
                return;
            }

            
            permission.granted = false;
            permission.responseReceived = true;

            Debug.LogWarning($"User denied permission for: {permissionStr}", this);
            permission.onPermissionDenied.Invoke(permissionStr);
        }
    }
}
