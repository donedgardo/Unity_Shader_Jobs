using UnityEngine;
using static FunctionLibrary;

public class Graph : MonoBehaviour
{
    [SerializeField] private Transform pointPrefab;
    [SerializeField, Range(10, 200)] private int resolution = 10;

    [SerializeField] private FunctionName functionName;

    private enum TransitionMode
    {
        Cycle,
        Random
    }

    [SerializeField] private TransitionMode transitionMode = TransitionMode.Cycle;
    [SerializeField, Min(0f)] private float functionDuration = 1f, transitionDuration = 1f;

    private Transform[] _points;
    private float _duration;
    private bool _transitioning;
    private FunctionName _transitionFunction;

    private void Awake()
    {
        var step = 2f / resolution;
        var scale = Vector3.one * step;
        _points = new Transform[resolution * resolution];
        for (var i = 0; i < _points.Length; i++)
        {
            var point = _points[i] = Instantiate(pointPrefab, transform, false);
            point.localScale = scale;
        }
    }

    private void Update()
    {
        _duration += Time.deltaTime;
        if (_duration > functionDuration)
        {
            _duration -= functionDuration;
            _transitioning = true;
            _transitionFunction = functionName;
            PickNextFunction();
        }

        if (_transitioning)
        {
            UpdateGraphTransitionPoints();
        }
        else
        {
            UpdateGraphPoints();
        }
    }

    private void PickNextFunction()
    {
        functionName = transitionMode == TransitionMode.Cycle
            ? GetNextFunctionName(functionName)
            : GetRandomFunctionName(functionName);
    }

    private void UpdateGraphPoints()
    {
        var f = GetFunction(functionName);
        var time = Time.time;
        var step = 2f / resolution;
        var v = 0.5f * step - 1f;
        for (int i = 0, x = 0, z = 0; i < _points.Length; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z += 1;
                v = (z + 0.5f) * step - 1f;
            }

            var u = (x + 0.5f) * step - 1f;
            _points[i].localPosition = f(u, v, time);
        }
    }

    private void UpdateGraphTransitionPoints()
    {
        Function from = GetFunction(_transitionFunction), to = GetFunction(functionName);
        var progress = _duration / transitionDuration;
        var time = Time.time;
        var step = 2f / resolution;
        var v = 0.5f * step - 1f;
        for (int i = 0, x = 0, z = 0; i < _points.Length; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z += 1;
                v = (z + 0.5f) * step - 1f;
            }

            var u = (x + 0.5f) * step - 1f;
            _points[i].localPosition = Morph(u, v, time, from, to, progress);
        }
    }
}