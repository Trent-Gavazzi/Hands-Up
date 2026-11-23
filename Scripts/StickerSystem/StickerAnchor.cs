using Godot;
using Godot.Collections;

[Tool]
public partial class StickerAnchor : Node2D
{
    private AnimatedSprite2D _targetSprite;

    [Export]
    public AnimatedSprite2D TargetSprite
    {
        get => _targetSprite;
        set
        {
            _targetSprite = value;
            ResetTracking();
        }
    }

    // One entry per (animation, frame) pair, holding the transform for that frame.
    [Export] public Array<StickerFrameData> Keyframes { get; set; } = new();

    [ExportGroup("Editor Helpers")]
    [ExportToolButton("Initialize Current Anim")]
    public Callable InitAnimButton => Callable.From(InitializeCurrentAnimation);

    [ExportToolButton("Bake Current Frame")]
    public Callable BakeFrameButton => Callable.From(BakeCurrentFrame);

    [ExportToolButton("Bake & Next Frame")]
    public Callable BakeAndNextButton => Callable.From(BakeAndNextFrame);

    [ExportToolButton("Apply Current Frame")]
    public Callable ApplyFrameButton => Callable.From(ForceApplyCurrentFrame);

    private string _lastAnim;
    private int _lastFrame = -1;

    public override void _Ready()
    {
        // Run after the sprite has advanced its frame this tick.
        ProcessPriority = 1;
        ResetTracking();
    }

    public override void _Process(double delta)
    {
        if (_targetSprite == null || !GodotObject.IsInstanceValid(_targetSprite)) return;

        string anim = _targetSprite.Animation;
        int frame = _targetSprite.Frame;

        if (anim == _lastAnim && frame == _lastFrame) return;

        _lastAnim = anim;
        _lastFrame = frame;
        ApplyFrame(anim, frame);
    }

    // ---------- Lookup / apply ----------

    private int FindIndex(string anim, int frame)
    {
        if (Keyframes == null) return -1;

        for (int i = 0; i < Keyframes.Count; i++)
        {
            var k = Keyframes[i];
            if (k != null && k.AnimationName == anim && k.Frame == frame)
                return i;
        }
        return -1;
    }

    private void ApplyFrame(string anim, int frame)
    {
        int index = FindIndex(anim, frame);
        if (index >= 0)
            Keyframes[index].ApplyTo(this);
    }

    private void ResetTracking()
    {
        _lastAnim = null;
        _lastFrame = -1;
    }

    private bool HasValidSprite()
    {
        return _targetSprite != null
            && GodotObject.IsInstanceValid(_targetSprite)
            && _targetSprite.SpriteFrames != null;
    }

    private void MarkDirty()
    {
        if (!Engine.IsEditorHint()) return;

        NotifyPropertyListChanged();
        EditorInterface.Singleton?.MarkSceneAsUnsaved();
    }

    // ---------- Editor buttons ----------

    private void ForceApplyCurrentFrame()
    {
        if (!HasValidSprite()) return;
        ApplyFrame(_targetSprite.Animation, _targetSprite.Frame);
    }

    private void InitializeCurrentAnimation()
    {
        if (!HasValidSprite()) return;

        Keyframes ??= new Array<StickerFrameData>();

        string anim = _targetSprite.Animation;
        int count = _targetSprite.SpriteFrames.GetFrameCount(anim);

        for (int f = 0; f < count; f++)
        {
            if (FindIndex(anim, f) < 0)
                Keyframes.Add(StickerFrameData.FromNode(this, anim, f));
        }

        MarkDirty();
    }

    private void BakeCurrentFrame()
    {
        if (!HasValidSprite()) return;

        Keyframes ??= new Array<StickerFrameData>();

        string anim = _targetSprite.Animation;
        int frame = _targetSprite.Frame;

        // Replace (rather than mutate) so shared resources are never edited by accident.
        var data = StickerFrameData.FromNode(this, anim, frame);
        int index = FindIndex(anim, frame);

        if (index >= 0)
            Keyframes[index] = data;
        else
            Keyframes.Add(data);

        MarkDirty();
    }

    private void BakeAndNextFrame()
    {
        if (!HasValidSprite()) return;

        BakeCurrentFrame();

        int count = _targetSprite.SpriteFrames.GetFrameCount(_targetSprite.Animation);
        if (count > 0)
        {
            // _Process will pick up the frame change and apply the next frame's data.
            _targetSprite.Frame = (_targetSprite.Frame + 1) % count;
        }
    }
}