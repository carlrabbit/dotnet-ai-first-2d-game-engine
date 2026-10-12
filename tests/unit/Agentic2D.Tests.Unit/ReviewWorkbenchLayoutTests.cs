using Agentic2D.Tools;

namespace Agentic2D.Tests.Unit;

public sealed class ReviewWorkbenchLayoutTests
{
    [Test]
    public async Task PendingStatusHasDedicatedSpaceBesideNavigationAndAboveQuestion()
    {
        var question = ReviewSurfaceLayout.WorkbenchQuestion("Is the new UI readable and easy to navigate, with focus and operation feedback easy to perceive?");
        var status = ReviewSurfaceLayout.WorkbenchStatus;

        await Assert.That(status.Inside(ReviewSurfaceLayout.WindowWidth, ReviewSurfaceLayout.WindowHeight)).IsTrue();
        await Assert.That(status.Intersects(ReviewSurfaceLayout.WorkbenchPrevious)).IsFalse();
        await Assert.That(status.Intersects(ReviewSurfaceLayout.WorkbenchOrdinal)).IsFalse();
        await Assert.That(status.Intersects(ReviewSurfaceLayout.WorkbenchNext)).IsFalse();
        await Assert.That(status.Intersects(question.Bounds)).IsFalse();
        await Assert.That(question.Bounds.Intersects(ReviewSurfaceLayout.WorkbenchContent)).IsFalse();
    }
}
