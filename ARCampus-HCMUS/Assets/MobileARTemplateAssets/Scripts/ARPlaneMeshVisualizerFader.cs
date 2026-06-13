using UnityEngine.XR.Interaction.Toolkit.Utilities.Tweenables.Primitives;

namespace UnityEngine.XR.Templates.AR
{
    [RequireComponent(typeof(MeshRenderer))]
    public class ARPlaneMeshVisualizerFader : MonoBehaviour
    {
        [Tooltip("Renderer component on the ARPlaneMeshVisualizer prefab. Used to fetch the material to fade in/out.")]
        [SerializeField]
        Renderer m_PlaneRenderer;

        public Renderer planeRenderer
        {
            get => m_PlaneRenderer;
            set => m_PlaneRenderer = value;
        }

        [Tooltip("Fade in/out speed multiplier applied during the alpha tweening. The lower the value, the slower it works. A value of 1 is full speed (1 second).")]
        [Range(0.1f, 1.0f)]
        [SerializeField]
        float m_FadeSpeed = 1f;

        public float fadeSpeed
        {
            get => m_FadeSpeed;
            set => m_FadeSpeed = value;
        }

        int m_ShaderAlphaPropertyID;
        float m_SurfaceVisualAlpha = 1f;
        float m_TweenProgress;
        Material m_PlaneMaterial;

#pragma warning disable CS0618 
        readonly FloatTweenableVariable m_AlphaTweenableVariable = new FloatTweenableVariable();
#pragma warning restore CS0618

        void Awake()
        {
            m_ShaderAlphaPropertyID = Shader.PropertyToID("_PlaneAlpha");
            m_PlaneMaterial = m_PlaneRenderer.material;
            visualizeSurfaces = true;
        }

        void OnDestroy()
        {
            m_AlphaTweenableVariable.Dispose();
        }

        void Update()
        {
            m_AlphaTweenableVariable.HandleTween(m_TweenProgress);
            m_TweenProgress += Time.unscaledDeltaTime * m_FadeSpeed;
            m_SurfaceVisualAlpha = m_AlphaTweenableVariable.Value;
            m_PlaneMaterial.SetFloat(m_ShaderAlphaPropertyID, m_SurfaceVisualAlpha);
        }

        public bool visualizeSurfaces
        {
            set
            {
                m_TweenProgress = 0f;
                m_AlphaTweenableVariable.target = value ? 1f : 0f;
                m_AlphaTweenableVariable.HandleTween(0f);
            }
        }

        public void SetVisualsImmediate(float alpha)
        {
            m_AlphaTweenableVariable.target = alpha;
            m_AlphaTweenableVariable.HandleTween(1f);
        }
    }
}
