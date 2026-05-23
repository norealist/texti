namespace textiMain;

using System.Text;

class TextBuffer
{
    private List<string> _lines = new() { "" };

    public int LineCount => _lines.Count;

    public string GetLine(int index)
    {
        if (index < 0 || index >= _lines.Count) return "";
        return _lines[index];
    }

    public int GetLineLength(int index)
    {
        return GetLine(index).Length;
    }

    public void SetFromText(string text)
    {
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");
        _lines = new List<string>(text.Split('\n'));
        if (_lines.Count == 0) _lines.Add("");
    }

    public string GetAllText()
    {
        return string.Join("\n", _lines);
    }

    public List<string> GetLinesCopy()
    {
        return new List<string>(_lines);
    }

    public void SetLines(List<string> lines)
    {
        _lines = new List<string>(lines);
        if (_lines.Count == 0) _lines.Add("");
    }

    // ── Single-character operations ──────────────────────────────

    public void InsertChar(int row, int col, char c)
    {
        if (row < 0 || row >= _lines.Count) return;
        string line = _lines[row];
        col = Math.Clamp(col, 0, line.Length);
        _lines[row] = line.Insert(col, c.ToString());
    }

    public void DeleteCharBefore(int row, int col)
    {
        if (col > 0)
        {
            string line = _lines[row];
            _lines[row] = line.Remove(col - 1, 1);
        }
        else if (row > 0)
        {
            // Merge current line into previous
            _lines[row - 1] += _lines[row];
            _lines.RemoveAt(row);
        }
    }

    public void DeleteCharAt(int row, int col)
    {
        string line = _lines[row];
        if (col < line.Length)
        {
            _lines[row] = line.Remove(col, 1);
        }
        else if (row < _lines.Count - 1)
        {
            // Merge next line into current
            _lines[row] += _lines[row + 1];
            _lines.RemoveAt(row + 1);
        }
    }

    // ── Line operations ──────────────────────────────────────────

    public void InsertNewLine(int row, int col)
    {
        string line = _lines[row];
        col = Math.Clamp(col, 0, line.Length);
        string before = line[..col];
        string after = line[col..];
        _lines[row] = before;
        _lines.Insert(row + 1, after);
    }

    // ── Multi-line text insert ───────────────────────────────────

    public void InsertText(int row, int col, string text)
    {
        if (row < 0 || row >= _lines.Count) return;

        string[] parts = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        string currentLine = _lines[row];
        col = Math.Clamp(col, 0, currentLine.Length);

        string before = currentLine[..col];
        string after = currentLine[col..];

        if (parts.Length == 1)
        {
            _lines[row] = before + parts[0] + after;
        }
        else
        {
            _lines[row] = before + parts[0];
            for (int i = 1; i < parts.Length - 1; i++)
            {
                _lines.Insert(row + i, parts[i]);
            }
            _lines.Insert(row + parts.Length - 1, parts[^1] + after);
        }
    }

    // ── Range operations ─────────────────────────────────────────

    public string GetTextInRange((int Row, int Col) start, (int Row, int Col) end)
    {
        if (start.Row == end.Row)
        {
            string line = _lines[start.Row];
            int s = Math.Clamp(start.Col, 0, line.Length);
            int e = Math.Clamp(end.Col, 0, line.Length);
            return line[s..e];
        }

        var sb = new StringBuilder();
        sb.Append(_lines[start.Row][Math.Min(start.Col, _lines[start.Row].Length)..]);

        for (int i = start.Row + 1; i < end.Row; i++)
        {
            sb.Append('\n');
            sb.Append(_lines[i]);
        }

        sb.Append('\n');
        sb.Append(_lines[end.Row][..Math.Min(end.Col, _lines[end.Row].Length)]);

        return sb.ToString();
    }

    public void DeleteRange((int Row, int Col) start, (int Row, int Col) end)
    {
        if (start.Row == end.Row)
        {
            string line = _lines[start.Row];
            int s = Math.Clamp(start.Col, 0, line.Length);
            int e = Math.Clamp(end.Col, 0, line.Length);
            _lines[start.Row] = line[..s] + line[e..];
        }
        else
        {
            string head = _lines[start.Row][..Math.Min(start.Col, _lines[start.Row].Length)];
            string tail = _lines[end.Row][Math.Min(end.Col, _lines[end.Row].Length)..];

            for (int i = end.Row; i > start.Row; i--)
                _lines.RemoveAt(i);

            _lines[start.Row] = head + tail;
        }
    }
}
