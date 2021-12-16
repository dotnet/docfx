// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ChakraHost.Hosting;

/// <summary>
///     A script exception.
/// </summary>
public sealed class JavaScriptScriptException : JavaScriptException
{
    /// <summary>
    /// The error.
    /// </summary>
    private readonly JavaScriptValue _error;

    /// <summary>
    ///     Initializes a new instance of the <see cref="JavaScriptScriptException"/> class. 
    /// </summary>
    /// <param name="code">The error code returned.</param>
    /// <param name="error">The JavaScript error object.</param>
    public JavaScriptScriptException(JavaScriptErrorCode code, JavaScriptValue error) :
        this(code, error, "JavaScript Exception")
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="JavaScriptScriptException"/> class. 
    /// </summary>
    /// <param name="code">The error code returned.</param>
    /// <param name="error">The JavaScript error object.</param>
    /// <param name="message">The error message.</param>
    public JavaScriptScriptException(JavaScriptErrorCode code, JavaScriptValue error, string message) :
        base(code, message)
    {
        _error = error;
    }

    /// <summary>
    ///     Gets a JavaScript object representing the script error.
    /// </summary>
    public JavaScriptValue Error
    {
        get
        {
            return _error;
        }
    }
}
