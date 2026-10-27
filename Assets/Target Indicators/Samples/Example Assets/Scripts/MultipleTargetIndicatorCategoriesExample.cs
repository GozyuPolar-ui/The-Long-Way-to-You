using System.Collections.Generic;
using UnityEngine;

namespace TargetIndicators.Samples
{
    public class MultipleTargetIndicatorCategoriesExample : MonoBehaviour
    {
        [Header("UI Target Managers")]
        [SerializeField]
        VisualIndicatorManager _mainIndicatorManager;

        [SerializeField]
        VisualIndicatorManager _offsetIndicatorManager;

        [Header("Capture Points")]
        [SerializeField]
        Transform _capturePointA;

        [SerializeField]
        VisualIndicator _capturePointAIndicator;

        [Space]
        [SerializeField]
        Transform _capturePointB;

        [SerializeField]
        VisualIndicator _capturePointBIndicator;

        [Space]
        [SerializeField]
        Transform _capturePointC;

        [SerializeField]
        VisualIndicator _capturePointCIndicator;

        [Header("Points of Interest")]
        [SerializeField]
        Transform _primaryPointOfInterestTarget;

        [SerializeField]
        VisualIndicator _primaryPointOfInterestIndicatorPrefab;

        [SerializeField]
        List<Transform> _secondaryPointsOfInterestTargets = new();

        [Header("Enemies")]
        [SerializeField]
        VisualIndicator _enemyTargetIndicator;

        [SerializeField]
        List<Transform> _enemyTargets;

        void Start()
        {
            // To prevent the UI indicator manager from auto adding visual indicators when the TargetIndicatorManager adds
            // a target, change AddIndicatorMode to manual.
            _mainIndicatorManager.AddIndicatorMode = AddIndicatorMode.Manual;

            // Add capture point targets with their visual indicators.
            _mainIndicatorManager.TryAddVisualIndicator(_capturePointA, _capturePointAIndicator, out _);
            _mainIndicatorManager.TryAddVisualIndicator(_capturePointB, _capturePointBIndicator, out _);
            _mainIndicatorManager.TryAddVisualIndicator(_capturePointC, _capturePointCIndicator, out _);

            // Add point of interest target with their visual indicators.
            _mainIndicatorManager.TryAddVisualIndicator(_primaryPointOfInterestTarget, _primaryPointOfInterestIndicatorPrefab, out _);

            // Add secondary points of interest, relying on the default visual indicator prefab.
            foreach (var target in _secondaryPointsOfInterestTargets)
            {
                _mainIndicatorManager.TryAddVisualIndicator(target, out _);
            }

            // Add visual indicators that have offsets to the target indicator manager with the offset boundary.
            _offsetIndicatorManager.AddIndicatorMode = AddIndicatorMode.Manual;
            _offsetIndicatorManager.DefaultVisualIndicatorPrefab = _enemyTargetIndicator;
            foreach (var target in _enemyTargets)
            {
                _offsetIndicatorManager.TryAddVisualIndicator(target, out _);
            }
        }
    }
}
