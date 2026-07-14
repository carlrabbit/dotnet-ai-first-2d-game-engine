using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agentic2D.Input;

public static class InputIds
{
    public const string PlayerOneSource = "input-source.player-1";
    public const string DefaultMap = "input-map.player.default";
    public const string Move = "action.move";
    public const string Interact = "action.interact";
    public const string Zoom = "action.zoom";
    public const string PrimaryPointer = "pointer.primary";
}

public enum InputDeviceKind { Keyboard, Mouse, Controller, Synthetic, Replay }
public enum ActionValueKind { Digital, Scalar, Vector2 }
public enum DigitalPhase { Inactive, Pressed, Held, Released }
public enum PointerSpace { Window, LogicalViewport, World }

public sealed record RawInputSample(long Sequence, string InputSourceId, string PhysicalDeviceId, InputDeviceKind DeviceKind, long PresentationSampleIndex, string Control, double Value = 0, double? X = null, double? Y = null, PointerSpace Space = PointerSpace.Window, string? Provenance = null, IReadOnlyList<string>? Diagnostics = null);
public sealed record InputDiagnostic(string Id, string Severity, string Message, string? Target = null);
public sealed record InputAction(string Id, ActionValueKind ValueKind);
public sealed record InputBinding(string Id, string ActionId, string Kind, string? Control = null, double Scale = 1, double DeadZone = 0, IReadOnlyDictionary<string, string>? Composite = null);
public sealed record InputMap(string Schema, string Id, string Revision, string InputSourceId, IReadOnlyList<InputAction> Actions, IReadOnlyList<InputBinding> Bindings)
{
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true, Converters = { new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower) } };
}

public sealed record DigitalActionValue(double Value, DigitalPhase Phase);
public sealed record ScalarActionValue(double Value);
public sealed record Vector2ActionValue(double X, double Y);
public sealed record PointerState(string Id, string InputSourceId, string PhysicalDeviceId, double X, double Y, double DeltaX, double DeltaY, double WheelX, double WheelY, PointerSpace Space, bool InsideViewport, double? WorldX = null, double? WorldY = null, IReadOnlyList<string>? Diagnostics = null);
public sealed record InputFrame(string Schema, int Tick, long FrameSequence, string InputSourceId, string InputMapId, string InputMapRevision, IReadOnlyDictionary<string, DigitalActionValue> DigitalActions, IReadOnlyDictionary<string, ScalarActionValue> ScalarActions, IReadOnlyDictionary<string, Vector2ActionValue> VectorActions, IReadOnlyDictionary<string, PointerState> Pointers, IReadOnlyList<long> RawSampleSequences, IReadOnlyList<InputDiagnostic> Diagnostics)
{
    public DigitalActionValue Digital(string id) => DigitalActions.TryGetValue(id, out var value) ? value : new(0, DigitalPhase.Inactive);
    public ScalarActionValue Scalar(string id) => ScalarActions.TryGetValue(id, out var value) ? value : new(0);
    public Vector2ActionValue Vector2(string id) => VectorActions.TryGetValue(id, out var value) ? value : new(0, 0);
}

public sealed record ViewportTransform(double LetterboxOffsetX, double LetterboxOffsetY, int IntegerScale, double LogicalWidth = 320, double LogicalHeight = 180, double CameraX = 0, double CameraY = 0)
{
    public PointerState Convert(PointerState pointer)
    {
        if (pointer.Space != PointerSpace.Window) return pointer;
        if (IntegerScale <= 0) throw new ArgumentOutOfRangeException(nameof(IntegerScale));
        var x = (pointer.X - LetterboxOffsetX) / IntegerScale;
        var y = (pointer.Y - LetterboxOffsetY) / IntegerScale;
        var inside = x >= 0 && y >= 0 && x < LogicalWidth && y < LogicalHeight;
        return pointer with { X = x, Y = y, DeltaX = pointer.DeltaX / IntegerScale, DeltaY = pointer.DeltaY / IntegerScale, Space = PointerSpace.LogicalViewport, InsideViewport = inside, WorldX = x + CameraX, WorldY = y + CameraY };
    }
}

public sealed record InputMapValidationResult(InputMap? Map, IReadOnlyList<InputDiagnostic> Diagnostics)
{
    public bool IsSuccess => Map is not null && Diagnostics.All(x => x.Severity != "error");
}

