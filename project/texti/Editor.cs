namespace textiMain;

using System.Text;

class Editor
{
    private readonly TextBuffer  _buffer    = new();
    private readonly Cursor      _cursor    = new();
    private readonly Selection   _selection = new();
    private readonly UndoManager _undo      = new();
    private readonly SearchMode  _search    = new();
    private readonly Renderer    _renderer  = new();

    private readonly string _filePath;
    private readonly string _fileName;

    private bool   _isModified;
    private string _statusMessage = "";
    private DateTime _statusTime = DateTime.MinValue;

    // ─────────────────────────────────────────────────────────────

    public Editor(string filePath)
    {
        _filePath = Path.GetFullPath(filePath);
        _fileName = Path.GetFileName(_filePath);

        if (File.Exists(_filePath))
        {
            string content = File.ReadAllText(_filePath, Encoding.UTF8);
            _buffer.SetFromText(content);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Main loop
    // ─────────────────────────────────────────────────────────────

    public void Run()
    {
        Console.CursorVisible = false;
        Console.TreatControlCAsInput = true;
        Console.Clear();
        Console.Write("\x1b[3J");

        try
        {
            while (true)
            {
                ClearExpiredStatus();
                _renderer.Render(_buffer, _cursor, _selection, _search,
                                 _statusMessage, _isModified, _fileName);

                var key = Console.ReadKey(true);

                // ── Search mode intercepts all keys ─────────
                if (_search.IsActive)
                {
                    HandleSearchKey(key);
                    continue;
                }

                bool ctrl  = (key.Modifiers & ConsoleModifiers.Control) != 0;
                bool shift = (key.Modifiers & ConsoleModifiers.Shift)   != 0;

                // ── Ctrl+<key> shortcuts ─────────────────────
                // We check both key.Key and key.KeyChar (e.g. \x13 for Ctrl+S) to support various terminals & layouts.
                bool isCtrlS = (ctrl && key.Key == ConsoleKey.S) || key.KeyChar == '\x13';
                bool isCtrlF = (ctrl && key.Key == ConsoleKey.F) || key.KeyChar == '\x06';
                bool isCtrlZ = (ctrl && key.Key == ConsoleKey.Z) || key.KeyChar == '\x1a';
                bool isCtrlC = (ctrl && key.Key == ConsoleKey.C) || key.KeyChar == '\x03';
                bool isCtrlX = (ctrl && key.Key == ConsoleKey.X) || key.KeyChar == '\x18';
                bool isCtrlV = (ctrl && key.Key == ConsoleKey.V) || key.KeyChar == '\x16';

                if (isCtrlS || isCtrlF || isCtrlZ || isCtrlC || isCtrlX || isCtrlV)
                {
                    if (isCtrlS) Save();
                    else if (isCtrlF) _search.Open();
                    else if (isCtrlZ) Undo();
                    else if (isCtrlC) Copy();
                    else if (isCtrlX) Cut();
                    else if (isCtrlV) Paste();
                    continue;
                }

                // ── Regular keys ─────────────────────────────
                switch (key.Key)
                {
                    case ConsoleKey.LeftArrow:
                        HandleMove(() => _cursor.MoveLeft(_buffer), shift);
                        break;

                    case ConsoleKey.RightArrow:
                        HandleMove(() => _cursor.MoveRight(_buffer), shift);
                        break;

                    case ConsoleKey.UpArrow:
                        HandleMove(() => _cursor.MoveUp(_buffer), shift);
                        break;

                    case ConsoleKey.DownArrow:
                        HandleMove(() => _cursor.MoveDown(_buffer), shift);
                        break;

                    case ConsoleKey.Home:
                        HandleMove(() => _cursor.MoveToStart(), shift);
                        break;

                    case ConsoleKey.End:
                        HandleMove(() => _cursor.MoveToEnd(_buffer), shift);
                        break;

                    case ConsoleKey.Backspace:
                        HandleBackspace();
                        break;

                    case ConsoleKey.Delete:
                        HandleDelete();
                        break;

                    case ConsoleKey.Enter:
                        HandleEnter();
                        break;

                    case ConsoleKey.Tab:
                        HandleTab();
                        break;

                    case ConsoleKey.F2:
                        Save();
                        break;

                    case ConsoleKey.Escape:
                        if (HandleEscape()) return;
                        break;

                    default:
                        if (!char.IsControl(key.KeyChar) && key.KeyChar != '\0')
                            InsertChar(key.KeyChar);
                        break;
                }
            }
        }
        finally
        {
            Cleanup();
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Cursor movement (with optional Shift-selection)
    // ─────────────────────────────────────────────────────────────

    private void HandleMove(Action moveAction, bool shift)
    {
        if (shift)
            _selection.Start(_cursor.Row, _cursor.Col);
        else
            _selection.Clear();

        moveAction();
    }

    // ─────────────────────────────────────────────────────────────
    //  Text editing
    // ─────────────────────────────────────────────────────────────

    private void InsertChar(char c)
    {
        DeleteSelectionIfActive();
        _undo.SaveSnapshot(_buffer, _cursor, isTyping: true);
        _buffer.InsertChar(_cursor.Row, _cursor.Col, c);
        _cursor.Col++;
        _isModified = true;
    }

    private void HandleBackspace()
    {
        if (_selection.IsActive) { DeleteSelectionIfActive(); return; }
        if (_cursor.Row == 0 && _cursor.Col == 0) return;

        _undo.SaveSnapshot(_buffer, _cursor);

        if (_cursor.Col > 0)
        {
            _buffer.DeleteCharBefore(_cursor.Row, _cursor.Col);
            _cursor.Col--;
        }
        else
        {
            int prevLen = _buffer.GetLineLength(_cursor.Row - 1);
            _buffer.DeleteCharBefore(_cursor.Row, _cursor.Col);
            _cursor.Row--;
            _cursor.Col = prevLen;
        }

        _isModified = true;
    }

    private void HandleDelete()
    {
        if (_selection.IsActive) { DeleteSelectionIfActive(); return; }

        int lastRow = _buffer.LineCount - 1;
        if (_cursor.Row == lastRow && _cursor.Col == _buffer.GetLineLength(lastRow))
            return;

        _undo.SaveSnapshot(_buffer, _cursor);
        _buffer.DeleteCharAt(_cursor.Row, _cursor.Col);
        _isModified = true;
    }

    private void HandleEnter()
    {
        DeleteSelectionIfActive();
        _undo.SaveSnapshot(_buffer, _cursor);
        _buffer.InsertNewLine(_cursor.Row, _cursor.Col);
        _cursor.Row++;
        _cursor.Col = 0;
        _isModified = true;
    }

    private void HandleTab()
    {
        DeleteSelectionIfActive();
        _undo.SaveSnapshot(_buffer, _cursor);
        for (int i = 0; i < 4; i++)
        {
            _buffer.InsertChar(_cursor.Row, _cursor.Col, ' ');
            _cursor.Col++;
        }
        _isModified = true;
    }

    private void DeleteSelectionIfActive()
    {
        if (!_selection.IsActive) return;

        _undo.SaveSnapshot(_buffer, _cursor);
        var range = _selection.GetRange(_cursor);
        _buffer.DeleteRange(range.Start, range.End);
        _cursor.Row = range.Start.Row;
        _cursor.Col = range.Start.Col;
        _selection.Clear();
        _isModified = true;
    }

    // ─────────────────────────────────────────────────────────────
    //  Clipboard
    // ─────────────────────────────────────────────────────────────

    private void Copy()
    {
        if (!_selection.IsActive) return;
        string text = _selection.GetSelectedText(_buffer, _cursor);
        ClipboardHelper.Copy(text);
        SetStatus("✓ Скопировано");
    }

    private void Cut()
    {
        if (!_selection.IsActive) return;
        string text = _selection.GetSelectedText(_buffer, _cursor);
        ClipboardHelper.Copy(text);
        DeleteSelectionIfActive();
        SetStatus("✓ Вырезано");
    }

    private void Paste()
    {
        string text = ClipboardHelper.Paste();
        if (string.IsNullOrEmpty(text)) return;

        DeleteSelectionIfActive();
        _undo.SaveSnapshot(_buffer, _cursor);

        string[] lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        _buffer.InsertText(_cursor.Row, _cursor.Col, text);

        if (lines.Length == 1)
        {
            _cursor.Col += lines[0].Length;
        }
        else
        {
            _cursor.Row += lines.Length - 1;
            _cursor.Col = lines[^1].Length;
        }

        _isModified = true;
        SetStatus("✓ Вставлено");
    }

    // ─────────────────────────────────────────────────────────────
    //  Undo
    // ─────────────────────────────────────────────────────────────

    private void Undo()
    {
        if (_undo.Undo(_buffer, _cursor))
        {
            _isModified = true;
            _selection.Clear();
            SetStatus("↩ Отменено");
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Save
    // ─────────────────────────────────────────────────────────────

    private void Save()
    {
        try
        {
            File.WriteAllText(_filePath, _buffer.GetAllText(), Encoding.UTF8);
            _isModified = false;
            SetStatus("✓ Файл сохранён!");
        }
        catch (Exception ex)
        {
            SetStatus($"✗ Ошибка: {ex.Message}");
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Exit
    // ─────────────────────────────────────────────────────────────

    /// <returns>true = exit the editor</returns>
    private bool HandleEscape()
    {
        if (!_isModified) return true;

        Renderer.RenderExitDialog(Console.WindowWidth, Console.WindowHeight);

        while (true)
        {
            var key = Console.ReadKey(true);

            char c = char.ToUpperInvariant(key.KeyChar);
            if (c == 'S' || c == 'Ы' || key.Key == ConsoleKey.S)
            {
                Save();
                return true;
            }
            if (c == 'D' || c == 'В' || key.Key == ConsoleKey.D)
            {
                return true;
            }
            if (c == 'C' || c == 'С' || key.Key == ConsoleKey.C)
            {
                return false;
            }

            if (key.Key == ConsoleKey.Escape)
                return false;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Search
    // ─────────────────────────────────────────────────────────────

    private void HandleSearchKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _search.Close();
                return;

            case ConsoleKey.Enter:
            case ConsoleKey.DownArrow:
                _search.NextMatch();
                JumpToCurrentMatch();
                return;

            case ConsoleKey.UpArrow:
                _search.PrevMatch();
                JumpToCurrentMatch();
                return;

            case ConsoleKey.Backspace:
                _search.RemoveChar();
                _search.FindAll(_buffer);
                JumpToCurrentMatch();
                return;

            default:
                if (!char.IsControl(key.KeyChar) && key.KeyChar != '\0')
                {
                    _search.AddChar(key.KeyChar);
                    _search.FindAll(_buffer);
                    JumpToCurrentMatch();
                }
                return;
        }
    }

    private void JumpToCurrentMatch()
    {
        var pos = _search.GetCurrentMatchPosition();
        if (pos.HasValue)
        {
            _cursor.Row = pos.Value.Row;
            _cursor.Col = pos.Value.Col;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────

    private void SetStatus(string message)
    {
        _statusMessage = message;
        _statusTime = DateTime.Now;
    }

    private void ClearExpiredStatus()
    {
        if (!string.IsNullOrEmpty(_statusMessage) &&
            (DateTime.Now - _statusTime).TotalSeconds > 2)
        {
            _statusMessage = "";
        }
    }

    private static void Cleanup()
    {
        Console.TreatControlCAsInput = false;
        Console.CursorVisible = true;
        Console.Write("\x1b[0m");
        Console.Clear();
        Console.Write("\x1b[3J");
    }
}
