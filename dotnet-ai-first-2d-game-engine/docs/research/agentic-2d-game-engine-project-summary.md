# Agentic 2D Game Engine — Project Summary

This document is a continuation summary for the **agentic 2D game engine project**. It captures the project idea, the design direction, and the decisions made so far. It is intended as a basis for continuing the project in another chat or with another agent.

---

## 1. Core project idea

The project is a conceptual design for a **2D game engine built from the beginning for agentic development**.

The assumption is:

> Humans design, review, and make creative decisions.  
> AI agents implement, modify, validate, and iterate on the game.

This is not “Godot with better tests” and not a traditional editor-centric engine. The engine should be **headless-first, CLI/API-first, validation-first, and artifact-first**.

A graphical editor is not considered essential. Instead, the primary workflow is:

```text
human design intent
→ agent modifies project
→ engine validates project
→ engine runs scenarios
→ engine generates artifacts/reports/previews
→ human reviews result
→ change is accepted or revised
```

The engine should behave more like a **domain-specific compiler plus deterministic runtime** than like a traditional visual authoring tool.

---

## 2. Main architectural principles

### 2.1 Headless-first

The engine should support normal operation through commands such as:

```bash
agentic2d validate
agentic2d scenario run smoke/new-game
agentic2d asset inspect assets/raw/tilesets/village.png
agentic2d map preview map.village_test
agentic2d shader compile
agentic2d package build
```

The graphical editor, if it exists later, is just another client.

### 2.2 Everything important has stable identity

Stable IDs are foundational.

Important things should have durable IDs:

```text
entities
components
commands
events
queries
assets
textures
tiles
tilesets
maps
markers
animations
animation events
materials
shaders
UI controls
dialogue nodes
quest steps
scenarios
validators
save fields
requirements
```

Agents should not rely on incidental filenames, hierarchy positions, display text, or visual coordinates.

### 2.3 Structured data over hidden editor state

Source-of-truth project data should be:

```text
schema-validated
diff-friendly
merge-friendly
round-trippable
addressable by stable ID
inspectable by agents
reviewable by humans
```

Avoid editor-only serialized blobs as the primary semantic model.

### 2.4 The runtime must be observable

The running game should expose structured inspection:

```text
current scene/map
entity list
component state
active systems
active UI screen
visible controls
physics contacts
navigation paths
loaded resources
event log
recent input
running animations
audio events
save state
random seed
performance counters
```

The agent should not infer game state from screenshots or source code alone.

### 2.5 Scenario testing is first-class

Scenario tests are a native engine concept, not a bolt-on testing framework.

Example scenario domains:

```text
smoke
gameplay
UI
asset import
map validation
animation validation
shader/material preview
save/load
performance
soak
regression
```

A scenario should be deterministic, machine-readable, and produce artifacts.

### 2.6 Rich diagnostics and artifacts

Failures should produce structured artifacts:

```text
result.json
diagnostics.json
events.jsonl
scene/map dump
UI dump
screenshot
preview image
collision overlay
navigation overlay
savegame snapshot
random seed
frame/tick number
performance metrics
```

This lets agents debug by inspecting evidence instead of guessing.

---

## 3. Runtime platform decision so far

We discussed possible .NET-based 2D runtime foundations.

### Recommended evaluation path

The recommended path was:

```text
Phase 1: raylib-cs spike
  Purpose: quickly prove the agentic runtime loop.

Phase 2: MonoGame prototype
  Purpose: evaluate serious .NET 2D runtime viability.

Phase 3: SDL3/Silk.NET spike only if needed
  Purpose: determine whether a fully custom runtime/platform layer is worth the cost.
```

### Current best serious candidate

The strongest default candidate is:

```text
MonoGame + custom engine architecture + Aether.Physics2D or Box2D.NET
```

Reasoning:

```text
MonoGame gives a mature .NET-friendly 2D runtime base.
It does not impose a full editor-centric engine model.
It provides windowing, graphics device, content, input, and audio.
The agentic engine can own the higher-level project model, scenario system, validation pipeline, asset pipeline, and behavior model.
```

