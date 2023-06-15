// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

using Microsoft.DocAsCode.YamlSerialization.Helpers;

namespace Microsoft.DocAsCode.YamlSerialization.NodeTypeResolvers;

internal sealed class ScalarYamlNodeTypeResolver : INodeTypeResolver
{
    bool INodeTypeResolver.Resolve(NodeEvent nodeEvent, ref Type currentType)
    {
        if (currentType == typeof(string) || currentType == typeof(object))
        {
            if (nodeEvent is Scalar scalar && scalar.IsPlainImplicit)
            {
                if (Regexes.BooleanLike.IsMatch(scalar.Value))
                {
                    currentType = typeof(bool);
                    return true;
                }

                if (Regexes.IntegerLike.IsMatch(scalar.Value))
                {
                    if (int.TryParse(scalar.Value, out _))
                    {
                        currentType = typeof(int);
                        return true;
                    }
                    if (long.TryParse(scalar.Value, out _))
                    {
                        currentType = typeof(long);
                        return true;
                    }
                    if (ulong.TryParse(scalar.Value, out _))
                    {
                        currentType = typeof(ulong);
                        return true;
                    }
                }

                if (Regexes.FloatLike.IsMatch(scalar.Value))
                {
                    currentType = typeof(double);
                    return true;
                }
            }
        }
        return false;
    }
}
