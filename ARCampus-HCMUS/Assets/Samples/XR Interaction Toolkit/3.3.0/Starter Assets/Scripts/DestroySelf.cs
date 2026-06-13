namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    public class DestroySelf : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The amount of time, in seconds, to wait after Start before destroying the GameObject.")]
        float m_Lifetime = 0.25f;

        public float lifetime
        {
            get => m_Lifetime;
            set => m_Lifetime = value;
        }

        void Start()
        {
            Destroy(gameObject, m_Lifetime);
        }
    }
}
