namespace Docfx.Plugins;

public class DocumentException : Exception
{
    public DocumentException() { }
    public DocumentException(string message) : base(message) { }
    public DocumentException(string message, Exception inner) : base(message, inner) { }
}
