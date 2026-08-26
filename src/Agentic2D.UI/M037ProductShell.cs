using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Agentic2D.UI;

public enum UiInputKind { Pointer, Key, Text, Confirm, Cancel, Scroll }
public sealed record UiInput(UiInputKind Kind, string? Value = null, int X = 0, int Y = 0);
public sealed record UiSize(int Width, int Height);
public sealed record UiRect(int X, int Y, int Width, int Height)
{
    public bool Contains(int x, int y) => x >= X && y >= Y && x < X + Width && y < Y + Height;
}
public abstract class UiNode : IDisposable
{
    private readonly List<UiNode> children = [];
    public string Id { get; }
    public UiNode? Parent { get; private set; }
    public IReadOnlyList<UiNode> Children => children;
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public bool Focusable { get; init; }
    public UiRect Bounds { get; private set; } = new(0, 0, 0, 0);
    public bool IsFocused { get; internal set; }
    protected UiNode(string id, bool focusable = false) { Id = Require(id); Focusable = focusable; }
    public void Add(UiNode child) { ArgumentNullException.ThrowIfNull(child); if (child.Parent is not null) throw new InvalidOperationException("UI node already has a parent."); child.Parent = this; children.Add(child); }
    public void Remove(UiNode child) { if (children.Remove(child)) { child.Parent = null; child.Dispose(); } }
    public IEnumerable<UiNode> Descendants() => children.SelectMany(x => new[] { x }.Concat(x.Descendants()));
    public virtual UiSize Measure(UiSize available) => available;
    internal void Arrange(UiRect bounds) { Bounds = bounds; ArrangeChildren(bounds); }
    protected virtual void ArrangeChildren(UiRect bounds) { foreach (var child in children.Where(x => x.Visible)) child.Arrange(bounds); }
    public virtual bool Handle(UiInput input) => false;
    public virtual void Dispose() { foreach (var child in children.ToArray()) child.Dispose(); children.Clear(); Parent = null; IsFocused = false; }
    protected static string Require(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Stable UI ID is required.") : value;
}
public abstract class UiControl(string id, bool focusable = true) : UiNode(id, focusable)
{
    public event Action<UiControl>? Activated;
    protected void Activate() => Activated?.Invoke(this);
    public override bool Handle(UiInput input) { if (!Enabled || !Visible) return false; if (input.Kind is UiInputKind.Confirm || input.Kind == UiInputKind.Pointer && Bounds.Contains(input.X, input.Y)) { Activate(); return true; } return false; }
}
public sealed class UiLabel(string id, string text = "") : UiControl(id, false) { public string Text { get; set; } = text; }
public sealed class UiButton(string id, string text) : UiControl(id) { public string Text { get; set; } = text; }
public sealed class UiToggle(string id, bool value = false) : UiControl(id) { public bool Value { get; private set; } = value; public override bool Handle(UiInput input) { if (!base.Handle(input)) return false; Value = !Value; return true; } }
public sealed class UiSlider(string id, double value = 0) : UiControl(id) { public double Value { get; private set; } = Math.Clamp(value, 0, 1); public void Set(double value) => Value = Math.Clamp(value, 0, 1); }
public sealed class UiTextField(string id, string value = "") : UiControl(id) { public string Value { get; private set; } = value; public bool CapturesText => true; public void Enter(string value) => Value = value; public override bool Handle(UiInput input) { if (input.Kind == UiInputKind.Text && Enabled && Visible) { Value += input.Value; return true; } return base.Handle(input); } }
public sealed class UiSelect(string id, IReadOnlyList<string> options) : UiControl(id) { public IReadOnlyList<string> Options { get; } = options; public int SelectedIndex { get; private set; } public string Selected => Options.Count == 0 ? string.Empty : Options[SelectedIndex]; public void Select(int index) => SelectedIndex = index is >= 0 and < 100000 && index < Options.Count ? index : throw new ArgumentOutOfRangeException(nameof(index)); }
public sealed class UiList(string id) : UiControl(id) { public IReadOnlyList<string> Items { get; private set; } = []; public int SelectedIndex { get; private set; } = -1; public void SetItems(IEnumerable<string> items) => Items = items.ToArray(); public void Select(int index) => SelectedIndex = index; }
public sealed class UiScrollView(string id) : UiContainer(id) { public int ScrollOffset { get; private set; } public void Scroll(int delta) => ScrollOffset = Math.Max(0, ScrollOffset + delta); }
public sealed class UiSeparator(string id) : UiNode(id);
public sealed class UiBusyIndicator(string id) : UiControl(id, false) { public bool Busy { get; set; } }
public class UiContainer(string id) : UiNode(id);
public sealed class UiRow(string id) : UiContainer(id);
public sealed class UiColumn(string id) : UiContainer(id);
public sealed class UiGrid(string id) : UiContainer(id);
public sealed class UiMargin(string id) : UiContainer(id);
public sealed class UiScreen(string id) : UiContainer(id);
public sealed class UiModal(string id, string title, string message) : UiContainer(id) { public string Title { get; } = title; public string Message { get; } = message; public bool IsOpen { get; private set; } public void Open() => IsOpen = true; public void Close() => IsOpen = false; }

public sealed class UiHost(UiScreen screen)
{
    public UiScreen Screen { get; private set; } = screen;
    public UiModal? Modal { get; private set; }
    public UiNode? Focused { get; private set; }
    public int CallbackCount { get; private set; }
    public void Replace(UiScreen screen) { Screen.Dispose(); Screen = screen; Modal = null; Focused = null; CallbackCount = 0; }
    public void ShowModal(UiModal modal) { Modal?.Dispose(); Modal = modal; modal.Open(); Focus(modal.Descendants().FirstOrDefault(x => x.Focusable)); }
    public void Focus(UiNode? node) { if (node is not null && (!node.Visible || !node.Enabled || !node.Focusable)) return; if (Focused is not null) Focused.IsFocused = false; Focused = node; if (node is not null) node.IsFocused = true; }
    public bool Dispatch(UiInput input)
    {
        if (Modal is { IsOpen: true } modal && DispatchTo(modal, input)) return true;
        if (Focused is UiTextField text && input.Kind is UiInputKind.Text) return text.Handle(input);
        if (Focused is not null && DispatchTo(Focused, input)) return true;
        return DispatchTo(Screen, input);
    }
    private bool DispatchTo(UiNode node, UiInput input) => node.Visible && node.Enabled && (node.Handle(input) || node.Children.Reverse().Any(child => DispatchTo(child, input)));
    public IReadOnlyList<UiLayoutRecord> Project(int width, int height) { Screen.Arrange(new(0, 0, width, height)); return ProjectTree(Screen).ToArray(); }
    private static IEnumerable<UiLayoutRecord> ProjectTree(UiNode n) { yield return new(n.Id, n.GetType().Name, n.Bounds.X, n.Bounds.Y, n.Bounds.Width, n.Bounds.Height, 0, n.Visible, Fingerprint(n.Id + n.Bounds)); foreach (var child in n.Children.SelectMany(ProjectTree)) yield return child; }
    private static string Fingerprint(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public enum ApplicationState { Starting, MainMenu, LoadingWorld, Playing, Paused, Saving, UnloadingWorld, ShuttingDown, FailedRecoverable }
public enum ClientComposition { Player, Diagnostics }
public sealed record MenuItem(string Id, string Label, bool Available = true);
public static class ProductMenus
{
    public static IReadOnlyList<MenuItem> Main(bool continueAvailable) => [new("continue", "Continue", continueAvailable), new("new-game", "New Game"), new("load-game", "Load Game"), new("tutorial", "Tutorial"), new("options", "Options"), new("credits", "Credits"), new("quit", "Quit")];
    public static IReadOnlyList<MenuItem> Pause() => [new("resume", "Resume"), new("save", "Save"), new("load", "Load"), new("options", "Options"), new("return-main-menu", "Return to Main Menu"), new("quit", "Quit")];
}
public sealed class ApplicationShell
{
    public ApplicationState State { get; private set; } = ApplicationState.Starting;
    public ClientComposition Composition { get; }
    public bool WorldActive { get; private set; }
    public List<string> TransitionLog { get; } = [];
    public ApplicationShell(ClientComposition composition = ClientComposition.Player) => Composition = composition;
    public void Start() => Transition(ApplicationState.MainMenu);
    public void OpenWorld() { Transition(ApplicationState.LoadingWorld); WorldActive = true; Transition(ApplicationState.Playing); }
    public void Pause() { if (!WorldActive) throw new InvalidOperationException("No world is active."); Transition(ApplicationState.Paused); }
    public void RequestDestructive(string operation) { if (WorldActive) TransitionLog.Add("confirm:save-and-" + operation + "/without-saving/cancel"); else TransitionLog.Add(operation); }
    public void UnloadWorld() { WorldActive = false; Transition(ApplicationState.UnloadingWorld); Transition(ApplicationState.MainMenu); }
    public void Shutdown() { WorldActive = false; Transition(ApplicationState.ShuttingDown); }
    private void Transition(ApplicationState next) { State = next; TransitionLog.Add(next.ToString()); }
}
public sealed record WorldConfiguration(string Id, int SchemaVersion, string DisplayName, string Description, IReadOnlyDictionary<string, int> Values, string Fingerprint, bool TutorialGuidanceEnabled = false);
public static class WorldConfigurations
{
    public static IReadOnlyList<WorldConfiguration> Bundled { get; } = [Create("relaxed", 90), Create("standard", 100), Create("demanding", 120), Create("stress-test", 150)];
    public static WorldConfiguration Tutorial => Bundled.Single(x => x.Id == "standard") with { TutorialGuidanceEnabled = true };
    public static WorldConfiguration Find(string id) => Bundled.SingleOrDefault(x => x.Id == id) ?? throw new InvalidOperationException("WORLD0301: unknown world configuration " + id);
    public static WorldConfiguration Validate(WorldConfiguration config) { if (config.SchemaVersion != 1 || string.IsNullOrWhiteSpace(config.Id) || config.Values.Any(x => x.Value < 0)) throw new InvalidOperationException("WORLD0302: invalid world configuration"); return config; }
    private static WorldConfiguration Create(string id, int value) { var values = new Dictionary<string, int> { ["starting-population"] = 4, ["resource-abundance"] = value }; return new(id, 1, id, "Endless settlement configuration", values, Fingerprint(id + value)); }
    private static string Fingerprint(string value) => "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
public sealed record NewGameRequest(string ConfigurationId, string? Seed, string WorldTitle);
public sealed record WorldSession(string WorldId, string WorldTitle, string Seed, WorldConfiguration Configuration, bool IsTutorial);
public static class NewGameFactory
{
    public static WorldSession Create(NewGameRequest request) { var config = WorldConfigurations.Validate(WorldConfigurations.Find(request.ConfigurationId)); var seed = string.IsNullOrWhiteSpace(request.Seed) ? "seed." + Guid.NewGuid().ToString("N") : request.Seed.Trim(); if (seed.Length > 128 || seed.Any(char.IsWhiteSpace)) throw new InvalidOperationException("WORLD0303: invalid seed"); if (string.IsNullOrWhiteSpace(request.WorldTitle)) throw new InvalidOperationException("WORLD0304: world title is required"); return new("world." + Guid.NewGuid().ToString("N"), request.WorldTitle.Trim(), seed, config, false); }
    public static WorldSession Tutorial() => new("world.tutorial." + Guid.NewGuid().ToString("N"), "Tutorial", "tutorial.fixed.v1", WorldConfigurations.Tutorial, true);
    public static WorldSession FromSave(SaveRecord save) { if (save.Health is not (SaveHealth.Valid or SaveHealth.Recoverable)) throw new InvalidOperationException("SAVE0306: save is not loadable"); var config = WorldConfigurations.Validate(WorldConfigurations.Find(save.WorldConfigurationId)); if (config.Fingerprint != save.WorldConfigurationFingerprint) throw new InvalidOperationException("SAVE0307: saved world configuration fingerprint differs"); return new(save.WorldId, save.WorldTitle, save.Seed, config, save.Seed == "tutorial.fixed.v1"); }
}

public enum SaveType { Manual, Autosave }
public enum SaveHealth { Valid, Recoverable, Incompatible, Corrupt }
public sealed record SaveRecord(string SaveId, string WorldId, string WorldTitle, string SaveTitle, string Seed, string WorldConfigurationId, string WorldConfigurationFingerprint, int SimulationDay, int Population, DateTimeOffset CreatedAt, DateTimeOffset SavedAt, SaveType Type, string GameVersion, int SaveSchema, SaveHealth Health = SaveHealth.Valid, string? CanonicalSavePath = null)
{
    public static string ManualTitle(string worldTitle, int day) => $"{worldTitle} — Day {day}";
    public static string AutosaveTitle(string worldTitle, int day) => $"Autosave — {worldTitle} — Day {day}";
}
public sealed record ContinueResolution(SaveRecord? Save, IReadOnlyList<string> Notices);
public sealed class SaveCatalog
{
    private readonly List<SaveRecord> saves = [];
    public SaveCatalog() { }
    public SaveCatalog(IEnumerable<SaveRecord> records) => saves.AddRange(records);
    public IReadOnlyList<SaveRecord> Saves => saves.OrderByDescending(x => x.SavedAt).ThenBy(x => x.SaveId, StringComparer.Ordinal).ToArray();
    public SaveRecord Find(string saveId) => saves.SingleOrDefault(x => x.SaveId == saveId) ?? throw new InvalidOperationException("SAVE0305: save not found");
    public SaveRecord AddManual(WorldSession world, int day, int population, DateTimeOffset now, string? title = null) => Add(world, day, population, now, SaveType.Manual, title ?? SaveRecord.ManualTitle(world.WorldTitle, day));
    public SaveRecord AddAutosave(WorldSession world, int day, int population, DateTimeOffset now) => Add(world, day, population, now, SaveType.Autosave, SaveRecord.AutosaveTitle(world.WorldTitle, day));
    public SaveRecord Rename(string saveId, string title) { var index = saves.FindIndex(x => x.SaveId == saveId); if (index < 0 || string.IsNullOrWhiteSpace(title)) throw new InvalidOperationException("SAVE0301: save not found or title is empty"); var result = saves[index] with { SaveTitle = title.Trim() }; saves[index] = result; return result; }
    public SaveRecord LinkCanonicalSave(string saveId, string canonicalSavePath) { var index = saves.FindIndex(x => x.SaveId == saveId); if (index < 0 || string.IsNullOrWhiteSpace(canonicalSavePath) || Path.IsPathRooted(canonicalSavePath)) throw new InvalidOperationException("SAVE0436: canonical save reference is invalid"); var result = saves[index] with { CanonicalSavePath = canonicalSavePath.Trim() }; saves[index] = result; return result; }
    public void Delete(string saveId) { if (!saves.RemoveAll(x => x.SaveId == saveId).Equals(1)) throw new InvalidOperationException("SAVE0302: save not found"); }
    public ContinueResolution ResolveContinue() { var notices = new List<string>(); foreach (var save in Saves) { if (save.Health == SaveHealth.Valid || save.Health == SaveHealth.Recoverable) return new(save, notices); notices.Add("Skipped " + save.SaveTitle + " (" + save.Health.ToString().ToLowerInvariant() + ")"); } return new(null, notices); }
    public int RetainAutosaves(string worldId, int retention) { if (retention is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(retention)); var old = saves.Where(x => x.WorldId == worldId && x.Type == SaveType.Autosave).OrderByDescending(x => x.SavedAt).ThenBy(x => x.SaveId, StringComparer.Ordinal).Skip(retention).ToArray(); foreach (var save in old) saves.Remove(save); return old.Length; }
    private SaveRecord Add(WorldSession world, int day, int population, DateTimeOffset now, SaveType type, string title) { var result = new SaveRecord("save." + Guid.NewGuid().ToString("N"), world.WorldId, world.WorldTitle, title, world.Seed, world.Configuration.Id, world.Configuration.Fingerprint, day, population, now, now, type, "0.7.2", 1); saves.Add(result); return result; }
}
public sealed class SaveCatalogStore
{
    private readonly string path;
    public SaveCatalogStore(string path) => this.path = path;
    public SaveCatalog Load()
    {
        if (!File.Exists(path)) return new();
        try
        {
            var envelope = JsonSerializer.Deserialize<SaveCatalogEnvelope>(File.ReadAllText(path)) ?? throw new InvalidOperationException();
            if (envelope.Schema != "agentic2d.save-catalog.v1") throw new InvalidOperationException();
            return new(envelope.Saves);
        }
        catch
        {
            File.Copy(path, path + ".rejected", true);
            return new();
        }
    }
    public void Save(SaveCatalog catalog)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(new SaveCatalogEnvelope("agentic2d.save-catalog.v1", 1, catalog.Saves), new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, path, true);
    }
    private sealed record SaveCatalogEnvelope(string Schema, int SchemaVersion, IReadOnlyList<SaveRecord> Saves);
}
public sealed class AutosaveScheduler
{
    public bool Enabled { get; set; } = true; public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(10); public int Retention { get; set; } = 5; public TimeSpan ActiveElapsed { get; private set; }
    public bool Pending { get; private set; }
    public bool SaveActive { get; set; }
    public DateTimeOffset? LastSuccess { get; private set; }
    public void Tick(TimeSpan elapsed, bool worldLoaded, bool applicationActive, bool validSaveBoundary) { if (!Enabled || !worldLoaded || !applicationActive) return; ActiveElapsed += elapsed; if (ActiveElapsed >= Interval) Pending = true; if (Pending && validSaveBoundary && !SaveActive) { SaveActive = true; } }
    public void Complete(bool success, DateTimeOffset now) { if (!SaveActive) throw new InvalidOperationException("SAVE0303: no autosave is active"); SaveActive = false; if (success) { Pending = false; ActiveElapsed = TimeSpan.Zero; LastSuccess = now; } }
}

public sealed record UserSettings(int SchemaVersion = 1, double MasterVolume = 1, double MusicVolume = 1, double EffectsVolume = 1, bool Muted = false, string DisplayMode = "windowed", string Resolution = "native", double UiScale = 1, bool AutosaveEnabled = true, int AutosaveIntervalMinutes = 10, int AutosaveRetention = 5, bool PauseOnFocusLoss = true, bool PauseOnCriticalAlert = false, bool EdgeScroll = true, bool ReducedMotion = false)
{
    public static UserSettings Defaults => new();
    public UserSettings Validate() { if (SchemaVersion != 1 || new[] { MasterVolume, MusicVolume, EffectsVolume, UiScale }.Any(x => x is < 0 or > 2) || DisplayMode is not ("windowed" or "borderless-windowed" or "fullscreen") || AutosaveIntervalMinutes is < 1 or > 120 || AutosaveRetention is < 1 or > 10) throw new InvalidOperationException("SET0301: invalid user settings"); return this; }
}
public sealed class UserSettingsStore
{
    private readonly string path;
    public UserSettings Current { get; private set; } = UserSettings.Defaults;
    public UserSettingsStore(string path) => this.path = path;
    public UserSettings Load(bool safeMode = false, bool reset = false) { if (safeMode || reset) return Current = UserSettings.Defaults; try { if (!File.Exists(path)) return Current; var loaded = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(path))?.Validate() ?? throw new InvalidOperationException(); return Current = loaded; } catch { if (File.Exists(path)) File.Copy(path, path + ".rejected", true); return Current = UserSettings.Defaults; } }
    public void Save(UserSettings settings) { Current = settings.Validate(); Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!); var temp = path + ".tmp"; File.WriteAllText(temp, JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true })); File.Move(temp, path, true); }
}

