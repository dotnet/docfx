// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.LearnValidation;

public class LearnLogItem
{
    public LearnErrorLevel ErrorLevel { get; }

    public LearnErrorCode ErrorCode { get; }

    public string Message { get; }

    public string File { get; }

    public LearnLogItem(LearnErrorLevel errorLevel, LearnErrorCode errorCode, string message, string file)
    {
        ErrorLevel = errorLevel;
        ErrorCode = errorCode;
        Message = message;
        File = file;
    }
}
