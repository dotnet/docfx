// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Docfx.Common;


/// <summary>
///   An item that contains metadate
/// </summary>
internal interface IItemWithMetadata
{

    /// <summary>
    ///   Gets the metadata.
    /// </summary>
    /// <value>The metadata.</value>
    Dictionary<string, object> Metadata { get; }

}
