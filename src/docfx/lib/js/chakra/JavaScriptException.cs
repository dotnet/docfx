// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ChakraHost.Hosting;

/// <summary>
///     An exception returned from the Chakra engine.
/// </summary>
public class JavaScriptException : Exception
{
    /// <summary>
    /// The error code.
    /// </summary>
    private readonly JavaScriptErrorCode _code;

    /// <summary>
    ///     Initializes a new instance of the <see cref="JavaScriptException"/> class. 
    /// </summary>
    /// <param name="code">The error code returned.</param>
    public JavaScriptException(JavaScriptErrorCode code) :
        this(code, "A fatal exception has occurred in a JavaScript runtime")
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="JavaScriptException"/> class. 
    /// </summary>
    /// <param name="code">The error code returned.</param>
    /// <param name="message">The error message.</param>
    public JavaScriptException(JavaScriptErrorCode code, string message) :
        base(message)
    {
        _code = code;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="JavaScriptException"/> class. 
    /// </summary>
    /// <param name="info">The serialization info.</param>
    /// <param name="context">The streaming context.</param>
    protected JavaScriptException(string message, Exception innerException) :
        base(message, innerException)
    {
        if (message != null)
        {
            _code = (JavaScriptErrorCode)base.HResult;
        }
    }

    /*
    /// <summary>
    ///     Serializes the exception information.
    /// </summary>
    /// <param name="info">The serialization information.</param>
    /// <param name="context">The streaming context.</param>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue("code", (uint)code);
    }
    */
    /// <summary>
    ///     Gets the error code.
    /// </summary>
    public JavaScriptErrorCode ErrorCode
    {
        get { return _code; }
    }
}
