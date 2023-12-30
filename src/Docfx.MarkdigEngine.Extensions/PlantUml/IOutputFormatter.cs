namespace Docfx.MarkdigEngine.Extensions;

internal interface IOutputFormatter
{
    string FormatOutput(byte[] output);
}