### Other candidates considered

#### raylib-cs

Good for a fast prototype. Too thin as a likely long-term engine base unless we intentionally want to build nearly everything ourselves.

#### SDL3-CS + Silk.NET

Maximum control, but much more work. Better if the engine itself becomes the main long-term product.

#### FNA

Technically credible, XNA-compatible, but less obvious as the default unless XNA accuracy/desktop portability is central.

#### Stride

C# and powerful, but likely too much existing engine/editor/asset model for the intended architecture.

---

## 4. Core game logic architecture

### 4.1 CQRS-like model, but game-specific

We discussed CQRS, with the caveat that this should **not** be enterprise CQRS.

The game-engine version is:

```text
Command = requested mutation
System = validates/applies mutation
Event = factual result
Query = read-only view over current state
Projection = optimized read model
```

Example:

```text
Command:
  combat.attack(attacker, target)

Events:
  combat.attack_started
  combat.damage_applied
  enemy.defeated

Queries:
  combat.living_enemies_in_radius
  entity.position
  inventory.contents
```

### 4.2 Avoid enterprise CQRS patterns

Avoid:

```text
async command handlers inside simulation tick
DI-heavy dispatch
reflection in hot paths
boxed messages
string routing in packaged runtime
MediatR-style pipelines
JSON in hot paths
```

Prefer:

```text
record structs
typed IDs
generated dispatch
fixed tick scheduler
explicit command context
event buffers
dense component storage
predeclared queries
no reflection in packaged mode
```

### 4.3 CQRS + ECS-like storage

The recommended direction is not necessarily orthodox ECS, but ECS-like:

```text
entities
components
systems
commands
events
queries
projections
```

Commands mutate via systems. Queries read components/projections. Events record factual results.

### 4.4 Events are essential

Events serve:

```text
scenario assertions
debugging
agent-readable traces
coverage
replay
save/load validation
behavior inspection
```

Commands should usually return only small status results. Actual outcomes should be emitted as events.

---

## 5. Debug runtime vs packaged runtime

We discussed a dual-mode model.

### 5.1 Development/debug mode

Optimized for humans and agents:

```text
string IDs
JSON/YAML commands
rich diagnostics
runtime inspection
event logs
scenario runner
debug projections
source locations
review reports
```

### 5.2 Packaged/release mode

Optimized for performance:

```text
integer IDs
binary resources
precompiled lookup tables
generated dispatch
compiled projections
minimal diagnostics
no reflection
no JSON in hot paths
debug metadata stripped or minimized
```

### 5.3 Important decision

Do **not** build two different engines.

Use the same semantic model and generate different representations:

```text
same command semantics
same event semantics
same scenario semantics
same save semantics
same simulation tick behavior
```

Only replace:

```text
dispatch implementation
resource representation
diagnostic level
serialization format
ID resolution strategy
query/projection storage
```

The same scenarios should run against both debug and packaged runtime to avoid “works in editor/debug, broken in final build.”

---

## 6. Source generators

Source generators are considered a strong fit, but they should generate **boring deterministic plumbing**, not hidden game behavior.

Good generator targets:

```text
typed IDs
command metadata
event metadata
query metadata
dispatch tables
behavior registries
asset registries
JSON debug serializers
binary release serializers
schema documentation
scenario bindings
diagnostic descriptors
dependency reports
```

Avoid initially:

```text
full behavior generation
automatic query optimization
complex hidden dataflow analysis
large magical generated systems
```

Generator output should be:

```text
readable
snapshot-tested
stable
diagnostic-rich
boring
```

---

## 7. Asset pipeline and asset authoring

We expanded the project beyond gameplay logic. The engine must support **agent-operable asset workflows**.

Raw asset creation is out of scope:

```text
Agent does not need to create professional sprite art, audio, or final 2D models from scratch.
```

But asset interpretation and authoring from raw data is in scope:

```text
import raw image/audio/font data
inspect assets
detect grids/regions
derive textures, sprites, tiles, animations
create maps from images or symbolic descriptions
generate previews
validate references
wire assets into runtime content
```

### 7.1 Asset pipeline model

The core pattern:

```text
raw asset
+ asset perception
+ semantic authoring commands
+ provenance
+ generated metadata
+ previews
+ validators
+ dependency graph
= agent-operable asset workflow
```

### 7.2 Asset folders

Possible structure:

```text
/game
  /content
    /entities
    /items
    /quests
    /dialogue
    /ui
    /maps
    /materials
    /animations
    /shaders

  /assets
    /raw or /source
      /textures
      /audio
      /fonts
      /packages
    /import
      /textures
      /audio
      /fonts
      /shaders
    /generated
      /atlases
      /compiled-shaders
      /previews
      /reports
```

Generated assets should not be hand-edited.

### 7.3 Asset provenance

When an agent derives a tile, sprite, animation frame, or material from raw data, the engine should record provenance.

Example:

```yaml
id: tile.wall_stone
source:
  image: assets/raw/tilesets/village.png
  rect: [192, 0, 16, 16]
  derivedBy: tileset.create-grid
```

This supports:

```text
reimport
dependency tracking
human review
license/source tracking
change impact analysis
asset rebuild
```

---

## 8. Asset workbench commands

We envisioned a CLI/API asset workbench with commands like:

```text
asset inspect <path>
asset deps <id>
asset rebuild --affected <path-or-id>
asset validate <id|--all|--changed>
asset preview <id>

image detect-grid <path>
image detect-sprites <path>
image find-regions <path>
image crop <path> --rect ...
image contact-sheet <path>

texture create-from-image
spritesheet create-from-grid
spritesheet create-from-regions
animation create-from-frames
animation add-event
animation preview
animation validate

tileset create-grid
tileset classify
tileset add-collision
tileset add-terrain-rule
tileset preview
tileset validate

map create
map create-from-mask
map paint-region
map place-marker
map validate
map preview
map path-check

atlas create
atlas add
atlas pack
atlas preview
atlas validate

material create
material set
material preview
shader compile
shader preview
```

The agent should use semantic operations rather than manually patching many files.

---

## 9. Semantic asset metadata

A key problem: somebody needs to create semantic metadata for raw assets.

We concluded:

> AI should be used for first-pass semantic tagging, but AI tags should not be treated as final truth.

The model:

```text
AI proposes semantic metadata.
Engine validates structural consistency.
Rules translate labels into behavior.
Human reviews high-impact semantics.
Approved metadata becomes source-of-truth content.
```

### 9.1 Metadata layers

We should not collapse all meaning into one tag list.

Separate:

```text
Structural:
  tile size, rect, alpha, grid position, animation frames

Visual:
  grass, flower, butterfly, stone, wall, water

Physical:
  walkable, blocked, slow, collision, navigation cost

Gameplay:
  harvestable, damaging, cover, spawnable, interactable

Narrative/design:
  peaceful, magical, village, forest, dangerous

Technical:
  atlas group, render layer, material, shader
```

Important distinction:

```text
"grass" is visual interpretation.
"walkable" is behavior.
```

### 9.2 Controlled vocabulary/taxonomy

Avoid free-form AI labels like:

```text
grass
green grass
lush grass
lawn
grass tile
green_floor
```

Use a project taxonomy:

```yaml
id: taxonomy.top_down_pixelart

visualLabels:
  ground:
    children:
      - grass
      - dirt
      - sand
      - stone_floor
      - road
  vegetation:
    children:
      - flower
      - bush
      - tree
      - tall_grass
  water:
    children:
      - water
      - water_edge
      - waterfall
  structure:
    children:
      - wall
      - roof
      - fence
      - bridge
  decoration:
    children:
      - butterfly
      - sign
      - crate
      - barrel
```

AI chooses from the taxonomy or proposes new labels for review.