public static class InputMapValidator
{
    private static readonly HashSet<string> BindingKinds = ["keyboard-key", "mouse-button", "mouse-wheel-x", "mouse-wheel-y", "controller-button", "controller-axis", "controller-stick", "composite-vector2"];
    private static readonly HashSet<string> MouseControls = ["primary", "secondary", "middle", "auxiliary-1", "auxiliary-2"];
    private static readonly HashSet<string> ControllerButtons = ["south", "east", "west", "north", "left-shoulder", "right-shoulder", "left-stick", "right-stick", "start", "select", "dpad-up", "dpad-down", "dpad-left", "dpad-right"];
    private static readonly HashSet<string> ControllerAxes = ["left-stick-x", "left-stick-y", "right-stick-x", "right-stick-y", "left-trigger", "right-trigger"];
    public static InputMapValidationResult Validate(InputMap map)
    {
        var diagnostics = new List<InputDiagnostic>();
        if (map.Schema != "agentic2d.input-map.v1") diagnostics.Add(new("INPUTMAP0001", "error", "Unsupported input-map schema.", "schema"));
        if (map.Id != InputIds.DefaultMap) diagnostics.Add(new("INPUTMAP0008", "error", "Unsupported input-map identity.", "id"));
        if (map.InputSourceId != InputIds.PlayerOneSource) diagnostics.Add(new("INPUTMAP0008", "error", "Only input-source.player-1 is supported.", "inputSourceId"));
        var actions = new Dictionary<string, InputAction>(StringComparer.Ordinal);
        foreach (var action in map.Actions.OrderBy(x => x.Id, StringComparer.Ordinal)) if (!actions.TryAdd(action.Id, action)) diagnostics.Add(new("INPUTMAP0002", "error", "Duplicate action ID: " + action.Id, action.Id));
        var bindings = new HashSet<string>(StringComparer.Ordinal);
        foreach (var binding in map.Bindings.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            if (!bindings.Add(binding.Id)) diagnostics.Add(new("INPUTMAP0002", "error", "Duplicate binding ID: " + binding.Id, binding.Id));
            if (!BindingKinds.Contains(binding.Kind)) diagnostics.Add(new("INPUTMAP0003", "error", "Unsupported binding kind: " + binding.Kind, binding.Id));
            if (!actions.TryGetValue(binding.ActionId, out var action)) { diagnostics.Add(new("INPUTMAP0004", "error", "Binding references unknown action.", binding.Id)); continue; }
            if (!double.IsFinite(binding.Scale)) diagnostics.Add(new("INPUTMAP0006", "error", "Binding scale must be finite.", binding.Id));
            if (!double.IsFinite(binding.DeadZone) || binding.DeadZone < 0 || binding.DeadZone >= 1) diagnostics.Add(new("INPUTMAP0005", "error", "Dead zone must be finite and in [0,1).", binding.Id));
            if (!Compatible(action.ValueKind, binding.Kind)) diagnostics.Add(new("INPUTMAP0004", "error", "Binding type is incompatible with action value type.", binding.Id));
            if (!ValidControl(binding)) diagnostics.Add(new("INPUTMAP0007", "error", "Unknown or backend-specific semantic control.", binding.Id));
            if (binding.Kind == "composite-vector2" && (binding.Composite is null || !new[] { "up", "down", "left", "right" }.All(binding.Composite.ContainsKey))) diagnostics.Add(new("INPUTMAP0003", "error", "Composite vector2 requires up/down/left/right semantic controls.", binding.Id));
        }
        return new(map with { Actions = map.Actions.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray(), Bindings = map.Bindings.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray() }, diagnostics);
    }
    private static bool Compatible(ActionValueKind type, string kind) => type switch { ActionValueKind.Digital => kind is "keyboard-key" or "mouse-button" or "controller-button", ActionValueKind.Scalar => kind is "mouse-wheel-x" or "mouse-wheel-y" or "controller-axis", ActionValueKind.Vector2 => kind is "controller-stick" or "composite-vector2", _ => false };
    private static bool ValidControl(InputBinding binding)
    {
        if (binding.Kind == "composite-vector2") return binding.Composite!.Values.All(Keyboard);
        if (string.IsNullOrWhiteSpace(binding.Control) || binding.Control.Contains("KEY_", StringComparison.OrdinalIgnoreCase) || binding.Control.Contains("MOUSE_", StringComparison.OrdinalIgnoreCase) || binding.Control.Contains("GAMEPAD_", StringComparison.OrdinalIgnoreCase)) return false;
        return binding.Kind switch { "keyboard-key" => Keyboard(binding.Control), "mouse-button" => MouseControls.Contains(binding.Control), "mouse-wheel-x" or "mouse-wheel-y" => binding.Control is "wheel-x" or "wheel-y", "controller-button" => ControllerButtons.Contains(binding.Control), "controller-axis" => ControllerAxes.Contains(binding.Control), "controller-stick" => binding.Control is "left-stick" or "right-stick", _ => true };
    }
    private static bool Keyboard(string control) => control.Length is > 0 and <= 12 && control.All(c => char.IsLetterOrDigit(c) || c is '-' or '_');
}

