using static System.Text.Encoding;

namespace Docfx.MarkdigEngine.Extensions;

internal class SvgOutputFormatter : IOutputFormatter
{
    public SvgOutputFormatter()
    {
    }

    public string FormatOutput(byte[] output)
    {
        string svg = UTF8.GetString(output);
        return $"<div class=\"lang-plantUml\">{svg}</div>";
    }
}
