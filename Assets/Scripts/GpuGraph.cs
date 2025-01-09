using UnityEngine;
using static FunctionLibrary;

public class GpuGraph : MonoBehaviour
{
    private const int MaxResolution = 1000;

    [SerializeField, Range(10, MaxResolution)]
    private int resolution = 10;

    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private Material material;
    [SerializeField] private Mesh mesh;
    [SerializeField] private FunctionName functionName;

    private enum TransitionMode
    {
        Cycle,
        Random
    }

    [SerializeField] private TransitionMode transitionMode = TransitionMode.Cycle;
    [SerializeField, Min(0f)] private float functionDuration = 1f, transitionDuration = 1f;

    private ComputeBuffer _positionsBuffer;
    private float _duration;
    private bool _transitioning;
    private FunctionName _transitionFunction;

    private static readonly int
        PositionsId = Shader.PropertyToID("_Positions"),
        ResolutionId = Shader.PropertyToID("_Resolution"),
        StepId = Shader.PropertyToID("_Step"),
        TimeId = Shader.PropertyToID("_Time"),
        TransitionProgressId = Shader.PropertyToID("_TransitionProgress");


    private void OnEnable()
    {
        const int sizeOfVector = 3;
        _positionsBuffer = new ComputeBuffer(MaxResolution * MaxResolution, sizeOfVector * sizeof(float));
    }

    private void OnDisable()
    {
        _positionsBuffer.Release();
        _positionsBuffer = null;
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

        UpdateFunctionOnGPU();
    }

    private void PickNextFunction()
    {
        functionName = transitionMode == TransitionMode.Cycle
            ? GetNextFunctionName(functionName)
            : GetRandomFunctionName(functionName);
    }

    private void UpdateFunctionOnGPU()
    {
        var step = 2f / resolution;
        computeShader.SetInt(ResolutionId, resolution);
        computeShader.SetFloat(StepId, step);
        computeShader.SetFloat(TimeId, Time.time);
        if (_transitioning)
        {
            computeShader.SetFloat(
                TransitionProgressId,
                Mathf.SmoothStep(0f, 1f, _duration / transitionDuration)
            );
        }

        var kernelIndex =
            (int)functionName + (int)(_transitioning ? _transitionFunction : functionName) * FunctionCount;
        computeShader.SetBuffer(kernelIndex, PositionsId, _positionsBuffer);
        var groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(kernelIndex, groups, groups, 1);
        material.SetBuffer(PositionsId, _positionsBuffer);
        material.SetFloat(StepId, step);
        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + step));
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, resolution * resolution);
    }
}