/// <summary>Pure binding evaluator. It only transforms accumulated raw state; it has no runtime dependency.</summary>
public static class InputMapper
{
    public static InputFrame Resolve(InputMap map, int tick, long frameSequence, AccumulatedInput state, IReadOnlyDictionary<string, DigitalPhase> previous, ViewportTransform? viewport = null)
    {
        var validation = InputMapValidator.Validate(map);
        var digital = new Dictionary<string, DigitalActionValue>(StringComparer.Ordinal); var scalars = new Dictionary<string, ScalarActionValue>(StringComparer.Ordinal); var vectors = new Dictionary<string, Vector2ActionValue>(StringComparer.Ordinal);
        foreach (var action in map.Actions.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            var bindings = map.Bindings.Where(x => x.ActionId == action.Id).OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            if (action.ValueKind == ActionValueKind.Digital)
            {
                var down = bindings.Any(x => state.Down.Contains(x.Control!)); var phase = Phase(down, previous.TryGetValue(action.Id, out var old) ? old : DigitalPhase.Inactive, bindings.Any(x => state.Pressed.Contains(x.Control!)), bindings.Any(x => state.Released.Contains(x.Control!)));
                digital[action.Id] = new(down ? 1 : 0, phase);
            }
            else if (action.ValueKind == ActionValueKind.Scalar)
            {
                var best = (Value: 0d, Id: string.Empty);
                foreach (var b in bindings) { var value = b.Kind.StartsWith("mouse-wheel", StringComparison.Ordinal) ? (b.Kind == "mouse-wheel-x" ? state.WheelX : state.WheelY) : state.Scalar(b.Control!); value = Axial(value, b.DeadZone) * b.Scale; if (double.IsFinite(value) && (Math.Abs(value) > Math.Abs(best.Value) || Math.Abs(value) == Math.Abs(best.Value) && string.CompareOrdinal(b.Id, best.Id) < 0)) best = (value, b.Id); }
                scalars[action.Id] = new(Math.Clamp(best.Value, -1, 1));
            }
            else
            {
                var total = Vector2.Zero;
                foreach (var b in bindings) { Vector2 v; if (b.Kind == "controller-stick") v = Radial(state.Stick(b.Control!), b.DeadZone); else { var c = b.Composite!; v = new((state.Down.Contains(c["right"]) ? 1 : 0) - (state.Down.Contains(c["left"]) ? 1 : 0), (state.Down.Contains(c["down"]) ? 1 : 0) - (state.Down.Contains(c["up"]) ? 1 : 0)); } total += v * (float)b.Scale; }
                if (total.LengthSquared() > 1) total = Vector2.Normalize(total); vectors[action.Id] = new(total.X, total.Y);
            }
        }
        var pointer = state.Pointer with { Id = InputIds.PrimaryPointer, InputSourceId = map.InputSourceId, DeltaX = state.PointerDeltaX, DeltaY = state.PointerDeltaY, WheelX = state.WheelX, WheelY = state.WheelY };
        if (viewport is not null) pointer = viewport.Convert(pointer);
        return new("agentic2d.input-frame.v1", tick, frameSequence, map.InputSourceId, map.Id, map.Revision, digital, scalars, vectors, new Dictionary<string, PointerState> { [InputIds.PrimaryPointer] = pointer }, state.Sequences.Order().ToArray(), validation.Diagnostics);
    }
    private static DigitalPhase Phase(bool down, DigitalPhase previous, bool pressed, bool released) => pressed ? DigitalPhase.Pressed : released && !down ? DigitalPhase.Released : down ? DigitalPhase.Held : DigitalPhase.Inactive;
    public static double Axial(double value, double dz) { if (!double.IsFinite(value)) throw new ArgumentException("INPUT0001 non-finite scalar input"); var magnitude = Math.Abs(value); return magnitude < dz ? 0 : Math.Sign(value) * (magnitude - dz) / (1 - dz); }
    public static Vector2 Radial(Vector2 value, double dz) { if (!float.IsFinite(value.X) || !float.IsFinite(value.Y)) throw new ArgumentException("INPUT0001 non-finite vector input"); var magnitude = value.Length(); if (magnitude <= dz + 0.0000001d || magnitude == 0) return Vector2.Zero; return Vector2.Normalize(value) * (float)((magnitude - dz) / (1 - dz)); }
}

