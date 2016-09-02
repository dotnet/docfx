// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;

    public class DependencyItem : IEquatable<DependencyItem>
    {
        public string From { get; set; }

        public string To { get; set; }

        public string ReportedBy { get; set; }

        public string Type { get; set; }

        public bool IsTransitive { get; set; }

        public bool Equals(DependencyItem dp)
        {
            if (dp == null)
            {
                return false;
            }
            if (ReferenceEquals(this, dp))
            {
                return true;
            }
            return this.From == dp.From &&
                this.To == dp.To &&
                this.ReportedBy == dp.ReportedBy &&
                this.Type == dp.Type &&
                this.IsTransitive == dp.IsTransitive;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as DependencyItem);
        }

        public override string ToString()
        {
            return $"From: {From}, To: {To}, ReportedBy: {ReportedBy}, Type: {Type}, IsTransitive: {IsTransitive}.";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
