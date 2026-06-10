# Behavior Modules

## Authority

This document is authoritative for initial behavior/scripting principles.

## Primary language

C# is the default behavior language because the engine is .NET-based and can use Roslyn analyzers, source generators, static typing, and direct engine contracts.

F# may be added later for rule-heavy or state-machine-heavy modules.

## Behavior rule

Behavior modules read queries and emit commands. They do not mutate world state directly.

Preferred model:

```csharp
ctx.Commands.Emit(new DamageCommand(target, amount));
var roll = ctx.Random.NextInt(0, 100);
```

Avoid:

```csharp
player.Health -= 10;
Random.Shared.Next();
DateTime.Now;
```

## Analyzer direction

Future analyzers should restrict direct world mutation, uncontrolled IO/network access, global randomness, wall-clock time, reflection, static mutable state, unmanaged threading, and other non-deterministic behavior in behavior modules.