### 9.3 Rules map visual labels to behavior

Example:

```yaml
rules:
  - when:
      visual.has: grass
    apply:
      physical.walkable: true
      physical.collision: none
      rendering.layer: ground

  - when:
      visual.has: flower
    apply:
      physical.walkable: true
      gameplay.decorative: true
      rendering.layer: ground_detail

  - when:
      visual.has: butterfly
    apply:
      gameplay.decorative: true
      rendering.layer: overlay
      physical.collision: none

  - when:
      visual.has: wall
    apply:
      physical.walkable: false
      physical.collision: solid

  - when:
      visual.has: water
    apply:
      physical.walkable: false
      physical.collision: blocked
```

AI may detect “grass.” Project rules decide whether grass is walkable.

### 9.4 Review gates

Auto-approve low-risk metadata:

```text
obvious visual labels
duplicate groups
tile variants
transparency
frame rects
palette info
```

Require human review for:

```text
collision
walkability
interactability
quest relevance
damage zones
collectibility
resource semantics
progression blockers
navigation-critical metadata
```

### 9.5 Semantic overlays

Review should happen through generated overlays.

For tilesets:

```text
labels overlay
collision overlay
walkability overlay
render-layer overlay
uncertain labels overlay
```

For maps:

```text
preview
collision overlay
navigation overlay
semantic overlay
markers overlay
```

Agents consume JSON. Humans review images/contact sheets.

---

## 10. Map creation

The engine should allow agents to create maps from semantic descriptions, masks, or symbolic layouts.

### 10.1 Symbolic maps

Example:

```yaml
id: map.test_village
tileset: tileset.village
size: [20, 12]

legend:
  ".": tile.grass
  "#": tile.wall_stone
  "D": tile.door_wood
  "W": tile.water
  "P": marker.player_start
  "N": marker.npc_blacksmith

layers:
  ground:
    rows:
      - "...................."
      - "...######..........."
      - "...#....#..........."
      - "...#..D.#.....WW...."
      - "...######.....WW...."
      - "...................."
      - ".......P.....N......"
```

### 10.2 Map from mask image

A raw image can serve as a semantic mask:

```text
green = grass
gray = road
blue = water
black = wall
red = player spawn
yellow = quest marker
```

Command idea:

```bash
agentic2d map create-from-mask \
  --id map.village_test \
  --source assets/raw/maps/village_layout_mask.png \
  --palette content/map-palettes/village.palette.yaml \
  --tileset tileset.village
```

### 10.3 Tile resolver

Agents should request semantic tiles, not tile indices.

Example request:

```json
{
  "labels": ["grass", "flower"],
  "constraints": {
    "walkable": true,
    "layer": "ground_detail",
    "style": "forest"
  }
}
```

The engine resolves this to concrete tile variants.

### 10.4 Map validation

Validators should check:

```text
player spawn exists
required exits exist
markers exist
walkable area is connected
critical NPCs are reachable
doors connect valid regions
collision does not block spawn
camera bounds include playable region
no missing tile IDs
navigation graph can reach objectives
```

---

## 11. Animation workflow

Animations should be created from raw spritesheets or regions through structured commands.

Example:

```bash
agentic2d animation create-from-grid \
  --id animation.player.run \
  --texture texture.character.knight_sheet \
  --frame-size 32x32 \
  --frames 4..9 \
  --duration 4ticks \
  --loop true \
  --pivot bottom-center
```

Animation metadata should include:

```text
frame rects
duration
pivot
loop mode
events
hitboxes
hurtboxes
source provenance
```

### 11.1 Animation events

Events should be semantic:

```bash
agentic2d animation add-event \
  --animation animation.player.attack \
  --frame 3 \
  --event combat.hit_window_open
```

Validation should check:

```text
event exists
payload is valid
frame exists
hit window closes
audio cue exists
animation is compatible with entity
```

### 11.2 Hitboxes/hurtboxes

Hitboxes and hurtboxes should be explicit, visible, and validated.

