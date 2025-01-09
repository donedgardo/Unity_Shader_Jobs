using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Serialization;
using static Unity.Mathematics.math;
using float3x4 = Unity.Mathematics.float3x4;
using float3 = Unity.Mathematics.float3;
using quaternion = Unity.Mathematics.quaternion;
using Random = UnityEngine.Random;


public class Fractal : MonoBehaviour
{
    [SerializeField, Range(3, 8)] public int depth = 4;
    [SerializeField] private Mesh mesh;
    [SerializeField] private Mesh leafMesh;
    [SerializeField] private Material material;
    [SerializeField] private Gradient gradientA, gradientB;
    [SerializeField] private Color leafColorA, leafColorB;
    [SerializeField, Range(0f, 90f)] private float maxSagAngleA = 15f, maxSagAngleB = 25f;
    [SerializeField, Range(0f, 90f)] private float spinSpeedA = 20f, spinSpeedB = 25f;
    [SerializeField, Range(0f, 1f)] private float reverseSpinChance = 0.25f;


    private struct FractalPart
    {
        public float3 WorldPosition;
        public quaternion Rotation, WorldRotation;
        public float SpinAngle, MaxSagAngle, SpinVelocity;
    }

    private NativeArray<FractalPart>[] _fractalParts;
    private NativeArray<float3x4>[] _matrices;

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    private struct UpdateFractalLevelJob : IJobFor
    {
        public float Scale;
        public float deltaTime;
        [ReadOnly] public NativeArray<FractalPart> Parents;
        [WriteOnly] public NativeArray<float3x4> Matrices;
        public NativeArray<FractalPart> Parts;

        public void Execute(int index)
        {
            var parent = Parents[index / 5];
            var part = Parts[index];
            part.SpinAngle += part.SpinVelocity * deltaTime;
            var upAxis = mul(mul(parent.WorldRotation, part.Rotation), up());
            var sagAxis = cross(up(), upAxis);
            var sagMagnitude = length(sagAxis);
            quaternion baseRotation;
            if (sagMagnitude > 0f)
            {
                sagAxis /= sagMagnitude;
                var sagRotation = quaternion.AxisAngle(sagAxis, part.MaxSagAngle * sagMagnitude);
                baseRotation = mul(sagRotation, parent.WorldRotation);
            }
            else
            {
                baseRotation = parent.WorldRotation;
            }

            part.WorldRotation = mul(baseRotation,
                mul(part.Rotation, quaternion.RotateY(part.SpinAngle))
            );
            part.WorldPosition =
                parent.WorldPosition +
                mul(part.WorldRotation, float3(0f, 1.5f * Scale, 0));
            Parts[index] = part;
            var r = float3x3(part.WorldRotation) * Scale;
            Matrices[index] = float3x4(r.c0, r.c1, r.c2, part.WorldPosition);
        }
    }

    private static readonly quaternion[] Rotations =
    {
        quaternion.identity,
        quaternion.RotateZ(-0.5f * PI), quaternion.RotateZ(0.5f * PI),
        quaternion.RotateX(0.5f * PI), quaternion.RotateX(-0.5f * PI)
    };


    private ComputeBuffer[] _matricesBuffers;

    private readonly int
        _colorAId = Shader.PropertyToID("_ColorA"),
        _colorBId = Shader.PropertyToID("_ColorB"),
        _matricesId = Shader.PropertyToID("_Matrices"),
        _sequenceNumbersId = Shader.PropertyToID("_SequenceNumbers");

    private static MaterialPropertyBlock _propertyBlock;
    private static Vector4[] _sequenceNumber;

