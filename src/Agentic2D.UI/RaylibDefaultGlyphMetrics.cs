using System.Collections.ObjectModel;

namespace Agentic2D.UI;

/// <summary>Glyph rectangle widths for Raylib 6.0's built-in 10-pixel font, pinned as portable layout data.</summary>
internal static class RaylibDefaultGlyphMetrics
{
    private static readonly int[] Widths = [
        3,1,4,6,5,7,6,2,3,3,5,5,2,4,1,7,5,2,5,5,5,5,5,5,5,5,1,1,3,4,3,6,
        7,6,6,6,6,6,6,6,6,3,5,6,5,7,6,6,6,6,6,6,7,6,7,7,6,6,6,2,7,2,3,5,
        2,5,5,5,5,5,4,5,5,1,2,5,2,5,5,5,5,5,5,5,4,5,5,5,5,5,5,3,1,3,4,4,
        1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
        1,1,5,5,5,7,1,5,3,7,3,5,4,1,7,4,3,5,3,3,2,5,6,1,2,2,3,5,6,6,6,6,
        6,6,6,6,6,6,7,6,6,6,6,6,3,3,3,3,7,6,6,6,6,6,6,5,6,6,6,6,6,6,4,6,
        5,5,5,5,5,5,9,5,5,5,5,5,2,2,3,3,5,5,5,5,5,5,5,5,5,5,5,5,5,5,3,5
    ];

    internal static IReadOnlyDictionary<int, double> Advances { get; } = new ReadOnlyDictionary<int, double>(
        Enumerable.Range(32, Widths.Length).ToDictionary(codePoint => codePoint, codePoint => (double)Widths[codePoint - 32]));
}
