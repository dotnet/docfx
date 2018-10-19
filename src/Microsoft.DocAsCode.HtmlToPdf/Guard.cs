// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf
{
    using System;

    /// <summary>
    /// Represents a simple class for validating parameters and throwing exceptions.
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// Validates argumentValue is not null and throws ArgumentNullException if it is null.
        /// </summary>
        /// <param name="argumentValue">
        /// The argument value.
        /// </param>
        /// <param name="argumentName">
        /// Name of the argument.
        /// </param>
        public static void ArgumentNotNull(object argumentValue, string argumentName)
        {
            if (argumentValue == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>
        /// Validates argumentValue is not null or an empty string and throws ArgumentNullException if it is null
        /// or ArgumentException if it is an empty string .
        /// </summary>
        /// <param name="argumentValue">
        /// The argument value.
        /// </param>
        /// <param name="argumentName">
        /// Name of the argument.
        /// </param>
        public static void ArgumentNotNullOrEmpty(string argumentValue, string argumentName)
        {
            ArgumentNotNull(argumentValue, argumentName);
            if (string.IsNullOrEmpty(argumentValue))
            {
                throw new ArgumentException(argumentName);
            }
        }

        /// <summary>
        /// Call a user-supplied validation delegate and throws an ArgumentException if the
        /// delegate returns false.
        /// </summary>
        /// <param name="validationFunction">This function will be called to perform validation.
        /// If the function returns false, an ArgumentException will be thrown.</param>
        /// <param name="argumentName">
        /// Name of the argument.
        /// </param>
        /// <param name="argumentMessage">
        /// Optional error message
        /// </param>
        public static void Argument(Func<bool> validationFunction, string argumentName, string argumentMessage = null)
        {
            if (validationFunction == null)
            {
                return;
            }

            if (!validationFunction())
            {
                throw !string.IsNullOrEmpty(argumentMessage)
                    ? new ArgumentException(argumentMessage, argumentName)
                    : new ArgumentException(argumentName);
            }
        }
    }
}