public sealed class AccumulatedInput
{
    public HashSet<string> Down { get; } = new(StringComparer.Ordinal); public HashSet<string> Pressed { get; } = new(StringComparer.Ordinal); public HashSet<string> Released { get; } = new(StringComparer.Ordinal); public Dictionary<string, double> Analogs { get; } = new(StringComparer.Ordinal); public Dictionary<string, Vector2> Sticks { get; } = new(StringComparer.Ordinal); public List<long> Sequences { get; } = []; public PointerState Pointer { get; set; } = new(InputIds.PrimaryPointer, InputIds.PlayerOneSource, "device.synthetic.mouse", 0, 0, 0, 0, 0, 0, PointerSpace.Window, true); public double PointerDeltaX { get; set; }
    public double PointerDeltaY { get; set; }
    public double WheelX { get; set; }
    public double WheelY { get; set; }
    public double Scalar(string control) => Analogs.TryGetValue(control, out var value) ? value : 0; public Vector2 Stick(string control) => Sticks.TryGetValue(control, out var value) ? value : Vector2.Zero;
    public void ClearOneShots() { Pressed.Clear(); Released.Clear(); Sequences.Clear(); PointerDeltaX = PointerDeltaY = WheelX = WheelY = 0; }
    public void ClearAll() { Down.Clear(); Analogs.Clear(); Sticks.Clear(); ClearOneShots(); Pointer = Pointer with { X = 0, Y = 0, DeltaX = 0, DeltaY = 0, WheelX = 0, WheelY = 0 }; }
}

public sealed class InputAccumulator
{
    private readonly AccumulatedInput state = new(); private readonly Dictionary<string, DigitalPhase> previous = new(StringComparer.Ordinal); private long sequence;
    public void Sample(RawInputSample sample)
    {
        if (sample.InputSourceId != InputIds.PlayerOneSource) throw new ArgumentException("INPUT0002 unsupported input source"); if (!double.IsFinite(sample.Value) || sample.X is double x && !double.IsFinite(x) || sample.Y is double y && !double.IsFinite(y)) throw new ArgumentException("INPUT0001 non-finite raw input");
        state.Sequences.Add(sample.Sequence); sequence = Math.Max(sequence, sample.Sequence);
        if (sample.Control is "pointer.primary") { var oldX = state.Pointer.X; var oldY = state.Pointer.Y; state.Pointer = state.Pointer with { PhysicalDeviceId = sample.PhysicalDeviceId, X = sample.X ?? oldX, Y = sample.Y ?? oldY, Space = sample.Space }; state.PointerDeltaX += state.Pointer.X - oldX; state.PointerDeltaY += state.Pointer.Y - oldY; return; }
        if (sample.Control is "wheel-x") { state.WheelX += sample.Value; return; }
        if (sample.Control is "wheel-y") { state.WheelY += sample.Value; return; }
        if (sample.Control.EndsWith("-stick", StringComparison.Ordinal)) { state.Sticks[sample.Control] = new((float)(sample.X ?? 0), (float)(sample.Y ?? 0)); return; }
        if (sample.DeviceKind is InputDeviceKind.Keyboard or InputDeviceKind.Mouse or InputDeviceKind.Controller && (sample.Value == 0 || sample.Value == 1) && !sample.Control.Contains("-axis", StringComparison.Ordinal)) { if (sample.Value > 0) { if (state.Down.Add(sample.Control)) state.Pressed.Add(sample.Control); } else { if (state.Down.Remove(sample.Control)) state.Released.Add(sample.Control); } return; }
        state.Analogs[sample.Control] = sample.Value;
    }
    public InputFrame Consume(InputMap map, int tick, ViewportTransform? viewport = null) { var frame = InputMapper.Resolve(map, tick, ++sequence, state, previous, viewport); foreach (var value in frame.DigitalActions) previous[value.Key] = value.Value.Phase; state.ClearOneShots(); return frame; }
    public void Reset() { state.ClearAll(); previous.Clear(); sequence = 0; }
}

