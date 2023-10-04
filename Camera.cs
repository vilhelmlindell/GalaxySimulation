using Godot;

namespace GalaxySimulation;

public partial class Camera : Camera3D
{
    private const float Offset = 0.01f;
    
    [Export] private float _distanceToPivot = 40f;
    [Export] private float _lookSensitivity = 1f;
    [Export] private float _zoomSensitivity = 1f;

    private Vector3 _pivotPoint = Vector3.Zero;

    private Vector2 _rotation = new Vector2(0, -Mathf.Pi / 2 + Offset);

    private Vector2 _previousMousePosition;

    public override void _Ready()
    {
        _previousMousePosition = GetViewport().GetMousePosition();
        
        UpdateCameraPosition();
    }

    public override void _Process(double delta)
    {
        var mouseMovement = GetMouseDelta();

        if (!Input.IsMouseButtonPressed(MouseButton.Right)) return;
        
        _rotation -= mouseMovement * _lookSensitivity * 0.001f;

        _rotation.Y = Mathf.Clamp(_rotation.Y, -Mathf.Pi / 2 + Offset, Mathf.Pi / 2 - Offset);

        UpdateCameraPosition();
    }

    private void UpdateCameraPosition()
    {
        var pivotRadius = new Vector3(0, 0, _distanceToPivot);

        pivotRadius = pivotRadius.Rotated(Vector3.Right, _rotation.Y).Rotated(Vector3.Up, _rotation.X);

        GlobalPosition = pivotRadius;

        LookAt(_pivotPoint);
    }

    private Vector2 GetMouseDelta()
    {
        var mousePosition = GetViewport().GetMousePosition();

        var mouseDelta = mousePosition - _previousMousePosition;

        _previousMousePosition = mousePosition;

        return mouseDelta;
    }
}