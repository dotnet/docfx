// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XRefService.Common.Dao
{
    using System.Data.Entity.Infrastructure;

    public class XRefSpecDBContextFactory : IDbContextFactory<XRefSpecDBContext>
    {
        public XRefSpecDBContext Create()
        {
            return new XRefSpecDBContext("");
        }
    }
}
