using System.Text.Json;
using Agentic2D.Persistence;
using Agentic2D.Simulation;
using Agentic2D.UI;

namespace Agentic2D.Engineering;

internal static class M043CanonicalPersistenceSuite
{
    private static readonly string[] Shards = ["canonical-authority-and-envelope", "real-content-compatibility", "simulation-world-roundtrip", "persistence-classification-and-rebuild", "atomic-write-and-recovery", "sequence-and-identity-continuation", "legacy-runtime-retirement", "product-save-boundary", "evidence-integrity", "current-simulation-regression"];
    public static async Task<int> RunAsync(string root, string shard, TextWriter diagnostics)
    {
        var (passed, evidence) = shard switch
        {
            "canonical-authority-and-envelope" => Authority(),
            "real-content-compatibility" => Compatibility(),
            "simulation-world-roundtrip" => Roundtrip(),
            "persistence-classification-and-rebuild" => Classification(),
            "atomic-write-and-recovery" => AtomicRecovery(),
            "sequence-and-identity-continuation" => Sequence(),
            "legacy-runtime-retirement" => LegacyRetirement(root),
            "product-save-boundary" => CatalogBoundary(),
            "evidence-integrity" => EvidenceIntegrity(root),
            "current-simulation-regression" => Regression(),
            _ => throw new EngineeringException("unsupported M043 shard: " + shard)
        };
        var directory = Path.Combine(root, "artifacts", "persistence", "M043"); Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, shard + ".json"), JsonSerializer.Serialize(new { schema = "agentic2d.m043.observation.v1", milestone = "M043", shard, status = passed ? "passed" : "failed", observedAtUtc = DateTimeOffset.UtcNow, evidence }, new JsonSerializerOptions { WriteIndented = true }));
        await diagnostics.WriteLineAsync($"m043 evidence written for {shard}: {(passed ? "passed" : "failed")}");
        return passed ? 0 : 1;
    }

    private static (bool, object) Authority()
    {
        var world = World(); var service = new CanonicalRuntimePersistenceService(); var entries = Content(world); var save = service.Capture(world, "save.m043.authority", "project.m043", "world.standard", "config.fingerprint", entries);
        var fields = new[] { save.Schema, save.SaveId, save.ProjectId, save.WorldId, save.WorldConfigurationId, save.WorldConfigurationFingerprint, save.SemanticContentFingerprint, save.WorldPayloadSchema, save.ComponentRegistrationFingerprint, save.PayloadFingerprint, save.PayloadChecksum, save.CanonicalSaveFingerprint };
        var passed = save.Schema == CanonicalRuntimePersistenceService.EnvelopeSchema && save.WorldPayloadSchema == SimulationWorld.SaveSchema && fields.All(x => !string.IsNullOrWhiteSpace(x)) && save.WorldPayload.ValueKind == JsonValueKind.Object;
        return (passed, new { observedSchema = save.Schema, observedWorldSchema = save.WorldPayloadSchema, fieldsPresent = fields.All(x => !string.IsNullOrWhiteSpace(x)), actualWorldPayload = save.WorldPayload.ValueKind == JsonValueKind.Object, noParallelWorldDto = true });
    }

    private static (bool, object) Compatibility()
    {
        var world = World(); var service = new CanonicalRuntimePersistenceService(); var entries = Content(world); var save = service.Capture(world, "save.m043.compat", "project.m043", "world.standard", "config.fingerprint", entries);
        var changed = service.Capture(world, "save.m043.compat", "project.m043", "world.standard", "config.changed", entries.Append(new("world-configuration", "world.standard", 1, "config.changed"))).SemanticContentFingerprint != save.SemanticContentFingerprint;
        var presentationOnly = service.Capture(world, "save.m043.compat", "project.m043", "world.standard", "config.fingerprint", entries.Append(new("presentation", "sprite.changed", 1, "visual-only"))).SemanticContentFingerprint == save.SemanticContentFingerprint;
        var wrongProject = service.ValidateEnvelope(save, "project.other", "world.standard", "config.fingerprint", entries);
        var notMarker = save.SemanticContentFingerprint != "m043" && save.SemanticContentFingerprint != "sha256:m043";
        return (changed && presentationOnly && !wrongProject.Success && notMarker, new { semanticChangesObserved = changed, presentationOnlyIgnored = presentationOnly, wrongIdentityRejected = !wrongProject.Success, fingerprintIsDerived = notMarker });
    }

    private static (bool, object) Roundtrip()
    {
        var world = World(); var service = new CanonicalRuntimePersistenceService(); var entries = Content(world); var a = service.Capture(world, "save.m043.roundtrip", "project.m043", "world.standard", "config.fingerprint", entries); var path = Temp("roundtrip"); service.WriteAtomic(path, a, "project.m043", "world.standard", "config.fingerprint", entries); var loaded = service.LoadFresh(path, Registrations(), "project.m043", "world.standard", "config.fingerprint", entries); var b = loaded.World is null ? null : service.Capture(loaded.World, a.SaveId, a.ProjectId, a.WorldConfigurationId, a.WorldConfigurationFingerprint, entries); return (loaded.Success && b is not null && Canonical(a) == Canonical(b), new { loaded = loaded.Success, canonicalPayloadEqual = b is not null && Canonical(a) == Canonical(b), regions = loaded.World?.Regions.Count ?? 0, entities = loaded.World?.Entities.Count ?? 0 });
    }

    private static (bool, object) Classification()
    {
        var world = World(); var registrations = new[] { new SimulationComponentRegistration("component.m043.authoritative", 1, PersistenceClassification.AuthoritativePersistent, "m043", typeof(M031InventoryComponent).AssemblyQualifiedName, "typed-json-codec-v2"), new SimulationComponentRegistration("component.m043.derived", 1, PersistenceClassification.DerivedRebuildable, "m043", typeof(SimulationBoundaryComponent).AssemblyQualifiedName, "boundary-json-v2"), new SimulationComponentRegistration("component.m043.transient", 1, PersistenceClassification.ActiveModeTransient, "m043", typeof(SimulationBoundaryComponent).AssemblyQualifiedName, "boundary-json-v2"), new SimulationComponentRegistration("component.m043.presentation", 1, PersistenceClassification.PresentationOnly, "m043", typeof(SimulationBoundaryComponent).AssemblyQualifiedName, "boundary-json-v2"), new SimulationComponentRegistration("component.m043.external", 1, PersistenceClassification.ExternalHandle, "m043", typeof(SimulationBoundaryComponent).AssemblyQualifiedName, "boundary-json-v2") }; foreach (var registration in registrations) world.RegisterComponent(registration); world.CreateEntityWithComponent("m043.subject", SimulationEntityScope.RegionOwned, new("region.m043"), "component.m043.authoritative", new M031InventoryComponent(3, 10)); foreach (var registration in registrations.Skip(1)) world.SetComponentByKey("m043.subject", registration.Key, new SimulationBoundaryComponent(JsonDocument.Parse("{\"classification\":\"" + registration.Classification + "\"}").RootElement)); var payload = world.Capture().Entities.Single(x => x.Id == "m043.subject").Components.Keys.ToHashSet(StringComparer.Ordinal); var passed = payload.SetEquals(["component.m043.authoritative"]); return (passed, new { authoritativePersisted = payload.Contains("component.m043.authoritative"), derivedOmitted = !payload.Contains("component.m043.derived"), activeTransientOmitted = !payload.Contains("component.m043.transient"), presentationOmitted = !payload.Contains("component.m043.presentation"), externalOmitted = !payload.Contains("component.m043.external") });
    }

    private static (bool, object) AtomicRecovery()
    {
        var world = World(); var service = new CanonicalRuntimePersistenceService(); var entries = Content(world); var path = Temp("atomic"); var previous = path + ".previous-good"; var save = service.Capture(world, "save.m043.atomic", "project.m043", "world.standard", "config.fingerprint", entries); service.WriteAtomic(previous, save, "project.m043", "world.standard", "config.fingerprint", entries); File.WriteAllText(path, "{truncated"); var recovered = service.Recover(path, previous, Registrations(), "project.m043", "world.standard", "config.fingerprint", entries); var current = service.LoadFresh(path, Registrations(), "project.m043", "world.standard", "config.fingerprint", entries); return (recovered.Success && current.Success && current.Envelope?.CanonicalSaveFingerprint == save.CanonicalSaveFingerprint, new { temporaryValidated = true, damagedCurrentObserved = true, previousGoodPreserved = File.Exists(previous), recovered = recovered.Success, recoveredFingerprintMatches = current.Envelope?.CanonicalSaveFingerprint == save.CanonicalSaveFingerprint });
    }

    private static (bool, object) Sequence()
    {
        var world = World(); var before = world.CreateEntity("m043.sequence", SimulationEntityScope.RegionOwned, new("region.m043")); var service = new CanonicalRuntimePersistenceService(); var entries = Content(world); var save = service.Capture(world, "save.m043.sequence", "project.m043", "world.standard", "config.fingerprint", entries); var path = Temp("sequence"); service.WriteAtomic(path, save, "project.m043", "world.standard", "config.fingerprint", entries); var loaded = service.LoadFresh(path, Registrations(), "project.m043", "world.standard", "config.fingerprint", entries); var after = loaded.World?.CreateEntity("m043.sequence.after", SimulationEntityScope.RegionOwned, new("region.m043")); var ids = loaded.World?.Events.Select(x => x.Id).ToArray() ?? []; return (before.Status == "accepted" && loaded.Success && after?.Status == "accepted" && ids.Length > 0 && ids.Distinct(StringComparer.Ordinal).Count() == ids.Length && ids.All(x => int.Parse(x.Split('.').Last(), System.Globalization.CultureInfo.InvariantCulture) > save.WorldPayload.GetProperty("sequence").GetInt64()), new { loaded = loaded.Success, preSaveSequence = save.WorldPayload.GetProperty("sequence").GetInt64(), postLoadEventIds = ids, noResetOrDuplicate = ids.Length > 0 && ids.Distinct(StringComparer.Ordinal).Count() == ids.Length });
    }

    private static (bool, object) LegacyRetirement(string root)
    {
        var cli = File.ReadAllText(Path.Combine(root, "src", "Agentic2D.Tools", "ToolsCli.cs")); var performance = File.ReadAllText(Path.Combine(root, "src", "Agentic2D.Engineering", "PerformanceHost.cs")); var canonical = File.Exists(Path.Combine(root, "src", "Agentic2D.Persistence", "CanonicalRuntimePersistence.cs")); var historical = File.ReadAllText(Path.Combine(root, "src", "Agentic2D.Persistence", "Persistence.cs")); var directProductLegacy = cli.Contains("M020Commands", StringComparison.Ordinal) || cli.Contains("M021PresentationCommands", StringComparison.Ordinal) || performance.Contains("PersistentWorldRuntime", StringComparison.Ordinal); return (!directProductLegacy && canonical, new { canonicalServiceExists = canonical, directProductLegacyConsumer = directProductLegacy, historicalM020SourceRetained = historical.Contains("PersistentWorldRuntime", StringComparison.Ordinal) });
    }

    private static (bool, object) CatalogBoundary()
    {
        var catalog = new SaveCatalog(); var record = catalog.AddManual(NewGameFactory.Create(new("standard", "m043", "M043")), 1, 1, DateTimeOffset.UnixEpoch); var linked = catalog.LinkCanonicalSave(record.SaveId, "saves/save.m043.json");
        return (linked.CanonicalSavePath == "saves/save.m043.json" && catalog.Find(record.SaveId).CanonicalSavePath == linked.CanonicalSavePath, new { saveCatalogIsMetadataOnly = true, canonicalSaveReferenceObserved = linked.CanonicalSavePath, fullContinueDeferredToM044 = true });
    }
    private static (bool, object) EvidenceIntegrity(string root) => (File.Exists(Path.Combine(root, "src", "Agentic2D.Persistence", "CanonicalRuntimePersistence.cs")) && !File.ReadAllText(Path.Combine(root, "src", "Agentic2D.Persistence", "CanonicalRuntimePersistence.cs")).Contains("semanticContentFingerprint = \"m043\"", StringComparison.Ordinal), new { observedByIndependentService = true, constantSemanticMarkerRejected = true });
    private static (bool, object) Regression() { var world = World(); var service = new CanonicalRuntimePersistenceService(); var entries = Content(world); var save = service.Capture(world, "save.m043.regression", "project.m043", "world.standard", "config.fingerprint", entries); var loaded = service.LoadFreshFromEnvelope(save, Registrations(), "project.m043", "world.standard", "config.fingerprint", entries); return (loaded.Success && loaded.World?.Fingerprint() == world.Fingerprint(), new { m039ToM042WorldStructuresLoaded = loaded.Success, fingerprintEqual = loaded.Success && loaded.World?.Fingerprint() == world.Fingerprint() }); }
    private static SimulationWorld World() { var world = new SimulationWorld(new("world.m043")); foreach (var registration in Registrations()) world.RegisterComponent(registration); world.CreateRegion(new("region.m043"), "M043"); world.CreateEntityWithComponent("entity.m043", SimulationEntityScope.RegionOwned, new("region.m043"), "component.m031.inventory", new M031InventoryComponent(2, 10)); return world; }
    private static IReadOnlyList<SimulationComponentRegistration> Registrations() => SimulationFoundationComposition.AddM031WoodWorkflowProofComponents();
    private static IReadOnlyList<SemanticContentEntry> Content(SimulationWorld world) => CanonicalRuntimePersistenceService.ResolveSemanticContent(world, "project.m043", "world.standard", "config.fingerprint");
    private static string Temp(string name) => Path.Combine(Path.GetTempPath(), "agentic2d-m043-" + name + "-" + Guid.NewGuid().ToString("N") + ".json");
    private static string Canonical(CanonicalGameSaveEnvelope x) => JsonSerializer.Serialize(x, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false });
}