    private void OnEnable()
    {
        _fractalParts = new NativeArray<FractalPart>[depth];
        _matrices = new NativeArray<float3x4>[depth];
        _matricesBuffers = new ComputeBuffer[depth];
        _sequenceNumber = new Vector4[depth];
        const int stride = sizeof(float) * 12;
        for (int i = 0, length = 1; i < _fractalParts.Length; i++, length *= 5)
        {
            _fractalParts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            _matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
            _matricesBuffers[i] = new ComputeBuffer(length, stride);
            _sequenceNumber[i] = new Vector4(Random.value, Random.value, Random.value, Random.value);
        }

        _fractalParts[0][0] = CreatePart(0);
        for (var li = 1; li < _fractalParts.Length; li++)
        {
            var levelParts = _fractalParts[li];
            for (var fpi = 0; fpi < levelParts.Length; fpi += 5)
            {
                for (var ci = 0; ci < 5; ci++)
                {
                    levelParts[fpi + ci] = CreatePart(ci);
                }
            }
        }

        _propertyBlock ??= new MaterialPropertyBlock();
    }

    private void OnDisable()
    {
        for (var i = 0; i < _matricesBuffers.Length; i++)
        {
            _matricesBuffers[i].Release();
            _fractalParts[i].Dispose();
            _matrices[i].Dispose();
        }

        _fractalParts = null;
        _matrices = null;
        _matricesBuffers = null;
        _sequenceNumber = null;
    }

    private void OnValidate()
    {
        if (_fractalParts == null || !enabled) return;
        OnDisable();
        OnEnable();
    }

    private void Update()
    {
        var deltaTime = Time.deltaTime;
        var rootPart = _fractalParts[0][0];
        rootPart.SpinAngle += rootPart.SpinVelocity * deltaTime;
        rootPart.WorldRotation = mul(
            transform.rotation,
            mul(rootPart.Rotation, quaternion.RotateY(rootPart.SpinAngle))
        );
        rootPart.WorldPosition = transform.position;
        var objectScale = transform.lossyScale.x;
        _fractalParts[0][0] = rootPart;
        var r = float3x3(rootPart.WorldRotation) * objectScale;
        _matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.WorldPosition);
        var scale = objectScale;
        JobHandle jobHandle = default;
        for (var li = 1; li < _fractalParts.Length; li++)
        {
            scale *= 0.5f;
            jobHandle = new UpdateFractalLevelJob
            {
                deltaTime = deltaTime,
                Scale = scale,
                Parents = _fractalParts[li - 1],
                Parts = _fractalParts[li],
                Matrices = _matrices[li]
            }.ScheduleParallel(_fractalParts[li].Length, 5, jobHandle);
        }

        jobHandle.Complete();


        var bounds = new Bounds(rootPart.WorldPosition, 3f * objectScale * Vector3.one);
        var leafIndex = _matricesBuffers.Length - 1;
        for (var i = 0; i < _matricesBuffers.Length; i++)
        {
            Color colorA, colorB;
            Mesh instanceMesh;
            if (i == leafIndex)
            {
                colorA = leafColorA;
                colorB = leafColorB;
                instanceMesh = leafMesh;
            }
            else
            {
                var gradientInterpolator = i / (_matricesBuffers.Length - 2f);
                colorA = gradientA.Evaluate(gradientInterpolator);
                colorB = gradientB.Evaluate(gradientInterpolator);
                instanceMesh = mesh;
            }

            var buffer = _matricesBuffers[i];
            buffer.SetData(_matrices[i]);
            _propertyBlock.SetColor(_colorAId, colorA);
            _propertyBlock.SetColor(_colorBId, colorB);
            _propertyBlock.SetBuffer(_matricesId, buffer);
            _propertyBlock.SetVector(_sequenceNumbersId, _sequenceNumber[i]);
            Graphics.DrawMeshInstancedProcedural(
                instanceMesh,
                0,
                material,
                bounds,
                buffer.count,
                _propertyBlock
            );
        }
    }

    private FractalPart CreatePart(int childIndex) => new()
    {
        MaxSagAngle = radians(Random.Range(maxSagAngleA, maxSagAngleB)),
        Rotation = Rotations[childIndex],
        SpinVelocity = (Random.value < reverseSpinChance ? -1f : 1f) *
                       radians(Random.Range(spinSpeedA, spinSpeedB))
    };
}