public sealed record ShellProjection(ApplicationState State, IReadOnlyList<MenuItem> Menu, WorldSession? World, IReadOnlyList<SaveRecord> Saves, UserSettings Settings, string? Notice);
public sealed class ProductShellSession : IDisposable
{
    private readonly SaveCatalogStore catalogStore;
    private readonly UserSettingsStore settingsStore;
    public ApplicationShell Shell { get; } = new();
    public SaveCatalog Catalog { get; private set; }
    public UserSettings Settings => settingsStore.Current;
    public WorldSession? World { get; private set; }
    public string? Notice { get; private set; }
    public bool Started { get; private set; }
    public ProductShellSession(string catalogPath, string settingsPath)
    {
        catalogStore = new(catalogPath);
        settingsStore = new(settingsPath);
        Catalog = catalogStore.Load();
    }
    public ShellProjection Start(bool safeMode = false, bool resetUserSettings = false)
    {
        settingsStore.Load(safeMode, resetUserSettings);
        Shell.Start();
        Started = true;
        return Project();
    }
    public ShellProjection StartNewGame(NewGameRequest request) { EnsureStarted(); ReplaceWorld(NewGameFactory.Create(request)); return Project(); }
    public ShellProjection StartTutorial() { EnsureStarted(); ReplaceWorld(NewGameFactory.Tutorial()); return Project(); }
    public SaveRecord SaveManual(int simulationDay, int population, DateTimeOffset now, string? title = null) { EnsureWorld(); var save = Catalog.AddManual(World!, simulationDay, population, now, title); catalogStore.Save(Catalog); return save; }
    public SaveRecord SaveAutosave(int simulationDay, int population, DateTimeOffset now) { EnsureWorld(); var save = Catalog.AddAutosave(World!, simulationDay, population, now); Catalog.RetainAutosaves(World!.WorldId, Settings.AutosaveRetention); catalogStore.Save(Catalog); return save; }
    public ShellProjection Load(string saveId) { EnsureStarted(); var save = Catalog.Find(saveId); ReplaceWorld(NewGameFactory.FromSave(save)); Notice = null; return Project(); }
    public ShellProjection Continue() { EnsureStarted(); var result = Catalog.ResolveContinue(); Notice = result.Notices.Count == 0 ? null : string.Join("; ", result.Notices); if (result.Save is not null) { try { return Load(result.Save.SaveId); } catch (InvalidOperationException exception) { Notice = "Continue could not load the newest save: " + exception.Message; } } return Project(); }
    public ShellProjection RequestDestructive(string operation) { EnsureStarted(); Shell.RequestDestructive(operation); return Project(); }
    public ShellProjection Project() => new(Shell.State, Shell.State == ApplicationState.Paused ? ProductMenus.Pause() : ProductMenus.Main(Catalog.ResolveContinue().Save is not null), World, Catalog.Saves, Settings, Notice);
    private void ReplaceWorld(WorldSession world) { World = world; Shell.OpenWorld(); }
    private void EnsureStarted() { if (!Started) throw new InvalidOperationException("APP0301: shell has not started"); }
    private void EnsureWorld() { EnsureStarted(); if (World is null) throw new InvalidOperationException("SAVE0304: no world is active"); }
    public void Dispose() { World = null; if (Shell.WorldActive) Shell.UnloadWorld(); }
}
public sealed record DisplayCandidate(string Mode, string Resolution, double UiScale);
public sealed class DisplayPreview
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15); public DisplayCandidate? PreviousKnownGood { get; private set; }
    public DisplayCandidate? Candidate { get; private set; }
    public TimeSpan Remaining { get; private set; }
    public void Begin(DisplayCandidate current, DisplayCandidate candidate, TimeSpan? timeout = null) { PreviousKnownGood = current; Candidate = candidate; Remaining = timeout ?? DefaultTimeout; }
    public bool Tick(TimeSpan elapsed) { if (Candidate is null) return false; Remaining -= elapsed; return Remaining <= TimeSpan.Zero; }
    public DisplayCandidate Keep() { var value = Candidate ?? throw new InvalidOperationException("SET0302: no display preview"); PreviousKnownGood = value; Candidate = null; return value; }
    public DisplayCandidate Revert() { var value = PreviousKnownGood ?? throw new InvalidOperationException("SET0303: no known-good display"); Candidate = null; return value; }
}

