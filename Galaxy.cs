using System;
using System.Runtime.InteropServices;
using Godot;
using Godot.Collections;

public partial class Galaxy : Node3D
{
    [Export]
    private int _starCount = 100;
    private Color _color;

    private Star[] _stars;
    private RenderingDevice _renderingDevice;
    private Rid _shader;
    private Rid _uniformSet;
    private Rid _pipeline;
    private Rid _starBuffer;

    private MultiMeshInstance3D _multiMeshInstance;

    public struct Star
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Acceleration;
    }

    public override void _Ready()
    {
        _multiMeshInstance = GetChild<MultiMeshInstance3D>(0);
        _renderingDevice = RenderingServer.CreateLocalRenderingDevice();

        var shaderFile = GD.Load<RDShaderFile>("res://galaxy_simulation.glsl");
        var shaderBytecode = shaderFile.GetSpirV();
        _shader = _renderingDevice.ShaderCreateFromSpirV(shaderBytecode);

        _pipeline = _renderingDevice.ComputePipelineCreate(_shader);

        _stars = new Star[_starCount];

        _multiMeshInstance.Multimesh.InstanceCount = _stars.Length;

        var random = new RandomNumberGenerator();
        _color = new Color(random.Randf(), random.Randf(), random.Randf());

        for (var i = 0; i < _stars.Length; i++)
        {
            var position = new Vector3(i * 1f, 0f, 0f);
            _stars[i] = new Star
            {
                Position = position,
                Velocity = new Vector3(0.0f, 0.0f, 0.0f),
                Acceleration = new Vector3(0.0f, 0.0f, 0.0f),
            };
        }

        var inputBytes = StarArrayToBytes(_stars);

        _starBuffer = _renderingDevice.StorageBufferCreate((uint)inputBytes.Length, inputBytes);

        var uniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        uniform.AddId(_starBuffer);
        _uniformSet = _renderingDevice.UniformSetCreate(
            new Array<RDUniform> { uniform },
            _shader,
            0
        );

        UpdateMultiMeshInstance();
    }

    public override void _Process(double delta)
    {
        ComputeStars();
        UpdateMultiMeshInstance();
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
            xGroups: 1,
            yGroups: 1,
            zGroups: 1
        );
        _renderingDevice.ComputeListEnd();

        var outputBytes = _renderingDevice.BufferGetData(_starBuffer);
        _stars = ByteArrayToStars(outputBytes);
    }

    private void UpdateMultiMeshInstance()
    {
        for (var i = 0; i < _stars.Length; i++)
        {
            _multiMeshInstance.Multimesh.SetInstanceTransform(
                i,
                new Transform3D(Basis.Identity, _stars[i].Position)
            );
            _multiMeshInstance.Multimesh.SetInstanceColor(i, _color);
        }
    }

    private byte[] StarArrayToBytes(Star[] stars)
    {
        var result = MemoryMarshal.Cast<Star, byte>(stars).ToArray();
        return result;
    }

    private Star[] ByteArrayToStars(byte[] bytes)
    {
        var result = MemoryMarshal.Cast<byte, Star>(bytes).ToArray();
        return result;
    }
}
