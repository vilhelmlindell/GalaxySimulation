using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Godot;
using Godot.Collections;

namespace GalaxySimulation;

public partial class Galaxy : Node3D
{
    [ExportGroup("Initial settings")]
    [Export] private int _starCount = 100;
    [Export] private float _galaxyRadius = 100f;
    [Export] private float _initialVelocity = 1f;
    
    [ExportGroup("Real time settings")]
    [Export] private float _timeStep = 1f;
    [Export] private float _smoothingLength = 0.016f;
    [Export] private Color _starColor = Colors.Aqua;
    
    private Star[] _stars;
    private RenderingDevice _renderingDevice;
    private Rid _shader;
    private Rid _uniformBuffer;
    private Rid _starBuffer;
    private Rid _uniformSet;
    private Rid _pipeline;
    private MultiMeshInstance3D _multiMeshInstance;

    private struct Star
    {
        // Use padding since vec3 unpacks 4 floats in glsl: https://github.com/godotengine/godot/issues/81511 
#pragma warning disable CS0169
        public Vector3 Position;
        private float _padding1;
        public Vector3 Velocity;
        private float _padding2;
#pragma warning restore CS0169
    }

    public override void _Ready()
    {
        InitializeRendering();
        InitializeStars();
    }

    public override void _Process(double delta)
    {
        UpdateUniforms();
        ComputeStars();
        UpdateMultiMeshInstance();
        GD.Print("FPS: " + Engine.GetFramesPerSecond());
    }

    public override void _ExitTree()
    {
        _renderingDevice.FreeRid(_shader);
        _renderingDevice.FreeRid(_uniformSet);
        _renderingDevice.FreeRid(_starBuffer);
        _renderingDevice.FreeRid(_uniformBuffer);
        _renderingDevice.FreeRid(_pipeline);
        _renderingDevice.Free();
    }

    private void InitializeRendering()
    {
        _multiMeshInstance = GetChild<MultiMeshInstance3D>(0);
        _renderingDevice = RenderingServer.CreateLocalRenderingDevice();

        var shaderFile = GD.Load<RDShaderFile>("res://galaxy_simulation.glsl");
        var shaderBytecode = shaderFile.GetSpirV();
        _shader = _renderingDevice.ShaderCreateFromSpirV(shaderBytecode);
        _pipeline = _renderingDevice.ComputePipelineCreate(_shader);
    }

    private void InitializeStars()
    {
        _stars = new Star[_starCount];
        _multiMeshInstance.Multimesh.InstanceCount = _stars.Length;

        for (var i = 0; i < _stars.Length; i++)
        {
            var position = RandomStarPosition();
            var normalizedVelocity = new Vector3(position.Z, position.Y, -position.X).Normalized() * _initialVelocity;
            _stars[i] = new Star { Position = position, Velocity = normalizedVelocity};
        }

    }

    private Vector3 RandomStarPosition()
    {
        var random = new RandomNumberGenerator();
        while (true)
        {
            var x = random.Randf() * 2 * _galaxyRadius - _galaxyRadius;
            var z = random.Randf() * 2 * _galaxyRadius - _galaxyRadius;
            
            if (!(x * x + z * z < _galaxyRadius * _galaxyRadius)) continue;
            
            var y = (random.Randf() - 0.5f) * _galaxyRadius / 5;
            return new Vector3(x, y, z);
        }
    }

    private void UpdateUniforms()
    {
        var uniformBytes = ArrayToBytes(new[]{ _timeStep, 0, _smoothingLength, 0});
        _uniformBuffer = _renderingDevice.UniformBufferCreate((uint)uniformBytes.Length, uniformBytes);
        var uniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0
        };
        uniform.AddId(_uniformBuffer);
        
        var starBytes = ArrayToBytes(_stars);
        _starBuffer = _renderingDevice.StorageBufferCreate((uint)starBytes.Length, starBytes);
        var starUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 1
        };
        starUniform.AddId(_starBuffer);
        
        _uniformSet = _renderingDevice.UniformSetCreate(
            new Array<RDUniform> { uniform, starUniform },
            _shader,
            0
        );
    }

    private void ComputeStars()
    {
        if (_stars.Length == 0)
            return;

        var computeList = _renderingDevice.ComputeListBegin();
        _renderingDevice.ComputeListBindComputePipeline(computeList, _pipeline);
        _renderingDevice.ComputeListBindUniformSet(computeList, _uniformSet, 0);
        _renderingDevice.ComputeListDispatch(
            computeList,
            xGroups: 10,
            yGroups: 1,
            zGroups: 1
        );
        _renderingDevice.ComputeListEnd();

        var outputBytes = _renderingDevice.BufferGetData(_starBuffer);
        
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        _stars = BytesToArray<Star>(outputBytes);
        stopwatch.Stop();
        
        GD.Print("Elapsed: " + stopwatch.ElapsedMilliseconds);

        _renderingDevice.FreeRid(_uniformBuffer);
        _renderingDevice.FreeRid(_starBuffer);
    }

    private void UpdateMultiMeshInstance()
    {
        for (var i = 0; i < _stars.Length; i++)
        {
            _multiMeshInstance.Multimesh.SetInstanceTransform(
                i,
                new Transform3D(Basis.Identity, _stars[i].Position)
            );
            _multiMeshInstance.Multimesh.SetInstanceColor(
                i,
                _starColor
            );
        }
    }

    private static byte[] ArrayToBytes<T>(T[] items) where T : struct
    {
        return MemoryMarshal.Cast<T, byte>(items).ToArray();
    }

    private static T[] BytesToArray<T>(byte[] bytes) where T : struct
    {
        return MemoryMarshal.Cast<byte, T>(bytes).ToArray();
    }
}