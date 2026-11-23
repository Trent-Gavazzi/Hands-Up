using Godot;

[GlobalClass, Tool]
public partial class StickerFrameData : Resource
{
    [Export] public string AnimationName { get; set; } = "";
    [Export] public int Frame { get; set; } = 0;

    [Export] public Vector2 Position { get; set; } = Vector2.Zero;
    [Export] public float RotationDegrees { get; set; } = 0f;
    [Export] public Vector2 Scale { get; set; } = Vector2.One;

    public void ApplyTo(Node2D node)
    {
        node.Position = Position;
        node.RotationDegrees = RotationDegrees;
        node.Scale = Scale;
    }

    public static StickerFrameData FromNode(Node2D node, string animationName, int frame)
    {
        return new StickerFrameData
        {
            AnimationName = animationName,
            Frame = frame,
            Position = node.Position,
            RotationDegrees = node.RotationDegrees,
            Scale = node.Scale
        };
    }
}