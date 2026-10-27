# Custom UI layers for Target Indicators

Read this when the user wants a pooled uGUI indicator layer (no `Instantiate`/`Destroy` at runtime) or markers rendered with UI Toolkit. Both implementations consume only the core `TargetIndicators` API and don't depend on the samples.

Shared rules (verified against `TargetIndicatorManager` and the sample `VisualIndicator`):

- `ScreenPose.position` is in screen pixels with the origin at the bottom-left (Padded, Absolute, Unbounded).
- `ScreenPose.rotation` is a Z rotation from screen-right (1,0) toward the target. Arrow art should point right at zero rotation.
- For `Unbounded`, targets behind the camera have `position.z < 0` and `position.x = float.MaxValue`. Hide them.
- Map elements by `TargetIndicatorId`. Don't store the event spans.
- Targets added before your layer subscribed only arrive through `TargetIndicatorsUpdated`, so acquire elements lazily in the update handler too.
- Set `TargetIndicatorManager.BoundaryScale` to your UI's scale factor so padding and size are in your UI's units. The manager doesn't read your UI, so your layer must set it and keep it updated.
- For `CompassTape`, `position.x` is a normalized heading. See the Compass bar workflow in `SKILL.md` instead of these placement rules.

## Pooled uGUI layer

Setup: a Screen Space - Overlay `Canvas`; a child `RectTransform` content stretched to anchors (0,0)–(1,1) with zero offsets; an indicator prefab whose root is a `RectTransform` and which optionally has a direct child named `Rotation`: a `RectTransform` at (0,0) holding the arrow art, which points right and is offset along +X (the same layout as the stock prefab's `Rotation Content` > `Arrow`).

```csharp
using System;
using System.Collections.Generic;
using TargetIndicators;
using UnityEngine;

public class PooledIndicatorLayer : MonoBehaviour
{
    sealed class PooledIndicator
    {
        public RectTransform Root;
        public RectTransform Rotation;
    }

    [SerializeField]
    TargetIndicatorManager _targetIndicatorManager;

    [SerializeField]
    Canvas _canvas;

    [SerializeField]
    RectTransform _content;

    [SerializeField]
    RectTransform _indicatorPrefab;

    [SerializeField, Min(1)]
    int _poolSize = 32;

    readonly Stack<PooledIndicator> _pool = new();
    readonly Dictionary<TargetIndicatorId, PooledIndicator> _activeIndicators = new();

    void Awake()
    {
        for (var i = 0; i < _poolSize; i++)
        {
            var root = Instantiate(_indicatorPrefab, _content);
            root.anchorMin = root.anchorMax = Vector2.zero;
            root.pivot = new Vector2(0.5f, 0.5f);
            root.gameObject.SetActive(false);
            _pool.Push(new PooledIndicator { Root = root, Rotation = root.Find("Rotation") as RectTransform });
        }
    }

    void OnEnable()
    {
        _targetIndicatorManager.TargetIndicatorsAdded += OnTargetIndicatorsAddedOrUpdated;
        _targetIndicatorManager.TargetIndicatorsUpdated += OnTargetIndicatorsAddedOrUpdated;
        _targetIndicatorManager.TargetIndicatorsRemoved += OnTargetIndicatorsRemoved;
    }

    void OnDisable()
    {
        _targetIndicatorManager.TargetIndicatorsAdded -= OnTargetIndicatorsAddedOrUpdated;
        _targetIndicatorManager.TargetIndicatorsUpdated -= OnTargetIndicatorsAddedOrUpdated;
        _targetIndicatorManager.TargetIndicatorsRemoved -= OnTargetIndicatorsRemoved;
    }

    void OnTargetIndicatorsAddedOrUpdated(ReadOnlySpan<TargetIndicator> targetIndicators)
    {
        var scaleFactor = _canvas.scaleFactor;

        // Takes effect from the next update.
        if (!Mathf.Approximately(_targetIndicatorManager.BoundaryScale, scaleFactor))
            _targetIndicatorManager.BoundaryScale = scaleFactor;

        foreach (var targetIndicator in targetIndicators)
        {
            if (!_activeIndicators.TryGetValue(targetIndicator.Id, out var pooledIndicator))
            {
                // Pool exhausted: raise _poolSize.
                if (!_pool.TryPop(out pooledIndicator))
                    continue;

                _activeIndicators.Add(targetIndicator.Id, pooledIndicator);
            }

            var screenPosition = targetIndicator.ScreenPose.position;
            var isVisible = !(screenPosition.z < 0f && _targetIndicatorManager.BoundaryType == BoundaryType.Unbounded);
            SetActive(pooledIndicator.Root.gameObject, isVisible);
            if (!isVisible)
                continue;

            pooledIndicator.Root.anchoredPosition = new Vector2(screenPosition.x, screenPosition.y) / scaleFactor;

            if (pooledIndicator.Rotation != null)
            {
                SetActive(pooledIndicator.Rotation.gameObject, targetIndicator.IsOutsideBoundary);
                pooledIndicator.Rotation.rotation = targetIndicator.ScreenPose.rotation;
            }
        }
    }

    void OnTargetIndicatorsRemoved(ReadOnlySpan<TargetIndicatorId> removedIds)
    {
        foreach (var removedId in removedIds)
        {
            if (!_activeIndicators.Remove(removedId, out var pooledIndicator))
                continue;

            pooledIndicator.Root.gameObject.SetActive(false);
            _pool.Push(pooledIndicator);
        }
    }

    static void SetActive(GameObject target, bool isActive)
    {
        if (target.activeSelf != isActive)
            target.SetActive(isActive);
    }
}
```

Notes:
- The `Rotation` node is rotated, not the arrow itself. Because the arrow sits offset along +X inside it, the arrow orbits the icon and points at the target. Rotating the arrow directly would only spin it in place. This mirrors the stock prefab, where `Arrow` sits at x = 35 inside `Rotation Content`.
- After `Awake`, this layer performs no `Instantiate`/`Destroy` and no per-frame allocations.

## UI Toolkit layer

Setup: a GameObject with a `UIDocument` (Panel Settings assigned) and this component. Elements are created in code. Replace the background color with a `style.backgroundImage` for real art.

```csharp
using System;
using System.Collections.Generic;
using TargetIndicators;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UIToolkitIndicatorLayer : MonoBehaviour
{
    [SerializeField]
    TargetIndicatorManager _targetIndicatorManager;

    [SerializeField, Min(1)]
    int _poolSize = 32;

    [SerializeField]
    float _size = 32f;

    readonly Stack<VisualElement> _pool = new();
    readonly Dictionary<TargetIndicatorId, VisualElement> _activeElements = new();
    VisualElement _root;

    void Start()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        for (var i = 0; i < _poolSize; i++)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.style.position = Position.Absolute;
            element.style.width = _size;
            element.style.height = _size;

            // Center the element on its position.
            element.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            element.style.backgroundColor = Color.yellow;
            element.style.display = DisplayStyle.None;
            _root.Add(element);
            _pool.Push(element);
        }

        _targetIndicatorManager.TargetIndicatorsAdded += OnTargetIndicatorsAddedOrUpdated;
        _targetIndicatorManager.TargetIndicatorsUpdated += OnTargetIndicatorsAddedOrUpdated;
        _targetIndicatorManager.TargetIndicatorsRemoved += OnTargetIndicatorsRemoved;
    }

    void OnDestroy()
    {
        if (_targetIndicatorManager == null)
            return;

        _targetIndicatorManager.TargetIndicatorsAdded -= OnTargetIndicatorsAddedOrUpdated;
        _targetIndicatorManager.TargetIndicatorsUpdated -= OnTargetIndicatorsAddedOrUpdated;
        _targetIndicatorManager.TargetIndicatorsRemoved -= OnTargetIndicatorsRemoved;
    }

    void OnTargetIndicatorsAddedOrUpdated(ReadOnlySpan<TargetIndicator> targetIndicators)
    {
        if (_root?.panel == null)
            return;

        foreach (var targetIndicator in targetIndicators)
        {
            if (!_activeElements.TryGetValue(targetIndicator.Id, out var element))
            {
                if (!_pool.TryPop(out element))
                    continue;

                _activeElements.Add(targetIndicator.Id, element);
            }

            var screenPosition = targetIndicator.ScreenPose.position;
            if (screenPosition.z < 0f && _targetIndicatorManager.BoundaryType == BoundaryType.Unbounded)
            {
                element.style.display = DisplayStyle.None;
                continue;
            }

            // UI Toolkit's y axis points down: flip the bottom-left screen y before converting.
            var panelPosition = RuntimePanelUtils.ScreenToPanel(
                _root.panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            element.style.display = DisplayStyle.Flex;
            element.style.left = panelPosition.x;
            element.style.top = panelPosition.y;
            element.style.rotate = new Rotate(
                new Angle(-targetIndicator.ScreenPose.rotation.eulerAngles.z, AngleUnit.Degree));
        }
    }

    void OnTargetIndicatorsRemoved(ReadOnlySpan<TargetIndicatorId> removedIds)
    {
        foreach (var removedId in removedIds)
        {
            if (!_activeElements.Remove(removedId, out var element))
                continue;

            element.style.display = DisplayStyle.None;
            _pool.Push(element);
        }
    }
}
```

Notes:
- The rotation is negated because the y-down panel space mirrors the angle direction. Rotate only elements that are arrows, or put the arrow in a child element and rotate that.
- `RuntimePanelUtils.ScreenToPanel` accounts for the Panel Settings scale mode, so element positions need no manual scale factor. Still set `TargetIndicatorManager.BoundaryScale` to your panel's scale so padding and size match your UI's units.

## Verification for either layer

- In Play mode, the number of active elements equals `TargetIndicatorManager.TrackedTargetsCount` (as long as it's ≤ pool size).
- A target directly in front of the camera renders at the screen center.
- The Profiler's GC Alloc column for the layer's handlers stays at 0 B per frame after warm-up (uGUI layer).
