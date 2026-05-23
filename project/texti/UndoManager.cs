namespace textiMain;

class UndoManager
{
    private sealed record Snapshot(List<string> Lines, int CursorRow, int CursorCol);

    private readonly Stack<Snapshot> _stack = new();
    private DateTime _lastTime = DateTime.MinValue;
    private bool _lastWasTyping;

    /// <summary>
    /// Saves a snapshot of the current state before a modification.
    /// Consecutive typing actions within 500 ms are grouped into one snapshot.
    /// </summary>
    public void SaveSnapshot(TextBuffer buffer, Cursor cursor, bool isTyping = false)
    {
        if (isTyping && _lastWasTyping &&
            (DateTime.Now - _lastTime).TotalMilliseconds < 500)
        {
            _lastTime = DateTime.Now;
            return; // group consecutive typing
        }

        _stack.Push(new Snapshot(buffer.GetLinesCopy(), cursor.Row, cursor.Col));
        _lastTime = DateTime.Now;
        _lastWasTyping = isTyping;
    }

    /// <summary>
    /// Restores the most recent snapshot. Returns true if an undo was performed.
    /// </summary>
    public bool Undo(TextBuffer buffer, Cursor cursor)
    {
        if (_stack.Count == 0) return false;

        var snap = _stack.Pop();
        buffer.SetLines(snap.Lines);
        cursor.Row = snap.CursorRow;
        cursor.Col = snap.CursorCol;
        cursor.Clamp(buffer);
        _lastWasTyping = false;
        return true;
    }

    public void Clear()
    {
        _stack.Clear();
        _lastWasTyping = false;
    }
}
