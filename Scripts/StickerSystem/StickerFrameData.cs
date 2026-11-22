using Godot;

[GlobalClass, Tool]
public partial class StickerFrameData : Resource
{
    [Export] public Vector2 Position { get; set; } = Vector2.Zero;
    [Export] public float RotationDegrees { get; set; } = 0f;
    [Export] public Vector2 Scale { get; set; } = Vector2.One;

    public void ApplyTo(Node2D node)
    {
        node.Position = Position;
        node.RotationDegrees = RotationDegrees;
        node.Scale = Scale;
    }

    public static StickerFrameData FromNode(Node2D node)
    {
        return new StickerFrameData
        {
            Position = node.Position,
            RotationDegrees = node.RotationDegrees,
            Scale = node.Scale
        };
    }
}