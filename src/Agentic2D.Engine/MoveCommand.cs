using Agentic2D.Contracts;

namespace Agentic2D.Engine;

public sealed record MoveCommand(EntityId EntityId, int Amount);