public enum BindingActionType { Button, Axis, Value }
public enum BindingInputClass { Keyboard, Mouse, Modifier }
public sealed record BindableAction(string Id, string DisplayKey, string Category, string Description, BindingActionType ActionType, IReadOnlySet<BindingInputClass> AllowedInputClasses, IReadOnlyList<string> Defaults, bool Rebindable, bool AllowMultiple, string ConflictPolicy, string Context, bool Recoverable);
public sealed class BindingRegistry
{
    private readonly Dictionary<string, BindableAction> actions = new(StringComparer.Ordinal); private readonly Dictionary<string, List<string>> overrides = new(StringComparer.Ordinal);
    public IReadOnlyCollection<BindableAction> Actions => actions.Values.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
    public void Register(BindableAction action) { if (!actions.TryAdd(action.Id, action)) throw new InvalidOperationException("INPUT0301: duplicate action " + action.Id); }
    public IReadOnlyList<string> Effective(string id) => actions.TryGetValue(id, out var action) ? (overrides.TryGetValue(id, out var value) ? value : action.Defaults) : throw new InvalidOperationException("INPUT0302: unknown action " + id);
    public void Rebind(string id, string binding, bool replace = false) { if (!actions.TryGetValue(id, out var action) || !action.Rebindable) throw new InvalidOperationException("INPUT0303: action cannot be rebound"); var conflict = actions.Where(x => x.Value.Id != id && Effective(x.Key).Contains(binding, StringComparer.Ordinal)).Select(x => x.Key).ToArray(); if (conflict.Length > 0 && !replace) throw new InvalidOperationException("INPUT0304: binding conflicts with " + string.Join(",", conflict)); if (replace) foreach (var other in conflict) overrides.Remove(other); var list = action.AllowMultiple && overrides.TryGetValue(id, out var existing) ? existing : []; if (!action.AllowMultiple) list = []; list.Add(binding); overrides[id] = list.Distinct(StringComparer.Ordinal).ToList(); }
    public void RemoveOverride(string id) { if (!actions.ContainsKey(id)) throw new InvalidOperationException("INPUT0302: unknown action " + id); overrides.Remove(id); }
    public void ResetOne(string id) => RemoveOverride(id);
    public void ResetAll() => overrides.Clear();
    public void LoadOverrides(IReadOnlyDictionary<string, IReadOnlyList<string>> stored)
    {
        overrides.Clear();
        foreach (var pair in stored)
        {
            if (!actions.TryGetValue(pair.Key, out var action) || !action.Rebindable || pair.Value.Count == 0 || pair.Value.Any(string.IsNullOrWhiteSpace)) continue;
            if (pair.Value.Any(binding => !action.AllowedInputClasses.Any(inputClass => binding.StartsWith(inputClass switch { BindingInputClass.Keyboard => "Key:", BindingInputClass.Mouse => "Mouse:", _ => "Modifier:" }, StringComparison.Ordinal)))) continue;
            overrides[pair.Key] = pair.Value.Distinct(StringComparer.Ordinal).Take(action.AllowMultiple ? 8 : 1).ToList();
        }
    }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Overrides => overrides.ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Value.ToArray(), StringComparer.Ordinal);
}
public sealed class InputContextRouter
{
    public static readonly string[] Contexts = ["global", "menu", "gameplay", "map-tool", "text-entry", "diagnostics"];
    public bool TextEntryActive { get; set; }
    public string? Route(string context, bool modal, bool focusedText, bool activeTool, string actionId) { if (!Contexts.Contains(context, StringComparer.Ordinal)) throw new InvalidOperationException("INPUT0305: unknown context"); if (modal) return "modal"; if (focusedText || TextEntryActive) return "text-entry"; if (activeTool) return "map-tool"; return context; }
}
