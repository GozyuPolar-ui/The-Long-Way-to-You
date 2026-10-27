using UnityEngine;
using UnityEditor;

namespace TargetIndicators
{
    [CustomEditor(typeof(TargetIndicatorManager))]
    public class TargetIndicatorManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (target == null)
                return;

            serializedObject.Update();

            var scriptProperty = serializedObject.FindProperty("m_Script");
            GUI.enabled = false;
            EditorGUILayout.PropertyField(scriptProperty, true);
            GUI.enabled = true;

            var camera = serializedObject.FindProperty("_camera");
            var boundaryType = serializedObject.FindProperty("_boundaryType");
            var boundaryShape = serializedObject.FindProperty("_boundaryShape");
            var compassForwardReferenceOverride = serializedObject.FindProperty("_compassForwardReferenceOverride");

            var topPadding = serializedObject.FindProperty("_topPadding");
            var bottomPadding = serializedObject.FindProperty("_bottomPadding");
            var leftPadding = serializedObject.FindProperty("_leftPadding");
            var rightPadding = serializedObject.FindProperty("_rightPadding");

            var width = serializedObject.FindProperty("_width");
            var height = serializedObject.FindProperty("_height");

            var boundaryScale = serializedObject.FindProperty("_boundaryScale");

            var calculateLookAtDot = serializedObject.FindProperty("_calculateLookAtDot");

            EditorGUILayout.PropertyField(camera, new GUIContent("Camera"));
            EditorGUILayout.PropertyField(boundaryType, new GUIContent("Boundary Type"));

            if (boundaryShape != null)
            {
                if (boundaryShape.enumValueIndex < 0)
                    boundaryShape.enumValueIndex = (int)BoundaryShape.Rectangle;

                if (boundaryType.enumValueIndex is (int)BoundaryType.Padded or (int)BoundaryType.Absolute)
                {
                    EditorGUILayout.PropertyField(boundaryShape, new GUIContent("Boundary Shape"));
                }
            }

            if (boundaryType != null)
            {
                switch (boundaryType.enumValueIndex)
                {
                    case (int)BoundaryType.Padded when boundaryShape != null &&
                        boundaryShape.enumValueIndex is (int)BoundaryShape.Rectangle or (int)BoundaryShape.Ellipse:
                        EditorGUILayout.PropertyField(topPadding, new GUIContent("Top Padding"));
                        EditorGUILayout.PropertyField(bottomPadding, new GUIContent("Bottom Padding"));
                        EditorGUILayout.PropertyField(leftPadding, new GUIContent("Left Padding"));
                        EditorGUILayout.PropertyField(rightPadding, new GUIContent("Right Padding"));
                        break;
                    case (int)BoundaryType.Absolute:
                        EditorGUILayout.PropertyField(width, new GUIContent("Width"));
                        EditorGUILayout.PropertyField(height, new GUIContent("Height"));
                        break;
                    case (int)BoundaryType.CompassTape:
                        EditorGUILayout.PropertyField(compassForwardReferenceOverride, new GUIContent("Compass Forward Override"));
                        break;
                }

                if (boundaryType.enumValueIndex is (int)BoundaryType.Padded or (int)BoundaryType.Absolute)
                    EditorGUILayout.PropertyField(boundaryScale, new GUIContent("Boundary Scale", boundaryScale.tooltip));
            }

            EditorGUILayout.PropertyField(calculateLookAtDot);
            EditorGUILayout.Space();

            var targetIndicatorManager = (TargetIndicatorManager)target;
            var debugLinesComponent = targetIndicatorManager.gameObject.GetComponent<TargetIndicatorBoundaryVisualizer>();
            var hadDebugLines = debugLinesComponent != null;

            if (!hadDebugLines)
            {
                if (GUILayout.Button("Add Boundary Visualizer", GUILayout.Height(30)))
                {
                    AddBoundaryVisualizer(targetIndicatorManager);
                }
            }
            else
            {
                if (GUILayout.Button("Remove Boundary Visualizer", GUILayout.Height(30)))
                {
                    RemoveBoundaryVisualizer(targetIndicatorManager);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        internal static void AddBoundaryVisualizer(TargetIndicatorManager targetIndicatorManager)
        {
            Undo.AddComponent<TargetIndicatorBoundaryVisualizer>(targetIndicatorManager.gameObject);
        }

        internal static void RemoveBoundaryVisualizer(TargetIndicatorManager targetIndicatorManager)
        {
            if (!targetIndicatorManager.TryGetComponent<TargetIndicatorBoundaryVisualizer>(out var boundaryVisualizer))
                return;

            // The visualizer draws with the LineRenderer on its own GameObject. Look it up directly: a
            // SerializedObject left pointing at the destroyed visualizer can crash the Editor when it's
            // finalized.
            if (targetIndicatorManager.TryGetComponent<LineRenderer>(out var lineRenderer))
                Undo.DestroyObjectImmediate(lineRenderer);

            Undo.DestroyObjectImmediate(boundaryVisualizer);
        }
    }
}
