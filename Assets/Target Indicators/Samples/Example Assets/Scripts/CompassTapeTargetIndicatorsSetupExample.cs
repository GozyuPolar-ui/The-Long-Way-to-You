using System.Collections.Generic;
using UnityEngine;

namespace TargetIndicators.Samples
{
    public class CompassTapeTargetIndicatorsSetupExample : MonoBehaviour
    {
        [SerializeField, Tooltip("The visual indicator manager used for adding compass tape visual indicators to.")]
        CompassTapeVisualIndicatorManager _visualIndicatorManager;

        [Header("Player Targets")]
        [SerializeField]
        Transform _player1Target;

        [SerializeField]
        CompassTapeVisualIndicator _player1UIPrefab;

        [Space]
        [SerializeField]
        Transform _player2Target;

        [SerializeField]
        CompassTapeVisualIndicator _player2UIPrefab;

        [Space]
        [SerializeField]
        Transform _player3Target;

        [SerializeField]
        CompassTapeVisualIndicator _player3UIPrefab;

        [Space]
        [SerializeField]
        Transform _player4Target;

        [SerializeField]
        CompassTapeVisualIndicator _player4UIPrefab;

        readonly Dictionary<Transform, TargetIndicatorId> _targetsToIndicatorIds = new();

        void OnEnable()
        {
            // First set the `AddIndicatorMode` to manual so we can use custom prefabs for each target's visual indicator.
            _visualIndicatorManager.AddIndicatorMode = AddIndicatorMode.Manual;

            var wasAdded = _visualIndicatorManager.TryAddVisualIndicator(_player1Target, _player1UIPrefab, out var id);
            if (wasAdded)
                _targetsToIndicatorIds.Add(_player1Target, id);

            wasAdded = _visualIndicatorManager.TryAddVisualIndicator(_player2Target, _player2UIPrefab, out id);
            if (wasAdded)
                _targetsToIndicatorIds.Add(_player2Target, id);

            wasAdded = _visualIndicatorManager.TryAddVisualIndicator(_player3Target, _player3UIPrefab, out id);
            if (wasAdded)
                _targetsToIndicatorIds.Add(_player3Target, id);

            wasAdded = _visualIndicatorManager.TryAddVisualIndicator(_player4Target, _player4UIPrefab, out id);
            if (wasAdded)
                _targetsToIndicatorIds.Add(_player4Target, id);
        }

        void OnDisable()
        {
            foreach (var (target, id) in _targetsToIndicatorIds)
            {
                _visualIndicatorManager.RemoveTargetIndicator(id);
            }

            _targetsToIndicatorIds.Clear();
        }
    }
}
