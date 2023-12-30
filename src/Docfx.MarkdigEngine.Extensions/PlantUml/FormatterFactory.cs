using PlantUml.Net;

namespace Docfx.MarkdigEngine.Extensions;

internal class FormatterFactory
{
    private readonly DocfxPlantUmlSettings settings;

    private OutputFormat OutputFormat => settings.OutputFormat;

    public FormatterFactory(DocfxPlantUmlSettings settings)
    {
        this.settings = settings;
    }

    internal IOutputFormatter CreateOutputFormatter()
    {
        switch (OutputFormat)
        {
            case OutputFormat.Svg:
                return new SvgOutputFormatter();

            case OutputFormat.Ascii:
                return new AsciiOutputFormatter();

            case OutputFormat.Ascii_Unicode:
                return new AsciiUnicodeOutputFormatter();

            case OutputFormat.Png:
            case OutputFormat.Eps:
            case OutputFormat.Pdf:
            case OutputFormat.Vdx:
            case OutputFormat.Xmi:
            case OutputFormat.Scxml:
            case OutputFormat.Html:
            case OutputFormat.LaTeX:
            default:
                throw new NotSupportedException($"output format {OutputFormat} is not currently supported");
        }
    }
}
