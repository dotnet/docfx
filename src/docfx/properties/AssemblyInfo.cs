// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("docfx.Test")]
[assembly: InternalsVisibleTo("docfx.SpecTest")]
[assembly: InternalsVisibleTo("docfx.RegressionTest")]
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
[assembly: CLSCompliant(false)]
