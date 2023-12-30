
using static System.Text.Encoding;

namespace Docfx.MarkdigEngine.Extensions;

internal class AsciiOutputFormatter : IOutputFormatter
{
    public AsciiOutputFormatter()
    {
    }

    public string FormatOutput(byte[] output)
    {
        string ascii = ASCII.GetString(output);
        return $"<div class=\"lang-plantUml\"><pre>{ascii}</pre></div>";
    }
}

