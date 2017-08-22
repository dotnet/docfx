// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XRefService.Common.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("UidTable", Schema = "dbo")]
    public class XRefSpecObject
    {
        [Key]
        public string HashedUid { get; set; }

        public string Uid { get; set; }

        [Required]
        public string XRefSpecJson { get; set; }
    }
}