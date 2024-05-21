// Licensed to the .NET Foundation under one or more agreements.\r
// The .NET Foundation licenses this file to you under the MIT license.


namespace Docfx.Common;


/// <summary>
///   An item that contains metadate
/// </summary>
public interface IItemWithMetadata
{

    /// <summary>
    ///   Gets the metadata.
    /// </summary>
    /// <value>The metadata.</value>
    Dictionary<string, object> Metadata { get; }

}
