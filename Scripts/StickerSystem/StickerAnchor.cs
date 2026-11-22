using Godot;
using Godot.Collections;

[Tool]
public partial class StickerAnchor : Node2D
{
    [Export] public AnimatedSprite2D TargetSprite { get; set; }

    // Map "AnimationName" -> Array of StickerFrameData (one per frame)
    [Export] public Dictionary<string, Array<StickerFrameData>> FrameData { get; set; } = new();

    [ExportGroup("Editor Helpers")]
    [ExportToolButton("Initialize Current Anim")]
    public Callable InitAnimButton => Callable.From(InitializeCurrentAnimation);

    [ExportToolButton("Bake Current Frame")]
    public Callable BakeFrameButton => Callable.From(BakeCurrentFrame);

    [ExportToolButton("Bake & Next Frame")]
    public Callable BakeAndNextButton => Callable.From(BakeAndNextFrame);

    private void BakeAndNextFrame()
    {
        if (TargetSprite?.SpriteFrames == null) return;

        BakeCurrentFrame();

        string anim = TargetSprite.Animation;
        int count = TargetSprite.SpriteFrames.GetFrameCount(anim);
        if (count > 0)
        {
            TargetSprite.Frame = (TargetSprite.Frame + 1) % count;
        }
    }

    private AnimatedSprite2D _connectedSprite;

    


    public override void _Ready()
    {
        ConnectSprite();
        ApplyCurrentFrame();
    }

    public override void _Process(double delta)
    {
        // Reconnect automatically if you assign/change TargetSprite in the Inspector
        if (Engine.IsEditorHint() && _connectedSprite != TargetSprite)
        {
            ConnectSprite();
            ApplyCurrentFrame();
        }
    }

    private void ConnectSprite()
    {
        if (_connectedSprite != null && GodotObject.IsInstanceValid(_connectedSprite))
        {
            _connectedSprite.FrameChanged -= ApplyCurrentFrame;
            _connectedSprite.AnimationChanged -= ApplyCurrentFrame;
        }

        _connectedSprite = TargetSprite;

        if (_connectedSprite != null)
        {
            _connectedSprite.FrameChanged += ApplyCurrentFrame;
            _connectedSprite.AnimationChanged += ApplyCurrentFrame;
        }
    }

    private void ApplyCurrentFrame()
    {
        if (TargetSprite == null || FrameData == null) return;

        string anim = TargetSprite.Animation;
        int frame = TargetSprite.Frame;

        if (FrameData.TryGetValue(anim, out var frames) && frame >= 0 && frame < frames.Count)
        {
            frames[frame]?.ApplyTo(this);
        }
    }

    private void InitializeCurrentAnimation()
    {
        if (TargetSprite?.SpriteFrames == null) return;

        string anim = TargetSprite.Animation;
        int count = TargetSprite.SpriteFrames.GetFrameCount(anim);

        if (!FrameData.ContainsKey(anim))
        {
            FrameData[anim] = new Array<StickerFrameData>();
        }

        var list = FrameData[anim];
        while (list.Count < count)
        {
            list.Add(StickerFrameData.FromNode(this));
        }

        NotifyPropertyListChanged();
    }

    private void BakeCurrentFrame()
    {
        if (TargetSprite?.SpriteFrames == null) return;

        InitializeCurrentAnimation();

        string anim = TargetSprite.Animation;
        int frame = TargetSprite.Frame;

        FrameData[anim][frame] = StickerFrameData.FromNode(this);
        NotifyPropertyListChanged();
    }
}