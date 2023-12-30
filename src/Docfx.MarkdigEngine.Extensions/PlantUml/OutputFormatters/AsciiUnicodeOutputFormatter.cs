
using static System.Text.Encoding;

namespace Docfx.MarkdigEngine.Extensions;

internal class AsciiUnicodeOutputFormatter : IOutputFormatter
{
    public AsciiUnicodeOutputFormatter()
    {
    }

    public string FormatOutput(byte[] output)
    {
        string ascii = UTF8.GetString(output);
        return $"<div class=\"lang-plantUml\"><pre>{ascii}</pre></div>";
    }
}