Preview artifacts should include overlays, such as:

```text
attack_hitbox_overlay.png
```

---

## 12. Shader and material workflows

Shaders and materials must be first-class, validated assets.

### 12.1 Shader source model

Shaders should have source plus metadata/contract:

```yaml
id: shader.sprite_palette_swap
stage: fragment
inputs:
  - name: albedo
    type: texture2d
  - name: palette
    type: texture2d
  - name: paletteIndex
    type: int
outputs:
  - color
supports:
  batching: true
  atlasing: true
  premultipliedAlpha: true
preview:
  sprite: asset.preview.character_knight
```

### 12.2 Shader validation

Checks:

```text
compiles for all configured backends
required uniforms exist
uniform types match material schema
supports atlas UV transform if required
preserves alpha as expected
works with sprite batching
does not use forbidden backend/platform features
preview renders non-empty output
```

### 12.3 Material schema

Materials should be schema-defined:

```yaml
id: material.enemy_slime_poison
shader: shader.sprite_tint
properties:
  texture: texture.enemy_slime
  tint: "#77ff55"
  alpha: 1.0
```

Validators check:

```text
shader exists
texture exists
uniforms match shader contract
colors are valid
material can be packed
material has preview case
```

---

## 13. UI, fonts, audio

These were also identified as first-class content domains.

### 13.1 UI

UI should be declarative and semantically addressable:

```yaml
id: ui.inventory
layout:
  type: panel
  children:
    - id: ui.inventory.title
      type: text
      textKey: ui.inventory.title
    - id: ui.inventory.grid
      type: itemGrid
      columns: 5
    - id: ui.inventory.close
      type: button
      textKey: ui.close
      command: ui.close_panel
```

Stable UI control IDs are mandatory for semantic automation.

### 13.2 Fonts/localization

Validators should check:

```text
localization keys exist
glyphs are covered
text overflow in previews
font fallback policy
pixel font scale rules
```

### 13.3 Audio

Audio import metadata should define:

```text
source
kind: sfx/music/voice
format
channels
looping
bus
volume/loudness policy
```

Validators should check source existence, duration, loop flags, bus IDs, and target format support.

---

## 14. Behavior/scripting

We discussed scripting/behavior after asset workflows.

### 14.1 C# as default behavior language

Because the engine is .NET-based, C# is the default choice for behavior code.

Reasons:

```text
same language as engine
strong tooling
Roslyn analyzers
source generators
static typing
performance
debugging/refactoring
agent familiarity
easy integration with engine contracts
```

But we should avoid traditional unconstrained “scripts.”

Use the term:

```text
behavior modules
game behavior code
```

rather than free-form scripting.

### 14.2 F# as optional specialist language

F# is interesting for:

```text
state machines
rule-heavy systems
quest rules
dialogue conditions
scheduling
AI planning rules
domain modeling
```

But it should probably not be the default because:

```text
smaller game-dev ecosystem
less universal familiarity
more mixed-language project complexity
source generator ecosystem is more C#-centric
```

Recommended decision:

```text
C# as primary/default behavior language.
F# optional for specific rule/state-heavy modules.
```

### 14.3 Behavior should produce commands, not mutate world directly

Bad:

```csharp
player.Health -= 10;
world.Entities.Remove(enemy);
ui.InventoryPanel.Visible = true;
Random.Shared.Next();
DateTime.Now;
```

Good:

```csharp
ctx.Commands.Emit(new DamageCommand(target, amount));
ctx.Commands.Emit(new DespawnCommand(enemy));
ctx.Commands.Emit(new OpenPanelCommand(UiPanelIds.Inventory));
var roll = ctx.Random.NextInt(0, 100);
```

Behavior reads via queries and emits commands. The engine validates and applies commands. Events record outcomes.

### 14.4 Capability-based behavior context

Behavior code should not receive arbitrary `World`.

It should receive a constrained context:

