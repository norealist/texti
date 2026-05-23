namespace textiMain;

using System.Diagnostics;

static class ClipboardHelper
{
    public static void Copy(string text)
    {
        try
        {
            var psi = new ProcessStartInfo("clip")
            {
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process == null) return;
            process.StandardInput.Write(text);
            process.StandardInput.Close();
            process.WaitForExit(3000);
        }
        catch { }
    }

    public static string Paste()
    {
        try
        {
            var psi = new ProcessStartInfo("powershell", "-NoProfile -Command Get-Clipboard")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process == null) return string.Empty;
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            // PowerShell appends trailing newline — remove it
            if (result.EndsWith("\r\n"))
                result = result[..^2];
            else if (result.EndsWith("\n"))
                result = result[..^1];
            return result;
        }
        catch
        {
            return string.Empty;
        }
    }
}
