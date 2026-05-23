namespace textiMain;

using System.Text;
using System.Text.RegularExpressions;

class Renderer
{
    // ── ANSI escape codes ────────────────────────────────────────
    private const string Reset   = "\x1b[0m";
    private const string Bold    = "\x1b[1m";
    private const string Invert  = "\x1b[7m";

    // 256-colour palette codes
    private const string HeaderBg   = "\x1b[48;5;235m";   // dark gray bar
    private const string HeaderFg   = "\x1b[38;5;252m";   // light gray text
    private const string AccentFg   = "\x1b[38;5;75m";    // cyan-blue "Texti"
    private const string ModifiedFg = "\x1b[38;5;214m";   // orange dot
    private const string GrayFg     = "\x1b[38;5;240m";   // dim tilde
    private const string GreenFg    = "\x1b[38;5;114m";   // success messages
    private const string StatusFg   = "\x1b[38;5;250m";   // status bar text
    private const string ShortcutFg = "\x1b[38;5;75m";    // shortcut keys
    private const string DimFg      = "\x1b[38;5;243m";   // dim separators
    private const string SelectBg   = "\x1b[48;5;25m\x1b[38;5;255m";  // blue bg + white fg
    private const string MatchBg    = "\x1b[48;5;136m\x1b[30m";       // yellow-brown bg
    private const string CurMatchBg = "\x1b[48;5;214m\x1b[30m";       // bright orange bg
    private const string SearchBg   = "\x1b[48;5;238m";   // search input bg
    private const string WarningFg  = "\x1b[38;5;203m";   // red for warnings
    private const string YellowFg   = "\x1b[38;5;228m";   // yellow for options

    private int _scrollRow;
    private int _scrollCol;

    public int ScrollRow => _scrollRow;

    // ─────────────────────────────────────────────────────────────
    //  Main render
    // ─────────────────────────────────────────────────────────────

    public void Render(TextBuffer buffer, Cursor cursor, Selection selection,
                       SearchMode search, string statusMessage,
                       bool isModified, string fileName)
    {
        int width  = Math.Max(Console.WindowWidth, 20);
        int height = Math.Max(Console.WindowHeight, 5);
        int contentHeight = height - 2; // header + status bar

        UpdateScroll(cursor, contentHeight, width);

        var sb = new StringBuilder(width * height * 3);
        sb.Append("\x1b[?25l");  // hide hardware cursor
        sb.Append("\x1b[H");     // home

        // ── Header bar ──────────────────────────────────────────
        RenderHeader(sb, width, fileName, isModified);

        // ── Content area ────────────────────────────────────────
        for (int screenRow = 0; screenRow < contentHeight; screenRow++)
        {
            int lineIdx = _scrollRow + screenRow;
            RenderContentLine(sb, lineIdx, width, buffer, cursor, selection, search);
        }

        // ── Status / search bar ─────────────────────────────────
        if (search.IsActive)
            RenderSearchBar(sb, width, search, cursor);
        else
            RenderStatusBar(sb, width, cursor, statusMessage, isModified);

        sb.Append("\x1b[J"); // clear anything below
        Console.Write(sb.ToString());
    }

    // ─────────────────────────────────────────────────────────────
    //  Header
    // ─────────────────────────────────────────────────────────────

    private static void RenderHeader(StringBuilder sb, int width,
                                     string fileName, bool isModified)
    {
        string left  = $" {fileName}";
        string mod   = isModified ? " •" : "";
        string right = "Texti ";

        sb.Append(HeaderBg);
        sb.Append(HeaderFg);
        sb.Append(left);

        if (isModified)
        {
            sb.Append(ModifiedFg);
            sb.Append(mod);
            sb.Append(HeaderFg);
        }

        int fill = width - StripLen(left) - StripLen(mod) - StripLen(right);
        if (fill > 0) sb.Append(' ', fill);

        sb.Append(AccentFg);
        sb.Append(Bold);
        sb.Append(right);
        sb.Append(Reset);
        sb.Append("\x1b[K\n");
    }

    // ─────────────────────────────────────────────────────────────
    //  Content line
    // ─────────────────────────────────────────────────────────────