```csharp
public readonly ref struct EnemyContext
{
    public EntityId Self { get; init; }
    public TickTime Time { get; init; }
    public IEnemyQueries Query { get; init; }
    public ICommandWriter Commands { get; init; }
    public IRandomSource Random { get; init; }
    public IDiagnosticWriter Diagnostics { get; init; }
}
```

Principle:

```text
Scripts get capabilities.
They do not get global engine power.
```

### 14.5 Analyzers should constrain behavior code

Agent-authored behavior should be policed with analyzers.

Potentially banned or restricted:

```text
System.IO
System.Net
DateTime.Now
Random.Shared
Thread
Task.Run
reflection
static mutable global state
direct World mutation
```

This gives architectural sandboxing without trying to fully sandbox .NET runtime execution.

### 14.6 Behavior layers

Not all behavior should be C#.

Recommended behavior tiers:

```text
Declarative data:
  item definitions, simple rules, tuning

Structured DSLs:
  dialogue, quests, schedules, cutscenes, animation transitions

C# behavior modules:
  complex gameplay behavior, AI, procedural systems

F# optional modules:
  rule-heavy/state-machine-heavy domains

Generated code:
  registries, bindings, IDs, serializers
```

Avoid writing everything as C# scripts.

---

## 15. Testing and validation strategy

### 15.1 Test/validation tiers

The engine should support layered validation:

```text
Tier 0: static validation
  formatting
  C# compile
  schema validation
  content reference validation

Tier 1: structural runtime validation
  load all maps/scenes
  instantiate critical content
  validate input/actions
  validate assets
  validate registries

Tier 2: smoke scenarios
  boot
  main menu if present
  new game
  load test map
  save/load

Tier 3: focused gameplay scenarios
  combat
  inventory
  dialogue
  quest
  pathfinding
  economy
  NPC schedule

Tier 4: long soak/performance tests
  many ticks
  random/autoplayer simulation
  memory/performance checks
```

### 15.2 Asset-level scenarios

Examples:

```yaml
id: asset.map.village_smoke
kind: map
map: map.village_test
assert:
  - markerExists: player_start
  - markerExists: town_exit
  - reachable:
      from: player_start
      to: town_exit
  - noMissingTiles: true
  - renderPreview: true
```

Animation scenario:

```yaml
id: asset.animation.player_attack_smoke
kind: animation
animation: animation.player.attack
assert:
  - frameCountGreaterThan: 1
  - hasEvent: combat.hit_window_open
  - hasEvent: combat.hit_window_close
  - hasHitboxDuringEvent: combat.hit_window_open
  - renderPreview: true
```

### 15.3 Same scenarios against debug/release

A key validation decision:

```text
Run the same scenarios against debug runtime and packaged runtime.
Assert equivalent accepted commands, significant events, and final queried state.
```

---

## 16. Possible high-level project structure

A possible repository/project structure:

```text
/src
  Agentic2D.Engine
  Agentic2D.Contracts
  Agentic2D.Runtime
  Agentic2D.Runtime.Debug
  Agentic2D.Runtime.Packaged
  Agentic2D.SourceGen
  Agentic2D.Tools
  Agentic2D.AssetPipeline
  Agentic2D.ScenarioRunner
  Agentic2D.Validation

/game
  /design
    /features
    /contracts
    /requirements

  /content
    /entities
    /components
    /items
    /quests
    /dialogue
    /ui
    /maps
    /tilesets
    /materials
    /animations
    /shaders
    /taxonomies
    /rules

  /assets
    /raw
      /textures
      /audio
      /fonts
      /packages
    /import
    /generated
      /atlases
      /compiled-shaders
      /previews
      /reports

  /behavior
    Game.Behavior.csproj
    /Enemies
    /Npcs
    /Systems

  /behavior-fsharp
    optional F# rule modules

  /scenarios
    /smoke
    /gameplay
    /ui
    /asset-import
    /visual
    /save-load

  /artifacts
    generated locally/CI, probably ignored
```

---

## 17. Current conceptual milestones

