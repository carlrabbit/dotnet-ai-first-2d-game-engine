using Agentic2D.Contracts;
using Agentic2D.Engine;

namespace Agentic2D.Tests.Unit;

public sealed class SmokeTests
{
    [Test]
    public async Task ContractsAndEngineAssembliesAreUsable()
    {
        var entityId = EntityId.Empty;
        var nextTick = Tick.Zero.Next();
        var markerType = typeof(EngineAssemblyMarker);

        await Assert.That(entityId.Value).IsEqualTo(Guid.Empty);
        await Assert.That(nextTick.Value).IsEqualTo(1UL);
        await Assert.That(markerType.Assembly.GetName().Name).IsEqualTo("Agentic2D.Engine");
    }
}
