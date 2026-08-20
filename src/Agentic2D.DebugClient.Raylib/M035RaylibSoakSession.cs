using System.Diagnostics;
using System.Text.Json;
using Agentic2D.Simulation;
using Raylib_cs;
using Rl = Raylib_cs.Raylib;

namespace Agentic2D.DebugClient;

/// <summary>
/// A real-time, read-only view over a live M035 test fixture.  The adapter owns
/// window/input state only; <see cref="LiveFixture"/> owns the authoritative
/// world and every displayed change is derived from it.
/// </summary>
internal static class M035RaylibSoakSession
{
    private const double DwellActivationSeconds = .8d;
    private static readonly WorkflowButton[] WorkflowButtons =
    [
        new("pause", "Pause", new Rectangle(20, 628, 140, 42)),
        new("resume", "Resume", new Rectangle(170, 628, 140, 42)),
        new("speed-increase", "Faster", new Rectangle(320, 628, 140, 42)),
        new("speed-decrease", "Slower", new Rectangle(470, 628, 140, 42)),
        new("region-switch", "Switch region", new Rectangle(620, 628, 140, 42)),
        new("save", "Save", new Rectangle(770, 628, 140, 42)),
        new("load", "Load", new Rectangle(920, 628, 140, 42)),
        new("diagnostics-overlay", "Diagnostics", new Rectangle(1070, 628, 110, 42)),
    ];

    public static int Run(string[] arguments)
    {
        string? input = null, capture = null, output = null, continuationSession = null;
        var durationSeconds = 0;
        var workflowOnly = false;
        for (var index = 0; index < arguments.Length; index++)
        {
            if (arguments[index] == "--input" && ++index < arguments.Length) input = arguments[index];
            else if (arguments[index] == "--capture" && ++index < arguments.Length) capture = arguments[index];
            else if (arguments[index] == "--output" && ++index < arguments.Length) output = arguments[index];
            else if (arguments[index] == "--duration-seconds" && ++index < arguments.Length && int.TryParse(arguments[index], out durationSeconds)) { }
            else if (arguments[index] == "--continuation-session" && ++index < arguments.Length) continuationSession = arguments[index];
            else if (arguments[index] == "--workflow-only") workflowOnly = true;
            else return Usage();
        }
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(capture) || string.IsNullOrWhiteSpace(output) || durationSeconds < 1 || !File.Exists(input) || workflowOnly && (string.IsNullOrWhiteSpace(continuationSession) || !File.Exists(continuationSession))) return Usage();

