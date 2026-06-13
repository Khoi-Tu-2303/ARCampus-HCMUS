using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace UnityEngine.XR.Templates.AR
{
    public class ARTemplateMenuManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Button that opens the create menu.")]
        Button m_CreateButton;

        public Button createButton
        {
            get => m_CreateButton;
            set => m_CreateButton = value;
        }

        [SerializeField]
        [Tooltip("Button that deletes a selected object.")]
        Button m_DeleteButton;

        public Button deleteButton
        {
            get => m_DeleteButton;
            set => m_DeleteButton = value;
        }

        [SerializeField]
        [Tooltip("The menu with all the creatable objects.")]
        GameObject m_ObjectMenu;

        public GameObject objectMenu
        {
            get => m_ObjectMenu;
            set => m_ObjectMenu = value;
        }

        [SerializeField]
        [Tooltip("The modal with debug options.")]
        GameObject m_ModalMenu;

        public GameObject modalMenu
        {
            get => m_ModalMenu;
            set => m_ModalMenu = value;
        }

        [SerializeField]
        [Tooltip("The animator for the object creation menu.")]
        Animator m_ObjectMenuAnimator;

        public Animator objectMenuAnimator
        {
            get => m_ObjectMenuAnimator;
            set => m_ObjectMenuAnimator = value;
        }

        [SerializeField]
        [Tooltip("The object spawner component in charge of spawning new objects.")]
        ObjectSpawner m_ObjectSpawner;

        public ObjectSpawner objectSpawner
        {
            get => m_ObjectSpawner;
            set => m_ObjectSpawner = value;
        }

        [SerializeField]
        [Tooltip("Button that closes the object creation menu.")]
        Button m_CancelButton;

        public Button cancelButton
        {
            get => m_CancelButton;
            set => m_CancelButton = value;
        }

        [SerializeField]
        [Tooltip("The interaction group for the AR demo scene.")]
        XRInteractionGroup m_InteractionGroup;

        public XRInteractionGroup interactionGroup
        {
            get => m_InteractionGroup;
            set => m_InteractionGroup = value;
        }

        [SerializeField]
        [Tooltip("The slider for activating plane debug visuals.")]
        DebugSlider m_DebugPlaneSlider;

        public DebugSlider debugPlaneSlider
        {
            get => m_DebugPlaneSlider;
            set => m_DebugPlaneSlider = value;
        }

        [SerializeField]
        [Tooltip("The plane manager in the AR demo scene.")]
        ARPlaneManager m_PlaneManager;

        public ARPlaneManager planeManager
        {
            get => m_PlaneManager;
            set => m_PlaneManager = value;
        }

        [SerializeField]
        [Tooltip("Determines whether or not to fade the AR Planes when visualization is toggled.")]
        bool m_UseARPlaneFading = true;

        public bool useARPlaneFading
        {
            get => m_UseARPlaneFading;
            set => m_UseARPlaneFading = value;
        }

        [SerializeField]
        [Tooltip("The AR debug menu.")]
        ARDebugMenu m_ARDebugMenu;

        public ARDebugMenu arDebugMenu
        {
            get => m_ARDebugMenu;
            set => m_ARDebugMenu = value;
        }

        [SerializeField]
        [Tooltip("The slider for activating the debug menu.")]
        DebugSlider m_DebugMenuSlider;

        public DebugSlider debugMenuSlider
        {
            get => m_DebugMenuSlider;
            set => m_DebugMenuSlider = value;
        }

        [SerializeField]
        XRInputValueReader<Vector2> m_TapStartPositionInput = new XRInputValueReader<Vector2>("Tap Start Position");

        public XRInputValueReader<Vector2> tapStartPositionInput
        {
            get => m_TapStartPositionInput;
            set => XRInputReaderUtility.SetInputProperty(ref m_TapStartPositionInput, value, this);
        }

        [SerializeField]
        XRInputValueReader<Vector2> m_DragCurrentPositionInput = new XRInputValueReader<Vector2>("Drag Current Position");

        public XRInputValueReader<Vector2> dragCurrentPositionInput
        {
            get => m_DragCurrentPositionInput;
            set => XRInputReaderUtility.SetInputProperty(ref m_DragCurrentPositionInput, value, this);
        }

        bool m_IsPointerOverUI;
        bool m_ShowObjectMenu;
        bool m_ShowOptionsModal;
        bool m_VisualizePlanes = true;
        bool m_ShowDebugMenu;
        bool m_InitializingDebugMenu;
        float m_DebugMenuPlanesButtonValue = 0f;
        Vector2 m_ObjectButtonOffset = Vector2.zero;
        Vector2 m_ObjectMenuOffset = Vector2.zero;
        readonly List<ARPlane> m_ARPlanes = new List<ARPlane>();
        readonly Dictionary<ARPlane, ARPlaneMeshVisualizer> m_ARPlaneMeshVisualizers = new Dictionary<ARPlane, ARPlaneMeshVisualizer>();
        readonly Dictionary<ARPlane, ARPlaneMeshVisualizerFader> m_ARPlaneMeshVisualizerFaders = new Dictionary<ARPlane, ARPlaneMeshVisualizerFader>();

        void OnEnable()
        {
            m_CreateButton.onClick.AddListener(ShowMenu);
            m_CancelButton.onClick.AddListener(HideMenu);
            m_DeleteButton.onClick.AddListener(DeleteFocusedObject);
            m_PlaneManager.trackablesChanged.AddListener(OnPlaneChanged);
        }

        void OnDisable()
        {
            m_ShowObjectMenu = false;
            m_CreateButton.onClick.RemoveListener(ShowMenu);
            m_CancelButton.onClick.RemoveListener(HideMenu);
            m_DeleteButton.onClick.RemoveListener(DeleteFocusedObject);
            m_PlaneManager.trackablesChanged.RemoveListener(OnPlaneChanged);
        }

        void Start()
        {
            
            
            if (m_ARDebugMenu != null)
            {
                m_ARDebugMenu.gameObject.SetActive(true);
                m_InitializingDebugMenu = true;

                InitializeDebugMenuOffsets();
            }

            HideMenu();

            m_DebugMenuSlider.value = m_ShowDebugMenu ? 1 : 0;
            m_DebugPlaneSlider.value = m_VisualizePlanes ? 1 : 0;
        }

        void Update()
        {
            if (m_InitializingDebugMenu)
            {
                m_ARDebugMenu.gameObject.SetActive(false);
                m_InitializingDebugMenu = false;
            }

            if (m_ShowObjectMenu || m_ShowOptionsModal)
            {
                if (!m_IsPointerOverUI && (m_TapStartPositionInput.TryReadValue(out _) || m_DragCurrentPositionInput.TryReadValue(out _)))
                {
                    if (m_ShowObjectMenu)
                        HideMenu();

                    if (m_ShowOptionsModal)
                        m_ModalMenu.SetActive(false);
                }

                if (m_ShowObjectMenu)
                {
                    m_DeleteButton.gameObject.SetActive(false);
                }
                else
                {
                    m_DeleteButton.gameObject.SetActive(m_InteractionGroup?.focusInteractable != null);
                }

                m_IsPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1);
            }
            else
            {
                m_IsPointerOverUI = false;
                m_CreateButton.gameObject.SetActive(true);
                m_DeleteButton.gameObject.SetActive(m_InteractionGroup?.focusInteractable != null);
            }

            if (!m_IsPointerOverUI && m_ShowOptionsModal)
            {
                m_IsPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1);
            }
        }

        public void SetObjectToSpawn(int objectIndex)
        {
            if (m_ObjectSpawner == null)
            {
                Debug.LogWarning("Object Spawner not configured correctly: no ObjectSpawner set.");
            }
            else
            {
                if (m_ObjectSpawner.objectPrefabs.Count > objectIndex)
                {
                    m_ObjectSpawner.spawnOptionIndex = objectIndex;
                }
                else
                {
                    Debug.LogWarning("Object Spawner not configured correctly: object index larger than number of Object Prefabs.");
                }
            }

            HideMenu();
        }

        void ShowMenu()
        {
            m_ShowObjectMenu = true;
            m_ObjectMenu.SetActive(true);
            if (!m_ObjectMenuAnimator.GetBool("Show"))
            {
                m_ObjectMenuAnimator.SetBool("Show", true);
            }
            AdjustARDebugMenuPosition();
        }

        public void ShowHideModal()
        {
            if (m_ModalMenu.activeSelf)
            {
                m_ShowOptionsModal = false;
                m_ModalMenu.SetActive(false);
            }
            else
            {
                m_ShowOptionsModal = true;
                m_ModalMenu.SetActive(true);
            }
        }

        public void ShowHideDebugPlane()
        {
            m_VisualizePlanes = !m_VisualizePlanes;
            m_DebugPlaneSlider.value = m_VisualizePlanes ? 1 : 0;
            ChangePlaneVisibility(m_VisualizePlanes);
        }

        public void ShowHideDebugMenu()
        {
            m_ShowDebugMenu = !m_ShowDebugMenu;
            m_DebugMenuSlider.value = m_ShowDebugMenu ? 1 : 0;

            
            
            
            
            
            
            
            if (m_ShowDebugMenu)
            {
                m_ARDebugMenu.gameObject.SetActive(true);
                AdjustARDebugMenuPosition();
                if (m_ARDebugMenu.showPlanesButton.value != m_DebugMenuPlanesButtonValue)
                    m_ARDebugMenu.showPlanesButton.value = m_DebugMenuPlanesButtonValue;
            }
            else
            {
                m_DebugMenuPlanesButtonValue = m_ARDebugMenu.showPlanesButton.value;
                if (m_DebugMenuPlanesButtonValue == 1f)
                    m_ARDebugMenu.showPlanesButton.value = 0f;

                m_ARDebugMenu.gameObject.SetActive(false);
            }
        }

        public void ClearAllObjects()
        {
            foreach (Transform child in m_ObjectSpawner.transform)
            {
                Destroy(child.gameObject);
            }
        }

        public void HideMenu()
        {
            m_ObjectMenuAnimator.SetBool("Show", false);
            m_ShowObjectMenu = false;
            AdjustARDebugMenuPosition();
        }

        void ChangePlaneVisibility(bool setVisible)
        {
            foreach (var plane in m_ARPlanes)
            {
                if (m_ARPlaneMeshVisualizers.TryGetValue(plane, out var visualizer))
                {
                    visualizer.enabled = m_UseARPlaneFading ? true : setVisible;
                }

                if (m_ARPlaneMeshVisualizerFaders.TryGetValue(plane, out var fader))
                {
                    if (m_UseARPlaneFading)
                        fader.visualizeSurfaces = setVisible;
                    else
                        fader.SetVisualsImmediate(1f);
                }
            }
        }

        void DeleteFocusedObject()
        {
            var currentFocusedObject = m_InteractionGroup.focusInteractable;
            if (currentFocusedObject != null)
            {
                Destroy(currentFocusedObject.transform.gameObject);
            }
        }

        void InitializeDebugMenuOffsets()
        {
            if (m_CreateButton.TryGetComponent<RectTransform>(out var buttonRect))
                m_ObjectButtonOffset = new Vector2(0f, buttonRect.anchoredPosition.y + buttonRect.rect.height + 10f);
            else
                m_ObjectButtonOffset = new Vector2(0f, 200f);

            if (m_ObjectMenu.TryGetComponent<RectTransform>(out var menuRect))
                m_ObjectMenuOffset = new Vector2(0f, menuRect.anchoredPosition.y + menuRect.rect.height + 10f);
            else
                m_ObjectMenuOffset = new Vector2(0f, 345f);
        }

        void AdjustARDebugMenuPosition()
        {
            if (m_ARDebugMenu == null)
                return;

            float screenWidthInInches = Screen.width / Screen.dpi;

            if (screenWidthInInches < 5)
            {
                Vector2 menuOffset = m_ShowObjectMenu ? m_ObjectMenuOffset : m_ObjectButtonOffset;

                if (m_ARDebugMenu.toolbar.TryGetComponent<RectTransform>(out var rect))
                {
                    rect.anchorMin = new Vector2(0.5f, 0);
                    rect.anchorMax = new Vector2(0.5f, 0);
                    rect.eulerAngles = new Vector3(rect.eulerAngles.x, rect.eulerAngles.y, 90);
                    rect.anchoredPosition = new Vector2(0, 20) + menuOffset;
                }

                if (m_ARDebugMenu.displayInfoMenuButton.TryGetComponent<RectTransform>(out var infoMenuButtonRect))
                    infoMenuButtonRect.localEulerAngles = new Vector3(infoMenuButtonRect.localEulerAngles.x, infoMenuButtonRect.localEulerAngles.y, -90);

                if (m_ARDebugMenu.displayConfigurationsMenuButton.TryGetComponent<RectTransform>(out var configurationsMenuButtonRect))
                    configurationsMenuButtonRect.localEulerAngles = new Vector3(configurationsMenuButtonRect.localEulerAngles.x, configurationsMenuButtonRect.localEulerAngles.y, -90);

                if (m_ARDebugMenu.displayCameraConfigurationsMenuButton.TryGetComponent<RectTransform>(out var cameraConfigurationsMenuButtonRect))
                    cameraConfigurationsMenuButtonRect.localEulerAngles = new Vector3(cameraConfigurationsMenuButtonRect.localEulerAngles.x, cameraConfigurationsMenuButtonRect.localEulerAngles.y, -90);

                if (m_ARDebugMenu.displayDebugOptionsMenuButton.TryGetComponent<RectTransform>(out var debugOptionsMenuButtonRect))
                    debugOptionsMenuButtonRect.localEulerAngles = new Vector3(debugOptionsMenuButtonRect.localEulerAngles.x, debugOptionsMenuButtonRect.localEulerAngles.y, -90);

                if (m_ARDebugMenu.infoMenu.TryGetComponent<RectTransform>(out var infoMenuRect))
                {
                    infoMenuRect.anchorMin = new Vector2(0.5f, 0);
                    infoMenuRect.anchorMax = new Vector2(0.5f, 0);
                    infoMenuRect.pivot = new Vector2(0.5f, 0);
                    infoMenuRect.anchoredPosition = new Vector2(0, 150) + menuOffset;
                }

                if (m_ARDebugMenu.configurationMenu.TryGetComponent<RectTransform>(out var configurationsMenuRect))
                {
                    configurationsMenuRect.anchorMin = new Vector2(0.5f, 0);
                    configurationsMenuRect.anchorMax = new Vector2(0.5f, 0);
                    configurationsMenuRect.pivot = new Vector2(0.5f, 0);
                    configurationsMenuRect.anchoredPosition = new Vector2(0, 150) + menuOffset;
                }

                if (m_ARDebugMenu.cameraConfigurationMenu.TryGetComponent<RectTransform>(out var cameraConfigurationsMenuRect))
                {
                    cameraConfigurationsMenuRect.anchorMin = new Vector2(0.5f, 0);
                    cameraConfigurationsMenuRect.anchorMax = new Vector2(0.5f, 0);
                    cameraConfigurationsMenuRect.pivot = new Vector2(0.5f, 0);
                    cameraConfigurationsMenuRect.anchoredPosition = new Vector2(0, 150) + menuOffset;
                }

                if (m_ARDebugMenu.debugOptionsMenu.TryGetComponent<RectTransform>(out var debugOptionsMenuRect))
                {
                    debugOptionsMenuRect.anchorMin = new Vector2(0.5f, 0);
                    debugOptionsMenuRect.anchorMax = new Vector2(0.5f, 0);
                    debugOptionsMenuRect.pivot = new Vector2(0.5f, 0);
                    debugOptionsMenuRect.anchoredPosition = new Vector2(0, 150) + menuOffset;
                }
            }
        }

        void OnPlaneChanged(ARTrackablesChangedEventArgs<ARPlane> eventArgs)
        {
            if (eventArgs.added.Count > 0)
            {
                foreach (var plane in eventArgs.added)
                {
                    m_ARPlanes.Add(plane);
                    if (plane.TryGetComponent<ARPlaneMeshVisualizer>(out var vizualizer))
                    {
                        m_ARPlaneMeshVisualizers.Add(plane, vizualizer);
                        if (!m_UseARPlaneFading)
                        {
                            vizualizer.enabled = m_VisualizePlanes;
                        }
                    }

                    if (!plane.TryGetComponent<ARPlaneMeshVisualizerFader>(out var visualizer))
                    {
                        visualizer = plane.gameObject.AddComponent<ARPlaneMeshVisualizerFader>();
                    }
                    m_ARPlaneMeshVisualizerFaders.Add(plane, visualizer);
                    visualizer.visualizeSurfaces = m_VisualizePlanes;
                }
            }

            if (eventArgs.removed.Count > 0)
            {
                foreach (var plane in eventArgs.removed)
                {
                    var planeGameObject = plane.Value;
                    if (planeGameObject == null)
                        continue;

                    if (m_ARPlanes.Contains(planeGameObject))
                        m_ARPlanes.Remove(planeGameObject);

                    if (m_ARPlaneMeshVisualizers.ContainsKey(planeGameObject))
                        m_ARPlaneMeshVisualizers.Remove(planeGameObject);

                    if (m_ARPlaneMeshVisualizerFaders.ContainsKey(planeGameObject))
                        m_ARPlaneMeshVisualizerFaders.Remove(planeGameObject);
                }
            }

            
            if (m_PlaneManager.trackables.count != m_ARPlanes.Count)
            {
                m_ARPlanes.Clear();
                m_ARPlaneMeshVisualizers.Clear();
                m_ARPlaneMeshVisualizerFaders.Clear();

                foreach (var plane in m_PlaneManager.trackables)
                {
                    m_ARPlanes.Add(plane);
                    if (plane.TryGetComponent<ARPlaneMeshVisualizer>(out var vizualizer))
                    {
                        m_ARPlaneMeshVisualizers.Add(plane, vizualizer);
                        if (!m_UseARPlaneFading)
                        {
                            vizualizer.enabled = m_VisualizePlanes;
                        }
                    }

                    if (!plane.TryGetComponent<ARPlaneMeshVisualizerFader>(out var fader))
                    {
                        fader = plane.gameObject.AddComponent<ARPlaneMeshVisualizerFader>();
                    }
                    m_ARPlaneMeshVisualizerFaders.Add(plane, fader);
                    fader.visualizeSurfaces = m_VisualizePlanes;
                }
            }
        }
    }
}
