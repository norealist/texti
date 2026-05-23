namespace textiMain;

class SearchMode
{
    public bool IsActive { get; private set; }
    public string Query { get; private set; } = "";
    public List<(int Row, int Col, int Length)> Matches { get; } = new();
    public int CurrentMatchIndex { get; private set; } = -1;

    public void Open()
    {
        IsActive = true;
        Query = "";
        Matches.Clear();
        CurrentMatchIndex = -1;
    }

    public void Close()
    {
        IsActive = false;
        Query = "";
        Matches.Clear();
        CurrentMatchIndex = -1;
    }

    public void AddChar(char c)
    {
        Query += c;
    }

    public void RemoveChar()
    {
        if (Query.Length > 0)
            Query = Query[..^1];
    }

    /// <summary>
    /// Scan every line for occurrences of Query (case-insensitive).
    /// </summary>
    public void FindAll(TextBuffer buffer)
    {
        Matches.Clear();
        CurrentMatchIndex = -1;

        if (string.IsNullOrEmpty(Query)) return;

        for (int row = 0; row < buffer.LineCount; row++)
        {
            string line = buffer.GetLine(row);
            int idx = 0;
            while ((idx = line.IndexOf(Query, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                Matches.Add((row, idx, Query.Length));
                idx += Math.Max(1, Query.Length);
            }
        }

        if (Matches.Count > 0)
            CurrentMatchIndex = 0;
    }

    public void NextMatch()
    {
        if (Matches.Count == 0) return;
        CurrentMatchIndex = (CurrentMatchIndex + 1) % Matches.Count;
    }

    public void PrevMatch()
    {
        if (Matches.Count == 0) return;
        CurrentMatchIndex = (CurrentMatchIndex - 1 + Matches.Count) % Matches.Count;
    }

    public (int Row, int Col)? GetCurrentMatchPosition()
    {
        if (CurrentMatchIndex < 0 || CurrentMatchIndex >= Matches.Count)
            return null;
        var m = Matches[CurrentMatchIndex];
        return (m.Row, m.Col);
    }
}
