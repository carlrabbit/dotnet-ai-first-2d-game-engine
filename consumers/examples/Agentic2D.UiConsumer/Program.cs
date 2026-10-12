using System.Text.Json;
using Agentic2D.UI;

var operation = new UiOperationProjection("world.save", AgentUiOperationStatus.Working, "Saving confirmed by the application controller");
var screen = UiDefinition.Normalize("consumer.world-settings", Ui.Screen("settings",
    Ui.Margin("settings.gutter", UiInsets.All(24), Ui.Column("settings.form",
        Ui.Title("settings.title", "World settings"),
        Ui.Text("settings.introduction", "An independently compiled game screen authored with the public typed UI vocabulary."),
        Ui.Toggle("settings.pause", "Pause on focus loss", "settings.pause", true),
        Ui.Slider("settings.scale", "Interface scale", "settings.scale", 1),
        Ui.TextField("settings.name", "World name", "settings.name", "North meadow"),
        Ui.Select("settings.density", "Population", "settings.density", ["Relaxed", "Standard", "Demanding"]),
        Ui.Button("settings.save", "Save settings", "settings.save"),
        Ui.Busy("settings.operation", "Save status", operation)))));
var viewport = new UiViewport(1920, 1080, UiScale: 1, OsDpiScale: 1);
var session = new UiSession(screen, viewport);
var evidence = session.ToEvidence();
Console.WriteLine(JsonSerializer.Serialize(evidence));