    private void RenderContentLine(StringBuilder sb, int lineIdx, int width,
                                   TextBuffer buffer, Cursor cursor,
                                   Selection selection, SearchMode search)
    {
        if (lineIdx < buffer.LineCount)
        {
            string line = buffer.GetLine(lineIdx);
            string? prevStyle = null;

            for (int screenCol = 0; screenCol < width; screenCol++)
            {
                int charCol = _scrollCol + screenCol;
                char ch = charCol < line.Length ? line[charCol] : ' ';

                string style = GetCharStyle(lineIdx, charCol, cursor, selection, search);

                if (style != prevStyle)
                {
                    if (prevStyle != null) sb.Append(Reset);
                    if (style.Length > 0) sb.Append(style);
                    prevStyle = style;
                }

                sb.Append(ch);
            }

            if (prevStyle != null && prevStyle.Length > 0) sb.Append(Reset);
        }
        else
        {
            // Below the last text line — show tilde
            sb.Append(GrayFg);
            sb.Append('~');
            sb.Append(Reset);
            if (width > 1) sb.Append(' ', width - 1);
        }

        sb.Append("\x1b[K\n");
    }

    // ─────────────────────────────────────────────────────────────
    //  Per-character style selection
    // ─────────────────────────────────────────────────────────────

    private static string GetCharStyle(int row, int col,
                                       Cursor cursor, Selection selection,
                                       SearchMode search)
    {
        bool isCursor = (row == cursor.Row && col == cursor.Col);

        if (isCursor)
            return Invert;

        if (selection.IsActive && IsInSelection(row, col, selection, cursor))
            return SelectBg;

        if (search.IsActive && IsSearchMatch(row, col, search, out bool isCurrent))
            return isCurrent ? CurMatchBg + Bold : MatchBg;

        return "";
    }

    private static bool IsInSelection(int row, int col,
                                      Selection sel, Cursor cursor)
    {
        var (start, end) = sel.GetRange(cursor);

        if (row < start.Row || row > end.Row) return false;
        if (start.Row == end.Row)
            return col >= start.Col && col < end.Col;
        if (row == start.Row) return col >= start.Col;
        if (row == end.Row)   return col < end.Col;
        return true;  // middle row — fully selected
    }