public sealed class InputFrameRecorder
{
    private readonly List<InputFrame> frames = []; public IReadOnlyList<InputFrame> Frames => frames; public void Record(InputFrame frame) => frames.Add(frame); public void Reset() => frames.Clear();
    public InputRecording Export(InputReplayCompatibility compatibility) => new("agentic2d.input-recording.v1", compatibility, frames.OrderBy(x => x.Tick).ThenBy(x => x.FrameSequence).ToArray());
}
public sealed record InputReplayCompatibility(string ScenarioId, string ScenarioFingerprint, string InputMapId, string InputMapRevision, string RuntimeVersion, string ContentVersion, string Seed, int InitialTick);
public sealed record InputReplayEvidence(string ConsumedFramesFingerprint, string IntentsFingerprint, string MovementResolutionsFingerprint, string InteractionResolutionsFingerprint, string CommandsFingerprint, string EventsFingerprint, string FinalStateFingerprint, string AssertionsFingerprint, string RenderProjectionFingerprint);
public sealed record InputRecording(string Schema, InputReplayCompatibility Compatibility, IReadOnlyList<InputFrame> Frames, InputReplayEvidence? Evidence = null);
public sealed record InputReplayResult(bool Accepted, IReadOnlyList<string> Mismatches, IReadOnlyList<string>? ComparedCategories = null);
public static class SemanticReplay
{
    public static InputReplayResult CheckCompatibility(InputReplayCompatibility expected, InputRecording recording)
    {
        var actual = recording.Compatibility; var mismatches = new List<string>(); if (expected.ScenarioId != actual.ScenarioId) mismatches.Add("scenario-id"); if (expected.ScenarioFingerprint != actual.ScenarioFingerprint) mismatches.Add("scenario-fingerprint"); if (expected.InputMapId != actual.InputMapId || expected.InputMapRevision != actual.InputMapRevision) mismatches.Add("input-map"); if (expected.RuntimeVersion != actual.RuntimeVersion || expected.ContentVersion != actual.ContentVersion) mismatches.Add("content-runtime-version"); if (expected.Seed != actual.Seed || expected.InitialTick != actual.InitialTick) mismatches.Add("tick-seed"); return new(mismatches.Count == 0, mismatches);
    }
    public static IReadOnlyList<string> CompareEvidence(InputReplayEvidence? expected, InputReplayEvidence? actual)
    {
        if (expected is null || actual is null) return ["recording-evidence"];
        var mismatches = new List<string>();
        if (expected.ConsumedFramesFingerprint != actual.ConsumedFramesFingerprint) mismatches.Add("consumed-input-frames");
        if (expected.IntentsFingerprint != actual.IntentsFingerprint) mismatches.Add("behavior-intents");
        if (expected.MovementResolutionsFingerprint != actual.MovementResolutionsFingerprint) mismatches.Add("movement-resolutions");
        if (expected.InteractionResolutionsFingerprint != actual.InteractionResolutionsFingerprint) mismatches.Add("interaction-resolutions");
        if (expected.CommandsFingerprint != actual.CommandsFingerprint) mismatches.Add("commands");
        if (expected.EventsFingerprint != actual.EventsFingerprint) mismatches.Add("events");
        if (expected.FinalStateFingerprint != actual.FinalStateFingerprint) mismatches.Add("final-component-state");
        if (expected.AssertionsFingerprint != actual.AssertionsFingerprint) mismatches.Add("assertions");
        if (expected.RenderProjectionFingerprint != actual.RenderProjectionFingerprint) mismatches.Add("render-projection");
        return mismatches;
    }
    public static string Fingerprint(object value) => "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, InputMap.JsonOptions)))).ToLowerInvariant();
}
