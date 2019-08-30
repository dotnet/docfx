﻿namespace ChakraHost.Hosting
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    ///     A fatal exception occurred.
    /// </summary>
    public sealed class JavaScriptFatalException : JavaScriptException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="JavaScriptFatalException"/> class. 
        /// </summary>
        /// <param name="code">The error code returned.</param>
        public JavaScriptFatalException(JavaScriptErrorCode code) :
            this(code, "A fatal exception has occurred in a JavaScript runtime")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="JavaScriptFatalException"/> class. 
        /// </summary>
        /// <param name="code">The error code returned.</param>
        /// <param name="message">The error message.</param>
        public JavaScriptFatalException(JavaScriptErrorCode code, string message) :
            base(code, message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="JavaScriptFatalException"/> class.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        private JavaScriptFatalException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}