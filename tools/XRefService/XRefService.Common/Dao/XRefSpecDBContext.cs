// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XRefService.Common.Dao
{
    using XRefService.Common.Models;

    using System.Data.Entity;

    public class XRefSpecDBContext : DbContext
    {
        public XRefSpecDBContext(string connString) : base(connString)
        {

        }

        public DbSet<XRefSpecObject> XRefSpecObjects { get; set; }
    }
}