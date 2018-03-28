// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference.FSharp
open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

[<assembly: InternalsVisibleTo("Microsoft.DocAsCode.Metadata.ManagedReference.Tests")>]
[<assembly: InternalsVisibleTo("Microsoft.DocAsCode.Metadata.ManagedReference.FSharp.Tests")>]

do
    ()