    private static bool IsSearchMatch(int row, int col,
                                      SearchMode search, out bool isCurrent)
    {
        isCurrent = false;
        for (int i = 0; i < search.Matches.Count; i++)
        {
            var m = search.Matches[i];
            if (m.Row == row && col >= m.Col && col < m.Col + m.Length)
            {
                isCurrent = (i == search.CurrentMatchIndex);
                return true;
            }
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────────
    //  Status bar (normal mode)
    // ─────────────────────────────────────────────────────────────

    private static void RenderStatusBar(StringBuilder sb, int width,
                                        Cursor cursor, string statusMessage,
                                        bool isModified)
    {
        sb.Append(HeaderBg);

        // Left part: position or status message
        string left;
        if (!string.IsNullOrEmpty(statusMessage))
            left = $" {GreenFg}{statusMessage}{StatusFg}";
        else
            left = $" {StatusFg}Стр {cursor.Row + 1} : Кол {cursor.Col + 1}";

        if (isModified && string.IsNullOrEmpty(statusMessage))
            left += $" {ModifiedFg}[изменён]{StatusFg}";

        // Right part: shortcuts
        string right =
            $"{DimFg}│ {ShortcutFg}Ctrl+S/F2 {DimFg}Сохр {DimFg}│ " +
            $"{ShortcutFg}Ctrl+F {DimFg}Поиск {DimFg}│ " +
            $"{ShortcutFg}Ctrl+Z {DimFg}Отмена {DimFg}│ " +
            $"{ShortcutFg}Esc {DimFg}Выход ";

        sb.Append(left);

        int leftLen = StripAnsiLen(left);
        int rightLen = StripAnsiLen(right);
        int gap = width - leftLen - rightLen;
        if (gap > 0) sb.Append(' ', gap);

        sb.Append(right);
        sb.Append(Reset);
        sb.Append("\x1b[K");
    }

    // ─────────────────────────────────────────────────────────────
    //  Search bar
    // ─────────────────────────────────────────────────────────────

    private static void RenderSearchBar(StringBuilder sb, int width,
                                        SearchMode search, Cursor cursor)
    {
        sb.Append(SearchBg);
        sb.Append(StatusFg);

        string matchInfo = search.Matches.Count > 0
            ? $" {ShortcutFg}({search.CurrentMatchIndex + 1}/{search.Matches.Count})"
            : (search.Query.Length > 0 ? $" {WarningFg}(нет совпадений)" : "");

        string left = $" 🔍 Поиск: {Reset}{SearchBg}\x1b[97m{search.Query}" +
                      $"{StatusFg}{matchInfo}";

        string right = $"{DimFg}Enter{StatusFg}-след. {DimFg}│ " +
                       $"{DimFg}↑↓{StatusFg}-навиг. {DimFg}│ " +
                       $"{ShortcutFg}Esc{StatusFg}-закрыть ";

        sb.Append(left);

        int leftLen  = StripAnsiLen(left);
        int rightLen = StripAnsiLen(right);
        int gap = width - leftLen - rightLen;
        if (gap > 0) sb.Append(' ', gap);

        sb.Append(right);
        sb.Append(Reset);
        sb.Append("\x1b[K");
    }

    // ─────────────────────────────────────────────────────────────
    //  Exit dialog (overlay)
    // ─────────────────────────────────────────────────────────────

    public static void RenderExitDialog(int width, int height)
    {
        const int boxW = 44;
        const int boxH = 8;
        int startRow = Math.Max(1, (height - boxH) / 2);
        int startCol = Math.Max(1, (width - boxW) / 2);

        string border = $"{AccentFg}";
        string top    = $"┌{'─'.Repeat(boxW - 2)}┐";
        string bot    = $"└{'─'.Repeat(boxW - 2)}┘";

        string[] lines = new[]
        {
            top,
            MakeLine(boxW, $"  {Bold}\x1b[97mФайл изменён!{Reset}{border}"),
            MakeLine(boxW, ""),
            MakeLine(boxW, $"  {GreenFg}[S]{Reset}{border} Сохранить и выйти"),
            MakeLine(boxW, $"  {WarningFg}[D]{Reset}{border} Выйти без сохранения"),
            MakeLine(boxW, $"  {YellowFg}[C]{Reset}{border} Отмена — остаться"),
            MakeLine(boxW, ""),
            bot,
        };

        var sb = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            sb.Append($"\x1b[{startRow + i};{startCol}H");
            sb.Append(border);
            sb.Append(lines[i]);
            sb.Append(Reset);
        }

        Console.Write(sb.ToString());
    }

    // ─────────────────────────────────────────────────────────────
    //  Scroll management
    // ─────────────────────────────────────────────────────────────

    private void UpdateScroll(Cursor cursor, int contentHeight, int contentWidth)
    {
        if (cursor.Row < _scrollRow)
            _scrollRow = cursor.Row;
        if (cursor.Row >= _scrollRow + contentHeight)
            _scrollRow = cursor.Row - contentHeight + 1;

        if (cursor.Col < _scrollCol)
            _scrollCol = cursor.Col;
        if (cursor.Col >= _scrollCol + contentWidth)
            _scrollCol = cursor.Col - contentWidth + 1;
    }

    // ─────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────

    private static int StripLen(string s) => s.Length;

    private static readonly Regex AnsiRe = new(@"\x1b\[[0-9;]*m", RegexOptions.Compiled);

    private static int StripAnsiLen(string s) => AnsiRe.Replace(s, "").Length;

    /// <summary>Build a box line: "│" + content padded to boxW-2 + "│"</summary>
    private static string MakeLine(int boxW, string content)
    {
        int visLen = AnsiRe.Replace(content, "").Length;
        int pad = boxW - 2 - visLen;
        if (pad < 0) pad = 0;
        return "│" + content + new string(' ', pad) + "│";
    }
}

// Small extension to avoid allocations on repeated chars in string interpolation
static class CharExtensions
{
    public static string Repeat(this char c, int count)
        => count > 0 ? new string(c, count) : "";
}
