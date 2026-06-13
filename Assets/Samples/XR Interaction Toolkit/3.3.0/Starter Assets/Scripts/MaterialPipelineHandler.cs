using UnityEngine.Rendering;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
#if UNITY_EDITOR
    [InitializeOnLoad]
    static class RenderPipelineValidation
    {
        static RenderPipelineValidation()
        {
            foreach (var pipelineHandler in GetAllInstances())
                pipelineHandler.AutoRefreshPipelineShaders();
        }

        static List<MaterialPipelineHandler> GetAllInstances()
        {
            var instances = new List<MaterialPipelineHandler>();

            
            var guids = AssetDatabase.FindAssets("t:MaterialPipelineHandler");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<MaterialPipelineHandler>(path);
                if (asset != null)
                    instances.Add(asset);
            }

            return instances;
        }
    }
#endif

    [System.Serializable]
    public class ShaderContainer
    {
        public Material material;
        public bool useSRPShaderName = true;
        public string scriptableRenderPipelineShaderName = "Universal Render Pipeline/Lit";
        public Shader scriptableRenderPipelineShader;
        public bool useBuiltinShaderName = true;
        public string builtInPipelineShaderName = "Standard";
        public Shader builtInPipelineShader;
    }

    [CreateAssetMenu(fileName = "MaterialPipelineHandler", menuName = "XR/MaterialPipelineHandler", order = 0)]
    public class MaterialPipelineHandler : ScriptableObject
    {
        [SerializeField]
        [Tooltip("List of materials and their associated shaders.")]
        List<ShaderContainer> m_ShaderContainers;

        [SerializeField]
        [Tooltip("If true, the shaders will be refreshed automatically when the editor opens and when this scriptable object instance is enabled.")]
        bool m_AutoRefreshShaders = true;

#if UNITY_EDITOR
        void OnEnable()
        {
            if (Application.isPlaying)
                return;
            AutoRefreshPipelineShaders();
        }
#endif

        public void AutoRefreshPipelineShaders()
        {
            if (m_AutoRefreshShaders)
                SetPipelineShaders();
        }

        public void SetPipelineShaders()
        {
            if (m_ShaderContainers == null)
                return;

            bool isBuiltinRenderPipeline = GraphicsSettings.currentRenderPipeline == null;

            foreach (var info in m_ShaderContainers)
            {
                if (info.material == null)
                    continue;

                
                Shader birpShader = info.useBuiltinShaderName ? Shader.Find(info.builtInPipelineShaderName) : info.builtInPipelineShader;
                Shader srpShader = info.useSRPShaderName ? Shader.Find(info.scriptableRenderPipelineShaderName) : info.scriptableRenderPipelineShader;

                
                Shader currentShader = info.material.shader;

                
                if (isBuiltinRenderPipeline && birpShader != null && currentShader != birpShader)
                {
                    info.material.shader = birpShader;
                    MarkMaterialModified(info.material);
                }
                else if (!isBuiltinRenderPipeline && srpShader != null && currentShader != srpShader)
                {
                    info.material.shader = srpShader;
                    MarkMaterialModified(info.material);
                }
            }
        }

        static void MarkMaterialModified(Material material)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(material);
#endif
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ShaderContainer))]
    public class ShaderContainerDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty materialProp = property.FindPropertyRelative("material");
            SerializedProperty useSRPShaderNameProp = property.FindPropertyRelative("useSRPShaderName");
            SerializedProperty scriptableShaderNameProp = property.FindPropertyRelative("scriptableRenderPipelineShaderName");
            SerializedProperty scriptableShaderProp = property.FindPropertyRelative("scriptableRenderPipelineShader");
            SerializedProperty useShaderNameProp = property.FindPropertyRelative("useBuiltinShaderName");
            SerializedProperty builtInNameProp = property.FindPropertyRelative("builtInPipelineShaderName");
            SerializedProperty builtInShaderProp = property.FindPropertyRelative("builtInPipelineShader");

            
            position.height = singleLineHeight;
            EditorGUI.PropertyField(position, materialProp);
            position.y += singleLineHeight + verticalSpacing;

            
            EditorGUI.LabelField(position, "Scriptable Render Pipeline Shader", EditorStyles.boldLabel);
            position.y += EditorGUIUtility.singleLineHeight + verticalSpacing;

            EditorGUI.PropertyField(position, useSRPShaderNameProp);
            position.y += singleLineHeight + verticalSpacing;

            if (useSRPShaderNameProp.boolValue)
            {
                EditorGUI.PropertyField(position, scriptableShaderNameProp);
                position.y += singleLineHeight + verticalSpacing;
            }
            else
            {
                EditorGUI.PropertyField(position, scriptableShaderProp);
                position.y += singleLineHeight + verticalSpacing;
            }

            
            EditorGUI.LabelField(position, "Built-In Render Pipeline Shader", EditorStyles.boldLabel);
            position.y += singleLineHeight + verticalSpacing;

            EditorGUI.PropertyField(position, useShaderNameProp);
            position.y += singleLineHeight + verticalSpacing;

            if (useShaderNameProp.boolValue)
            {
                EditorGUI.PropertyField(position, builtInNameProp);
                position.y += singleLineHeight + verticalSpacing;
            }
            else
            {
                EditorGUI.PropertyField(position, builtInShaderProp);
                position.y += singleLineHeight + verticalSpacing;
            }

            
            position.y += verticalSpacing / 2; 
            position.height = 1;
            EditorGUI.DrawRect(new Rect(position.x, position.y, position.width, 1), Color.gray);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            const int baseFieldCount = 4; 
            int extraLineCount = property.FindPropertyRelative("useBuiltinShaderName").boolValue ? 0 : 1;
            extraLineCount += property.FindPropertyRelative("useSRPShaderName").boolValue ? 0 : 1;

            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;
            float headerHeight = EditorGUIUtility.singleLineHeight; 

            
            float fieldsHeight = baseFieldCount * singleLineHeight + (baseFieldCount - 1 + extraLineCount) * verticalSpacing;

            
            float headersHeight = 2 * (headerHeight + verticalSpacing);
            float separatorSpace = verticalSpacing / 2 + 1; 

            return fieldsHeight + headersHeight + separatorSpace + singleLineHeight * 1.5f;
        }
    }

    [CustomEditor(typeof(MaterialPipelineHandler)), CanEditMultipleObjects]
    public class MaterialPipelineHandlerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            
            if (GUILayout.Button("Refresh Shaders"))
            {
                foreach (var t in targets)
                {
                    var handler = (MaterialPipelineHandler)t;
                    handler.SetPipelineShaders();
                }
            }
        }
    }
#endif
}
