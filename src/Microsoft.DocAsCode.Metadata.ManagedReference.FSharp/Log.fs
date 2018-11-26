// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference.FSharp
open Microsoft.DocAsCode.Common
open Microsoft.FSharp.Core.Printf


module internal Log =

    let inline private log level msg =
        Logger.Log(level, msg)
        
    let inline error format = kprintf (log LogLevel.Error) format
    let inline warning format = kprintf (log LogLevel.Warning) format
    let inline verbose format = kprintf (log LogLevel.Verbose) format
    let inline info format = kprintf (log LogLevel.Info) format
    let inline debug format = kprintf (log LogLevel.Diagnostic) format

