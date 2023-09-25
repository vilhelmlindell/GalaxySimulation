using Godot;
using System;

public partial class Camera : Camera3D
{
    [Export]
    private float _distanceToOrigin = 40f;

    private float _sensitivity = 0.01f;

    private Vector3 _pivotPoint = Vector3.Zero;

    private Vector3 _rotation;

    private Vector2 _previousMousePosition;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GlobalPosition = new Vector3(0, 0, _distanceToOrigin);
        _previousMousePosition = GetViewport().GetMousePosition();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        var mouseMovement = GetMouseDelta();

        if (Input.IsMouseButtonPressed(MouseButton.Right))
        {
            _rotation.X -= mouseMovement.X * _sensitivity;
            _rotation.Y -= mouseMovement.Y * _sensitivity;

            LookAt(_pivotPoint);
        }
    }

    private Vector2 GetMouseDelta()
    {
        Vector2 mousePosition = GetViewport().GetMousePosition();
        Vector2 screenRectSize = GetViewport().GetVisibleRect().Size;

        Vector2 mouseDelta = mousePosition - _previousMousePosition;

        _previousMousePosition = GetViewport().GetMousePosition();

        return mouseDelta;
    }
}
