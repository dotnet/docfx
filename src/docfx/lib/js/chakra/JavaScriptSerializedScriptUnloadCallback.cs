namespace ChakraHost.Hosting
{
    using System;

    /// <summary>
    ///     Called by the runtime when it is finished with all resources related to the script execution.
    ///     The caller should free the source if loaded, the byte code, and the context at this time.
    /// </summary>
    /// <param name="sourceContext">The context passed to Js[Parse|Run]SerializedScriptWithCallback</param>
    public delegate void JavaScriptSerializedScriptUnloadCallback(JavaScriptSourceContext sourceContext);
}
