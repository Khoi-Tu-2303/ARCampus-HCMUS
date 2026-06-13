#if AR_FOUNDATION_PRESENT
using UnityEngine.Events;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets
{
    [RequireComponent(typeof(Rigidbody))]
    public class ARContactSpawnTrigger : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("If enabled, spawning will be blocked if the active interactor in the associated XR Interaction Group is hovering or selecting an interactable object.")]
        bool m_BlockSpawnDuringInteraction;

        public bool blockSpawnDuringInteraction
        {
            get => m_BlockSpawnDuringInteraction;
            set => m_BlockSpawnDuringInteraction = value;
        }

        [SerializeField]
        [Tooltip("XR Interaction Group associated with this contact spawn trigger.")]
        XRInteractionGroup m_InteractionGroup;

        public XRInteractionGroup interactionGroup
        {
            get => m_InteractionGroup;
            set => m_InteractionGroup = value;
        }

        [SerializeField]
        [Tooltip("Whether to require that the AR Plane has an alignment of horizontal up to spawn on it.")]
        bool m_RequireHorizontalUpSurface;

        public bool requireHorizontalUpSurface
        {
            get => m_RequireHorizontalUpSurface;
            set => m_RequireHorizontalUpSurface = value;
        }

   
        public UnityEvent<Vector3, Vector3> objectSpawnTriggered
        {
            get => m_ObjectSpawnTriggered;
            set => m_ObjectSpawnTriggered = value;
        }

        [Header("Events")]
        [SerializeField]
        [Tooltip("Calls the methods in its invocation list when an object is spawned.")]
        UnityEvent<Vector3, Vector3> m_ObjectSpawnTriggered = new UnityEvent<Vector3, Vector3>();

        void Start()
        {
            if (m_InteractionGroup == null)
            {
                m_InteractionGroup = GetComponentInParent<XRInteractionGroup>();

                if (m_BlockSpawnDuringInteraction && m_InteractionGroup == null)
                    Debug.LogWarning("Interaction group could be found. Spawning objects will not be blocked during hover or select interaction.", this);
            }
        }

       
        void OnTriggerEnter(Collider other)
        {
            if ((blockSpawnDuringInteraction && IsInteractionBlockingSpawn()) ||
                !TryGetSpawnSurfaceData(other, out var surfacePosition, out var surfaceNormal))
                return;

            var infinitePlane = new Plane(surfaceNormal, surfacePosition);
            var contactPoint = infinitePlane.ClosestPointOnPlane(transform.position);
            m_ObjectSpawnTriggered.Invoke(contactPoint, surfaceNormal);
        }

        public bool TryGetSpawnSurfaceData(Collider objectCollider, out Vector3 surfacePosition, out Vector3 surfaceNormal)
        {
            surfacePosition = default;
            surfaceNormal = default;

            var arPlane = objectCollider.GetComponent<ARPlane>();
            if (arPlane == null)
                return false;

            if (m_RequireHorizontalUpSurface && arPlane.alignment != PlaneAlignment.HorizontalUp)
                return false;

            surfaceNormal = arPlane.normal;
            surfacePosition = arPlane.center;
            return true;
        }

        bool IsInteractionBlockingSpawn()
        {
            if (m_InteractionGroup != null && m_InteractionGroup.activeInteractor != null)
            {
                var hoverInteractor = (IXRHoverInteractor)m_InteractionGroup.activeInteractor;
                var selectInteractor = (IXRSelectInteractor)m_InteractionGroup.activeInteractor;
                var isHovering = (hoverInteractor != null) && hoverInteractor.hasHover;
                var isSelecting = (selectInteractor != null) && selectInteractor.hasSelection;
                return isHovering || isSelecting;
            }

            return false;
        }
    }
}
#endif
