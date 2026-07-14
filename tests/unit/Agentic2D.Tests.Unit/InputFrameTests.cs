using System.Numerics;
using Agentic2D.Input;

namespace Agentic2D.Tests.Unit;

public sealed class InputFrameTests
{
    [Test]
    public async Task DeadZonesCoverBoundaryAndNonFiniteValues()
    {
        await Assert.That(InputMapper.Axial(0.2, 0.2)).IsEqualTo(0d);
        await Assert.That(InputMapper.Axial(-1, 0.2)).IsEqualTo(-1d);
        await Assert.That(InputMapper.Radial(new Vector2(0.2f, 0), 0.2).Length()).IsEqualTo(0f);
        await Assert.That(InputMapper.Radial(new Vector2(1, 0), 0.2).X).IsEqualTo(1f);
        await Assert.That(() => InputMapper.Axial(double.NaN, 0.2)).Throws<ArgumentException>();
    }

    [Test]
    public async Task AccumulatorRetainsHeldStateAndConsumesOneShotStateOnce()
    {
        var map = new InputMap("agentic2d.input-map.v1", InputIds.DefaultMap, "test", InputIds.PlayerOneSource,
            [new(InputIds.Interact, ActionValueKind.Digital)], [new("binding.interact", InputIds.Interact, "keyboard-key", "E")]);
        var accumulator = new InputAccumulator();
        accumulator.Sample(new RawInputSample(1, InputIds.PlayerOneSource, "device.synthetic.keyboard", InputDeviceKind.Keyboard, 1, "E", 1));
        accumulator.Sample(new RawInputSample(2, InputIds.PlayerOneSource, "device.synthetic.mouse", InputDeviceKind.Mouse, 1, "wheel-y", 2));
        var first = accumulator.Consume(map, 1);
        var second = accumulator.Consume(map, 2);
        await Assert.That(first.Digital(InputIds.Interact).Phase).IsEqualTo(DigitalPhase.Pressed);
        await Assert.That(first.Pointers[InputIds.PrimaryPointer].WheelY).IsEqualTo(2d);
        await Assert.That(second.Digital(InputIds.Interact).Phase).IsEqualTo(DigitalPhase.Held);
        await Assert.That(second.Pointers[InputIds.PrimaryPointer].WheelY).IsEqualTo(0d);
    }

    [Test]
    public async Task PointerTransformFlagsOutsideViewportWithoutClamping()
    {
        var pointer = new PointerState(InputIds.PrimaryPointer, InputIds.PlayerOneSource, "device.synthetic.mouse", 0, 500, 0, 0, 0, 0, PointerSpace.Window, true);
        var converted = new ViewportTransform(10, 20, 2).Convert(pointer);
        await Assert.That(converted.X).IsEqualTo(-5d);
        await Assert.That(converted.Y).IsEqualTo(240d);
        await Assert.That(converted.InsideViewport).IsFalse();
    }
}