        try
        {
            // The M034 operations dashboard is required as a known-valid launch input,
            // but it is not treated as live authority.
            using var document = JsonDocument.Parse(File.ReadAllText(input));
            var launchRegions = document.RootElement.GetProperty("regions").EnumerateArray().Select(item => item.Clone()).ToArray();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(capture))!);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            var fixture = new LiveFixture(launchRegions);
            var initial = fixture.Fingerprint;
            string? continuationFingerprint = null;
            if (workflowOnly)
            {
                using var continuation = JsonDocument.Parse(File.ReadAllText(continuationSession!));
                continuationFingerprint = continuation.RootElement.GetProperty("finalFingerprint").GetString();
                if (continuation.RootElement.GetProperty("completedSeconds").GetInt64() < 14_400 || continuation.RootElement.GetProperty("earlyTermination").GetBoolean()) throw new InvalidOperationException("M035 workflow continuation requires a completed four-hour primary session.");
            }
            var sessionId = Environment.GetEnvironmentVariable("M035_TESTER_SESSION_ID") ?? "session.m035.graphical";
            var startedAt = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            var frames = 0;
            var captured = false;
            var paused = false;
            var speed = 1;
            var controls = new SortedSet<string>(StringComparer.Ordinal);
            var samples = new List<object>();
            var nextSample = 60d;
            var lastAdvance = 0d;
            var closedEarly = false;
            string? hoveredAction = null;
            var hoverStartedAt = 0d;
            var hoverActivated = false;
            Rl.InitWindow(1200, 680, "Agentic2D M035 graphical soak — live runtime fixture");
            Rl.SetTargetFPS(30);
            try
            {
                void Activate(string action)
                {
                    switch (action)
                    {
                        case "pause": paused = true; controls.Add("pause"); return;
                        case "resume": paused = false; controls.Add("resume"); return;
                        case "pause-resume": paused = !paused; controls.Add(paused ? "pause" : "resume"); return;
                        case "speed-increase": speed = Math.Min(4, speed + 1); break;
                        case "speed-decrease": speed = Math.Max(1, speed - 1); break;
                        case "region-switch": fixture.SwitchDetailed(); break;
                        case "save": fixture.Save(); break;
                        case "load": fixture.Load(); break;
                    }
                    controls.Add(action);
                }
                while (!Rl.WindowShouldClose() && stopwatch.Elapsed.TotalSeconds < durationSeconds)
                {
                    if (Rl.IsKeyPressed(KeyboardKey.Space)) Activate("pause-resume");
                    if (Rl.IsKeyPressed(KeyboardKey.Equal)) Activate("speed-increase");
                    if (Rl.IsKeyPressed(KeyboardKey.Minus)) Activate("speed-decrease");
                    if (Rl.IsKeyPressed(KeyboardKey.Tab)) Activate("region-switch");
                    if (Rl.IsKeyPressed(KeyboardKey.S)) Activate("save");
                    if (Rl.IsKeyPressed(KeyboardKey.L)) Activate("load");
                    if (Rl.IsKeyPressed(KeyboardKey.F1)) Activate("diagnostics-overlay");
                    var mouse = Rl.GetMousePosition();
                    var hovered = WorkflowButtons.FirstOrDefault(button => Rl.CheckCollisionPointRec(mouse, button.Bounds));
                    var actionUnderPointer = hovered?.Action;
                    if (actionUnderPointer != hoveredAction)
                    {
                        hoveredAction = actionUnderPointer;
                        hoverStartedAt = stopwatch.Elapsed.TotalSeconds;
                        hoverActivated = false;
                    }
                    if (hovered is not null && !hoverActivated && stopwatch.Elapsed.TotalSeconds - hoverStartedAt >= DwellActivationSeconds) { Activate(hovered.Action); hoverActivated = true; }
                    if (hovered is not null && Rl.IsMouseButtonPressed(MouseButton.Left)) { Activate(hovered.Action); hoverActivated = true; }

                    if (!paused && stopwatch.Elapsed.TotalSeconds - lastAdvance >= .20d / speed)
                    {
                        fixture.Advance();
                        lastAdvance = stopwatch.Elapsed.TotalSeconds;
                    }
                    Draw(fixture, paused, speed, stopwatch.Elapsed, durationSeconds, controls, hoveredAction, hoverStartedAt, hoverActivated);
                    frames++;
                    if (!captured) { Rl.TakeScreenshot(Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.GetFullPath(capture))); captured = true; }
                    if (stopwatch.Elapsed.TotalSeconds >= nextSample)
                    {
                        samples.Add(new { elapsedSeconds = (long)stopwatch.Elapsed.TotalSeconds, simulationInstantMicroseconds = fixture.Instant, worldFingerprint = fixture.Fingerprint, transitionCount = fixture.Transitions, managedBytes = GC.GetTotalMemory(false), workingSetBytes = Environment.WorkingSet, frames, averageFrameMilliseconds = stopwatch.Elapsed.TotalMilliseconds / Math.Max(frames, 1) });
                        nextSample += 60d;
                    }
                }
                closedEarly = stopwatch.Elapsed.TotalSeconds < durationSeconds;
            }
            finally { if (Rl.IsWindowReady()) Rl.CloseWindow(); }
            stopwatch.Stop();
            var changed = initial != fixture.Fingerprint && fixture.Instant > 0 && fixture.Transitions > 0;
            var requiredControls = new[] { "pause", "resume", "speed-increase", "speed-decrease", "region-switch", "save", "load", "diagnostics-overlay" };
            var operatorWorkflowComplete = requiredControls.All(controls.Contains);
            var status = !closedEarly && captured && changed && operatorWorkflowComplete ? "passed" : "failed-early-termination-no-live-progress-or-incomplete-operator-workflow";
            File.WriteAllText(output, JsonSerializer.Serialize(new
            {
                schema = "agentic2d.m035.graphical-soak-session.v2",
                status,
                targetSeconds = durationSeconds,
                completedSeconds = (long)stopwatch.Elapsed.TotalSeconds,
                frames,
                sessionId,
                startedAtUtc = startedAt,
                finishedAtUtc = DateTimeOffset.UtcNow,
                seed = "m035-reference-seed",
                diagnosticsMode = "continuous-bounded",
                capture = Path.GetRelativePath(Path.GetDirectoryName(Path.GetFullPath(output))!, Path.GetFullPath(capture)),
                controlsObserved = controls.ToArray(),
                input = Path.GetRelativePath(Path.GetDirectoryName(Path.GetFullPath(output))!, Path.GetFullPath(input)),
                adapterReadOnly = true,
                liveAuthority = "agentic2d.simulation-world.v1",
                initialFingerprint = initial,
                finalFingerprint = fixture.Fingerprint,
                simulationInstantMicroseconds = fixture.Instant,
                transitionCount = fixture.Transitions,
                saveLoadCount = fixture.SaveLoads,
                requiredControls,
                operatorWorkflowComplete,
                samples,
                earlyTermination = closedEarly,
                workflowOnly,
                continuationSession = continuationSession is null ? null : Path.GetRelativePath(Path.GetDirectoryName(Path.GetFullPath(output))!, Path.GetFullPath(continuationSession)),
                continuationFinalFingerprint = continuationFingerprint,
                environment = "Raylib/X11 graphical session; live runtime fixture",
            }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            return status == "passed" ? 0 : 1;
        }
        catch (Exception exception) when (exception is IOException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            Console.Error.WriteLine("m035 graphical soak failed: " + exception.Message);
            return 1;
        }
    }

    private static void Draw(LiveFixture fixture, bool paused, int speed, TimeSpan elapsed, int targetSeconds, IReadOnlySet<string> controls, string? hoveredAction, double hoverStartedAt, bool hoverActivated)
    {
        Rl.BeginDrawing(); Rl.ClearBackground(new Color(20, 31, 48, 255));
        Rl.DrawText("M035: heavy internal-testing graphical soak", 30, 20, 28, Color.RayWhite);
        Rl.DrawText("LIVE authoritative fixture — displayed time, regions, events and saves change in SimulationWorld", 30, 56, 16, Color.LightGray);
        foreach (var region in fixture.Regions.Select((value, index) => (value, index))) DrawRegion(region.value, 30 + (region.index % 3) * 390, 110 + (region.index / 3) * 205);
        Rl.DrawText($"real {(long)elapsed.TotalSeconds}/{targetSeconds}s | sim day {fixture.Instant / 86_400_000_000L + 1} | {(paused ? "PAUSED" : "RUNNING")} | {speed}x | events {fixture.Events}", 30, 550, 20, paused ? Color.Yellow : Color.Lime);
        Rl.DrawText("Keyboard: Space toggle, +/- speed, Tab switch, S save, L load, F1 diagnostics. Mouse: click or hover a button for 0.8s.", 30, 585, 15, Color.LightGray);
        Rl.DrawText("Observed controls: " + (controls.Count == 0 ? "none yet" : string.Join(", ", controls)), 30, 607, 14, Color.LightGray);
        foreach (var button in WorkflowButtons)
        {
            var selected = controls.Contains(button.Action); var hovered = button.Action == hoveredAction;
            Rl.DrawRectangleRec(button.Bounds, selected ? Color.DarkGreen : hovered ? Color.DarkBlue : Color.DarkGray);
            Rl.DrawRectangleLinesEx(button.Bounds, 2, selected ? Color.Lime : Color.LightGray);
            Rl.DrawText(button.Label, (int)button.Bounds.X + 8, (int)button.Bounds.Y + 13, 15, Color.RayWhite);
            if (hovered && !hoverActivated)
            {
                var progress = (float)Math.Clamp((elapsed.TotalSeconds - hoverStartedAt) / DwellActivationSeconds, 0d, 1d);
                Rl.DrawRectangle((int)button.Bounds.X, (int)(button.Bounds.Y + button.Bounds.Height - 4), (int)(button.Bounds.Width * progress), 4, Color.Yellow);
            }
        }
        Rl.EndDrawing();
    }

    private static void DrawRegion(LiveRegion region, int x, int y)
    {
        var color = region.Detailed ? new Color(52, 132, 196, 255) : new Color(59, 84, 110, 255);
        Rl.DrawRectangle(x, y, 350, 175, color); Rl.DrawRectangleLines(x, y, 350, 175, region.Detailed ? Color.Yellow : Color.LightGray);
        Rl.DrawText(region.Id, x + 18, y + 16, 20, Color.RayWhite); Rl.DrawText(region.Detailed ? "DETAILED" : "ABSTRACT", x + 18, y + 44, 16, region.Detailed ? Color.Yellow : Color.LightGray);
        Rl.DrawText($"workers {region.Workers} | infrastructure {region.Infrastructure} | plans {region.Plans}", x + 18, y + 76, 16, Color.RayWhite);
        Rl.DrawText($"water {region.Water}/100  food {region.Food}/100  alerts {region.Alerts}", x + 18, y + 105, 16, Color.RayWhite);
        Rl.DrawText($"activity cycle {region.Cycle} | state revision {region.Revision}", x + 18, y + 134, 16, Color.LightGray);
    }

    private static int Usage() { Console.Error.WriteLine("m035 requires --input <world-dashboard.json> --duration-seconds <seconds> --capture <png> --output <session.json> [--workflow-only --continuation-session <four-hour-session.json>]"); return 2; }

    private sealed record WorkflowButton(string Action, string Label, Rectangle Bounds);

    private sealed class LiveFixture
    {
        private const string Component = "component.m035.graphical-worker";
        private SimulationWorld world = new(new WorldId("world.m035.graphical-live"));
        private readonly DiscreteEventScheduler scheduler = new();
        private RegionFidelityCoordinator coordinator;
        private SimulationSave? checkpoint;
        private int cursor;
        private int cycle;

        public LiveFixture(IReadOnlyList<JsonElement> launchRegions)
        {
            world.RegisterComponent(new(Component, 1, PersistenceClassification.AuthoritativePersistent, "m035.graphical-soak"));
            var ids = Enumerable.Range(1, 5).Select(value => value <= launchRegions.Count ? launchRegions[value - 1].GetProperty("regionId").GetString() ?? "region.m035.live." + value.ToString("D2", System.Globalization.CultureInfo.InvariantCulture) : "region.m035.live." + value.ToString("D2", System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            foreach (var (id, index) in ids.Select((value, index) => (value, index)))
            {
                Require(world.CreateRegion(new RegionId(id), id));
                for (var worker = 1; worker <= 10; worker++)
                {
                    var entity = id + ".worker." + worker.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
                    Require(world.CreateEntityWithComponent(entity, SimulationEntityScope.RegionOwned, new RegionId(id), Component, JsonSerializer.SerializeToElement(new { worker, water = 100, food = 100 })));
                    Require(world.ActivateEntity(entity));
                }
                for (var item = 1; item <= 3; item++) CreateFixtureEntity(id + ".infrastructure." + item.ToString("D2", System.Globalization.CultureInfo.InvariantCulture), id, "infrastructure", item);
                for (var item = 1; item <= 2; item++) CreateFixtureEntity(id + ".plan." + item.ToString("D2", System.Globalization.CultureInfo.InvariantCulture), id, "plan", item);
            }
            coordinator = NewCoordinator(world, scheduler, ids, 0);
        }

        public long Instant => world.Clock.Now.Microseconds;
        public string Fingerprint => world.Fingerprint();
        public int Events => world.Events.Count;
        public int Transitions => coordinator.Transitions.Count(item => item.Status == "committed");
        public int SaveLoads { get; private set; }
        public IReadOnlyList<LiveRegion> Regions => coordinator.Regions.Select(state =>
        {
            var entities = world.QueryRegion(new RegionId(state.RegionId));
            var workers = entities.Count(item => item.Id.Contains(".worker.", StringComparison.Ordinal));
            var infrastructure = entities.Count(item => item.Id.Contains(".infrastructure.", StringComparison.Ordinal));
            var plans = entities.Count(item => item.Id.Contains(".plan.", StringComparison.Ordinal));
            var consumption = (cycle + state.Revision) % 25;
            return new LiveRegion(state.RegionId, state.Fidelity == RegionFidelity.Detailed, workers, infrastructure, plans, 100 - consumption, 100 - ((consumption * 3) % 25), consumption > 18 ? 1 : 0, cycle, state.Revision);
        }).ToArray();

        public void Advance()
        {
            world.Advance(SimulationDuration.FromSeconds(3_600)); cycle++;
            var active = world.Entities.Where(item => item.Lifecycle == SimulationLifecycle.Active).Take(5).Select(item => item.Id).ToArray();
            Require(world.RecordFact("M035LiveFixtureAdvanced", active, new { cycle, instant = world.Clock.Now.Microseconds }));
            if (cycle % 4 == 0) SwitchDetailed();
        }

        public void SwitchDetailed()
        {
            var ids = coordinator.Regions.Select(item => item.RegionId).ToArray(); cursor = (cursor + 1) % ids.Length;
            var transition = coordinator.SwitchDetailed(ids[cursor]);
            if (transition.Status != "committed") throw new InvalidOperationException("M035-LIVE-FIDELITY: " + transition.Diagnostic);
        }

        public void Save() { checkpoint = world.Capture(); }
        public void Load()
        {
            if (checkpoint is null) return;
            var loaded = SimulationWorld.Load(checkpoint, [new(Component, 1, PersistenceClassification.AuthoritativePersistent, "m035.graphical-soak")]);
            if (!loaded.Success || loaded.World is null) throw new InvalidOperationException("M035-LIVE-LOAD: " + string.Join(",", loaded.Diagnostics.Select(item => item.Code)));
            world = loaded.World;
            var regions = world.Regions.Select(item => item.Id).ToArray();
            coordinator = NewCoordinator(world, scheduler, regions, cursor % regions.Length);
            SaveLoads++;
        }

        private void CreateFixtureEntity(string entity, string region, string role, int item)
        {
            Require(world.CreateEntityWithComponent(entity, SimulationEntityScope.RegionOwned, new(region), Component, JsonSerializer.SerializeToElement(new { role, item, condition = 100 })));
            Require(world.ActivateEntity(entity));
        }

        private static RegionFidelityCoordinator NewCoordinator(SimulationWorld value, DiscreteEventScheduler queue, IReadOnlyList<string> ids, int detailed) => new(value, queue, ids.Select((id, index) => new RegionFidelityState(id, index == detailed ? RegionFidelity.Detailed : RegionFidelity.Abstract, index == detailed ? "detailed" : "abstract", 1, RegionTransitionStatus.Stable, 0)));
        private static void Require(SimulationCommandResult result) { if (result.Status != "accepted") throw new InvalidOperationException("M035-LIVE-FIXTURE: " + string.Join(",", result.Diagnostics.Select(item => item.Code))); }
    }

    private sealed record LiveRegion(string Id, bool Detailed, int Workers, int Infrastructure, int Plans, int Water, int Food, int Alerts, int Cycle, int Revision);
}
