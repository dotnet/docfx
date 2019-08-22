namespace ChakraHost.Hosting
{
    using System;

    /// <summary>
    ///     A callback called before collecting an object.
    /// </summary>
    /// <remarks>
    ///     Use <c>JsSetObjectBeforeCollectCallback</c> to register this callback.
    /// </remarks>
    /// <param name="ref">The object to be collected.</param>
    /// <param name="callbackState">The state passed to <c>JsSetObjectBeforeCollectCallback</c>.</param>
    public delegate void JavaScriptObjectBeforeCollectCallback(JavaScriptValue reference, IntPtr callbackState);
}
