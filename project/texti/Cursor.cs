namespace textiMain;

class Cursor
{
    public int Row { get; set; }
    public int Col { get; set; }

    public void MoveLeft(TextBuffer buffer)
    {
        if (Col > 0)
        {
            Col--;
        }
        else if (Row > 0)
        {
            Row--;
            Col = buffer.GetLineLength(Row);
        }
    }

    public void MoveRight(TextBuffer buffer)
    {
        if (Col < buffer.GetLineLength(Row))
        {
            Col++;
        }
        else if (Row < buffer.LineCount - 1)
        {
            Row++;
            Col = 0;
        }
    }

    public void MoveUp(TextBuffer buffer)
    {
        if (Row > 0)
        {
            Row--;
            Col = Math.Min(Col, buffer.GetLineLength(Row));
        }
    }

    public void MoveDown(TextBuffer buffer)
    {
        if (Row < buffer.LineCount - 1)
        {
            Row++;
            Col = Math.Min(Col, buffer.GetLineLength(Row));
        }
    }

    public void MoveToStart()
    {
        Row = 0;
        Col = 0;
    }

    public void MoveToEnd(TextBuffer buffer)
    {
        Row = buffer.LineCount - 1;
        Col = buffer.GetLineLength(Row);
    }

    public void Clamp(TextBuffer buffer)
    {
        if (Row < 0) Row = 0;
        if (Row >= buffer.LineCount) Row = buffer.LineCount - 1;
        if (Col < 0) Col = 0;
        if (Col > buffer.GetLineLength(Row)) Col = buffer.GetLineLength(Row);
    }
}