A likely next implementation sequence:

### Milestone 1: Minimal deterministic runtime

```text
fixed tick loop
entity IDs
component storage prototype
commands/events/queries
event log
scenario runner
JSON result artifact
```

### Milestone 2: Source-generated command/event/query metadata

```text
typed IDs
registries
debug serializers
dispatch table
scenario bindings
diagnostic descriptors
```

### Milestone 3: Asset inspection and PNG workflow

```text
raw PNG inspection
grid detection
texture import metadata
tileset creation
preview rendering
asset provenance
asset validation
```

### Milestone 4: Map authoring from semantic assets

```text
tileset semantic labels
taxonomy
rules mapping labels to behavior
symbolic map creation
map preview
collision/navigation overlays
map validation
scenario loads generated map
```

### Milestone 5: Animation authoring

```text
spritesheet import
animation frame ranges
animation events
hitbox/hurtbox metadata
animation preview/contact sheet
animated entity scenario
```

### Milestone 6: Behavior modules

```text
C# behavior assembly
capability-based behavior context
behavior emits commands
analyzers for banned APIs
source-generated behavior registry
behavior scenarios
```

### Milestone 7: Packaged runtime mode

```text
integer IDs
binary resources
generated dispatch
compiled asset bundles
debug/release scenario equivalence
```

---

## 18. Important open questions

We have not decided yet:

```text
Exact runtime base:
  MonoGame vs raylib-cs spike vs SDL3/Silk.NET custom platform.

Exact data format:
  YAML vs JSON vs custom DSL vs combination.
  YAML is readable but whitespace-sensitive; JSON is stricter but noisier.
  This was raised as a side question and remains open.

Exact component storage:
  simple component dictionaries first vs sparse sets vs archetype ECS.

Exact renderer:
  MonoGame SpriteBatch first vs custom renderer.

Exact shader language/compilation path:
  depends on runtime backend.

Exact asset schema language:
  likely JSON Schema or custom schema validation, but not finalized.

Exact behavior API shape:
  capability-based context agreed conceptually, concrete types not designed.

Exact source generator boundaries:
  agreed to start with metadata/plumbing, not semantics.

Exact AI vision integration:
  AI tags assets, but model/provider/tool interface not designed.
```

---

## 19. Key decisions made so far

The main decisions/leanings are:

```text
1. Build an agentic 2D engine concept, not a Godot plugin.

2. Engine should be headless-first and CLI/API-first.

3. No traditional visual editor is required initially.

4. Everything important should have stable IDs.

5. Scenario testing and validation are first-class engine features.

6. Runtime should be deterministic and inspectable.

7. Use a game-specific CQRS-like command/event/query model.

8. Avoid enterprise CQRS patterns.

9. Use ECS-like storage/composition, but do not overcommit to orthodox ECS yet.

10. Use source generators for deterministic plumbing.

11. Keep debug and packaged runtime semantically equivalent.

12. Prefer MonoGame as the likely serious .NET runtime candidate, with raylib-cs as a useful spike option.

13. Asset workflows are first-class, not peripheral.

14. AI should operate an asset workbench, not merely read asset package READMEs.

15. AI can propose semantic tags for raw assets, but approved metadata is the source of truth.

16. Separate visual labels from physical/gameplay behavior.

17. Use taxonomy + rules + validation + overlays for semantic asset curation.

18. C# is the default behavior language.

19. F# is optional for rule-heavy/state-machine-heavy modules.

20. Behavior code should read queries and emit commands, not mutate world directly.

21. Use analyzers to constrain agent-authored behavior code.

22. Human review remains necessary for design-sensitive semantics, visual quality, and gameplay feel.
```

---

## 20. One-sentence project thesis

> We are designing a .NET-based 2D game engine for agentic development, where humans author design intent and review artifacts while agents implement through structured commands, deterministic scenarios, semantic asset pipelines, validated behavior modules, source-generated runtime plumbing, and reproducible debug/release builds.
