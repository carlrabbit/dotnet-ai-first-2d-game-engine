using Agentic2D.UI;

namespace Agentic2D.Tests.Unit;

public sealed class UiToolkitTests
{
    [Test] public async Task TextEntryCapturesText() { var field = new UiTextField("name"); var screen = new UiScreen("screen"); screen.Add(field); var host = new UiHost(screen); host.Focus(field); await Assert.That(host.Dispatch(new(UiInputKind.Text, "x"))).IsTrue(); await Assert.That(field.Value).IsEqualTo("x"); }
}
public sealed class ApplicationShellTests
{
    [Test] public async Task PlayerMenusHaveRequiredEntries() { await Assert.That(ProductMenus.Main(false).Select(x => x.Id)).IsEquivalentTo(["continue", "new-game", "load-game", "tutorial", "options", "credits", "quit"]); await Assert.That(ProductMenus.Pause().Select(x => x.Id)).DoesNotContain("statistics"); }
}
public sealed class PlayerDiagnosticsIsolationTests
{
    [Test] public async Task CompositionIsExplicit() { await Assert.That(new ApplicationShell(ClientComposition.Player).Composition).IsEqualTo(ClientComposition.Player); await Assert.That(new ApplicationShell(ClientComposition.Diagnostics).Composition).IsEqualTo(ClientComposition.Diagnostics); }
}
public sealed class SaveCatalogTests
{
    [Test] public async Task ContinueSkipsInvalidAndKeepsProvenance() { var world = NewGameFactory.Create(new("standard", "abc", "River")); var catalog = new SaveCatalog(); var save = catalog.AddManual(world, 3, 4, DateTimeOffset.UtcNow); catalog.Rename(save.SaveId, "Home"); await Assert.That(catalog.ResolveContinue().Save!.WorldId).IsEqualTo(world.WorldId); await Assert.That(catalog.ResolveContinue().Save!.SaveTitle).IsEqualTo("Home"); }
}
public sealed class AutosaveTests
{
    [Test] public async Task AutosaveWaitsForBoundary() { var scheduler = new AutosaveScheduler { Interval = TimeSpan.FromSeconds(1) }; scheduler.Tick(TimeSpan.FromSeconds(2), true, true, false); await Assert.That(scheduler.Pending).IsTrue(); await Assert.That(scheduler.SaveActive).IsFalse(); scheduler.Tick(TimeSpan.Zero, true, true, true); await Assert.That(scheduler.SaveActive).IsTrue(); }
}
public sealed class UserSettingsTests
{
    [Test] public async Task DefaultsValidate() => await Assert.That(UserSettings.Defaults.Validate()).IsEqualTo(UserSettings.Defaults);
}
public sealed class DisplaySafetyTests
{
    [Test] public async Task PreviewRevertsToKnownGood() { var preview = new DisplayPreview(); var known = new DisplayCandidate("windowed", "native", 1); preview.Begin(known, new("fullscreen", "1920x1080", 1)); await Assert.That(preview.Revert()).IsEqualTo(known); }
}
public sealed class SoftwareDefinedInputTests
{
    [Test] public async Task UnknownActionCannotBeRebound() { var registry = new BindingRegistry(); await Assert.That(() => registry.Rebind("missing", "K")).Throws<InvalidOperationException>(); }
}
public sealed class WorldConfigurationTests
{
    [Test] public async Task BundledConfigurationsValidate() { foreach (var config in WorldConfigurations.Bundled) await Assert.That(WorldConfigurations.Validate(config)).IsEqualTo(config); }
}
public sealed class ProductShellLifecycleTests
{
    [Test] public async Task DestructiveTransitionIsAlwaysConfirmed() { var shell = new ApplicationShell(); shell.Start(); shell.OpenWorld(); shell.RequestDestructive("quit"); await Assert.That(shell.TransitionLog).Contains("confirm:save-and-quit/without-saving/cancel"); }
    [Test] public async Task HostSessionStartsAtMainMenuPersistsCatalogAndContinues() { var root = Path.Combine(Path.GetTempPath(), "agentic2d-m037-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root); try { using var session = new ProductShellSession(Path.Combine(root, "catalog.json"), Path.Combine(root, "settings.json")); var startup = session.Start(safeMode: true); await Assert.That(startup.State).IsEqualTo(ApplicationState.MainMenu); session.StartNewGame(new("standard", "probe-seed", "River")); var saved = session.SaveManual(2, 4, DateTimeOffset.UnixEpoch); using var restored = new ProductShellSession(Path.Combine(root, "catalog.json"), Path.Combine(root, "settings.json")); restored.Start(); var continued = restored.Continue(); await Assert.That(continued.World!.WorldId).IsEqualTo(saved.WorldId); } finally { if (Directory.Exists(root)) Directory.Delete(root, true); } }
}

public sealed class BindingRecoveryTests
{
    [Test] public async Task InvalidOverrideFallsBackPerAction() { var registry = new BindingRegistry(); registry.Register(new("move", "input.move", "gameplay", "Move", BindingActionType.Axis, new HashSet<BindingInputClass> { BindingInputClass.Keyboard }, ["Key:W"], true, false, "replace", "gameplay", true)); registry.Register(new("confirm", "input.confirm", "menu", "Confirm", BindingActionType.Button, new HashSet<BindingInputClass> { BindingInputClass.Keyboard }, ["Key:Enter"], true, false, "replace", "menu", true)); registry.LoadOverrides(new Dictionary<string, IReadOnlyList<string>> { ["move"] = ["not-a-binding"], ["confirm"] = ["Key:Space"] }); await Assert.That(registry.Effective("move")).IsEquivalentTo(["Key:W"]); await Assert.That(registry.Effective("confirm")).IsEquivalentTo(["Key:Space"]); }
}
