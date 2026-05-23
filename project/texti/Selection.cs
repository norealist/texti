namespace textiMain;

class Selection
{
    public int AnchorRow { get; private set; }
    public int AnchorCol { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// Begin or continue a selection from the given anchor position.
    /// Called once when Shift is first held down; subsequent Shift+moves
    /// keep the same anchor.
    /// </summary>
    public void Start(int row, int col)
    {
        if (!IsActive)
        {
            AnchorRow = row;
            AnchorCol = col;
            IsActive = true;
        }
    }

    public void Clear()
    {
        IsActive = false;
    }

    /// <summary>
    /// Returns the ordered (start, end) positions of the selection
    /// where Start ≤ End in document order.
    /// </summary>
    public ((int Row, int Col) Start, (int Row, int Col) End) GetRange(Cursor cursor)
    {
        var anchor = (Row: AnchorRow, Col: AnchorCol);
        var caret  = (Row: cursor.Row, Col: cursor.Col);

        if (anchor.Row < caret.Row || (anchor.Row == caret.Row && anchor.Col <= caret.Col))
            return (anchor, caret);

        return (caret, anchor);
    }

    public string GetSelectedText(TextBuffer buffer, Cursor cursor)
    {
        if (!IsActive) return "";
        var range = GetRange(cursor);
        return buffer.GetTextInRange(range.Start, range.End);
    }
}
