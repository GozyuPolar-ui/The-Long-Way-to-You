using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TargetIndicators
{
    [CustomEditor(typeof(TargetIndicatorBoundaryVisualizer))]
    public class TargetIndicatorBoundaryVisualizerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (target == null)
                return;

            serializedObject.Update();

            DrawDefaultInspector();

            var targetIndicatorBoundaryVisualizer = (TargetIndicatorBoundaryVisualizer)target;

            var type = typeof(TargetIndicatorBoundaryVisualizer);
            var fieldInfo = type.GetField("_targetIndicatorManager", BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo != null)
            {
                var targetIndicatorManager = (TargetIndicatorManager)fieldInfo.GetValue(targetIndicatorBoundaryVisualizer);

                if (targetIndicatorManager != null)
                {
                    switch (targetIndicatorManager.BoundaryType)
                    {
                        case BoundaryType.CompassTape:
                            EditorGUILayout.HelpBox(
                                "Boundary type `CompassTape` has no visualizer.",
                                MessageType.Warning);
                            break;
                        case BoundaryType.Unbounded:
                            EditorGUILayout.HelpBox(
                                "Boundary type `Unbounded` has no visualizer.",
                                MessageType.Warning);
                            break;
                        default:
                        {
                            if (IsBoundaryOutsideScreen(targetIndicatorManager))
                            {
                                EditorGUILayout.HelpBox(
                                    "One or more boundary padding values is negative and will draw the corresponding " +
                                    "side's boundary outside of the screen view.",
                                    MessageType.Warning);
                            }
                            break;
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            EditorGUI.DropShadowLabel(r, "No preview available for this component.");
        }

        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent("Preview");
        }

        static bool IsBoundaryOutsideScreen(TargetIndicatorManager targetIndicatorManager)
        {
            return
                targetIndicatorManager.LeftPadding < 0 ||
                targetIndicatorManager.RightPadding < 0 ||
                targetIndicatorManager.TopPadding < 0 ||
                targetIndicatorManager.BottomPadding < 0;
        }
    }
}
