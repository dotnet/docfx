// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerializations.NodeTypeResolvers
{
    using System;

    using YamlDotNet.Core.Events;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.YamlSerializations.Helpers;

    internal sealed class ScalarYamlNodeTypeResolver : INodeTypeResolver
    {
        bool INodeTypeResolver.Resolve(NodeEvent nodeEvent, ref Type currentType)
        {
            if (currentType == typeof(string) || currentType == typeof(object))
            {
                var scalar = nodeEvent as Scalar;
                if (scalar != null && scalar.IsPlainImplicit)
                {
                    if (Regexes.BooleanLike.IsMatch(scalar.Value))
                    {
                        currentType = typeof(bool);
                        return true;
                    }

                    if (Regexes.IntegerLike.IsMatch(scalar.Value))
                    {
                        currentType = typeof(int);
                        return true;
                    }

                    if (Regexes.DoubleLike.IsMatch(scalar.Value))
                    {
                        currentType = typeof(double);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
