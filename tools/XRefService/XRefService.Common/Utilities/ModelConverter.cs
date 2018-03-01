// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XRefService.Common.Utilities
{
    using System;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using XRefService.Common.Models;

    public static class ModelConverter
    {
        public static XRefSpec ToXRefSpec(this XRefSpecObject xso)
        {
            return JsonUtility.FromJsonString<XRefSpec>(xso.XRefSpecJson);
        }

        public static XRefSpecObject ToXRefSpecObject(this XRefSpec spec)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }
            return new XRefSpecObject
            {
                HashedUid = MD5Encryption.CalculateMD5Hash(spec.Uid),
                Uid = spec.Uid,
                XRefSpecJson = spec.ToJsonString(),
            };
        }
    }
}
