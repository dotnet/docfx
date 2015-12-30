// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;

    public class OptionParserException : ArgumentException
    {
        public OptionParserException() : this("Invalid command options!")
        {
        }

        public OptionParserException(string message) : base(message)
        {
        }
    }
}
