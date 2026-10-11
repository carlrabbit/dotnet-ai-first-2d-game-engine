namespace Agentic2D.Tools;

public readonly record struct UiRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public bool Intersects(UiRect other) => X < other.Right && other.X < Right && Y < other.Bottom && other.Y < Bottom;
    public bool Inside(int width, int height) => X >= 0 && Y >= 0 && Right <= width && Bottom <= height;
}

public sealed record WrappedText(int FontSize, int LineHeight, IReadOnlyList<string> Lines, UiRect Bounds)
{
    public int Bottom => Bounds.Y + Lines.Count * LineHeight;
}

public static class ReviewSurfaceLayout
{
    public const int WindowWidth = 1120;
    public const int WindowHeight = 720;

    public static WrappedText WorkbenchQuestion(string subject)
    {
        for (var fontSize = 22; fontSize >= 14; fontSize -= 2)
        {
            var lineHeight = fontSize + 6;
            var lines = Wrap(subject, 1010, fontSize);
            if (lines.Count * lineHeight <= 92) return new(fontSize, lineHeight, lines, new(50, 132, 1010, 92));
        }

        var fallback = Wrap(subject, 1010, 14);
        return new(14, 20, fallback, new(50, 132, 1010, 92));
    }

    public static UiRect WorkbenchHeader => new(40, 18, 1040, 98);
    public static UiRect WorkbenchStatus => new(50, 112, 1010, 24);
    public static UiRect WorkbenchContent => new(50, 245, 1010, 290);
    public static UiRect WorkbenchPreviewLaunch => new(350, 430, 420, 60);
    public static UiRect WorkbenchPersistence => new(50, 548, 1010, 54);
    public static UiRect WorkbenchRestart => new(35, 620, 300, 72);
    public static UiRect WorkbenchReject => new(410, 620, 300, 72);
    public static UiRect WorkbenchAccept => new(780, 620, 300, 72);

    public static PreviewLayout AssetPreview(PreviewSurface surface)
    {
        var contextLines = new List<string>
        {
            $"{surface.Kind.ToString().ToUpperInvariant()} REVIEW · Candidate: {surface.Context.CandidateId}",
            $"Purpose: {surface.Context.PresentationRole} · Variant: {surface.Context.SelectedVariant}",
            $"Corrections: {(surface.Context.Corrections.Count == 0 ? "none" : string.Join(", ", surface.Context.Corrections))}",
            "Raw/base: " + surface.Context.RawMeaning,
            "Processed/current-draft: " + surface.Context.ProcessedMeaning,
            "Question: " + surface.Context.SubjectiveQuestion
        };
        var context = new WrappedText(15, 19, contextLines.SelectMany(line => Wrap(line, 1064, 15)).ToArray(), new(28, 18, 1064, 176));
        var content = new UiRect(28, 205, 620, 405);
        var controls = new UiRect(680, 205, 400, Math.Max(120, 52 * ((surface.Controls.Count + 1) / 2) + 38));
        return new(context, content, controls, new(680, 620, 400, 32));
    }

    public sealed record PreviewLayout(WrappedText Context, UiRect Content, UiRect Controls, UiRect Footer)
    {
        public bool IsInsideWindow => Context.Bounds.Inside(WindowWidth, WindowHeight) && Content.Inside(WindowWidth, WindowHeight) && Controls.Inside(WindowWidth, WindowHeight) && Footer.Inside(WindowWidth, WindowHeight);
        public bool ContextDoesNotOverlapControls => !Context.Bounds.Intersects(Controls);
        public bool ContextDoesNotOverlapContent => !Context.Bounds.Intersects(Content);
    }

    public static IReadOnlyList<string> Wrap(string text, int width, int fontSize)
    {
        var maxChars = Math.Max(1, (int)(width / (fontSize * .56f)));
        var lines = new List<string>();
        foreach (var paragraph in (text ?? string.Empty).Split('\n'))
        {
            var line = string.Empty;
            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (word.Length > maxChars)
                {
                    if (line.Length > 0) { lines.Add(line); line = string.Empty; }
                    for (var index = 0; index < word.Length; index += maxChars) lines.Add(word.Substring(index, Math.Min(maxChars, word.Length - index)));
                    continue;
                }
                var candidate = string.IsNullOrEmpty(line) ? word : line + " " + word;
                if (candidate.Length > maxChars && line.Length > 0) { lines.Add(line); line = word; } else line = candidate;
            }
            if (line.Length > 0 || lines.Count == 0) lines.Add(line);
        }
        return lines;
    }
}
