using UnityEngine.UI;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleColorToggler : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Normal color for the toggle in the on state.")]
        Color m_OnColor = new Color(32 / 255f, 150 / 255f, 243 / 255f);

        public Color onColor
        {
            get => m_OnColor;
            set => m_OnColor = value;
        }

        [SerializeField]
        [Tooltip("Normal color for the toggle in the off state.")]
        Color m_OffColor = new Color(46 / 255f, 46 / 255f, 46 / 255f);

        public Color offColor
        {
            get => m_OffColor;
            set => m_OffColor = value;
        }

        Toggle m_TargetToggle;

        void Awake()
        {
            m_TargetToggle = GetComponent<Toggle>();
        }

        void OnEnable()
        {
            m_TargetToggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        void OnDisable()
        {
            m_TargetToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }

        void OnToggleValueChanged(bool isOn)
        {
            var toggleColors = m_TargetToggle.colors;
            toggleColors.normalColor = isOn ? m_OnColor : m_OffColor;
            m_TargetToggle.colors = toggleColors;
        }
    }
}
