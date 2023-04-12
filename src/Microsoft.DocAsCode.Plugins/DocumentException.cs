namespace Microsoft.DocAsCode.Plugins;

public class DocumentException : Exception
{
    public string File { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }

    public DocumentException() { }
    public DocumentException(string message) : base(message) { }
    public DocumentException(string message, Exception inner) : base(message, inner) { }

    public static void RunAll(params Action[] actions)
    {
        if (actions == null)
        {
            throw new ArgumentNullException(nameof(actions));
        }
        DocumentException firstException = null;
        foreach (var action in actions)
        {
            try
            {
                action();
            }
            catch (DocumentException ex)
            {
                firstException ??= ex;
            }
        }
        if (firstException != null)
        {
            throw new DocumentException(firstException.Message, firstException);
        }
    }
}
