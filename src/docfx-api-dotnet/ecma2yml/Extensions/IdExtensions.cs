using ECMA2Yaml.Models;
using Monodoc.Ecma;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ECMA2Yaml
{
    public static class IdExtensions
    {
        public static string ToDisplayName(this string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr))
            {
                return typeStr;
            }
            if (!typeStr.Contains('<'))
            {
                var parts = typeStr.Split('.');
                return parts.Last();
            }

            return ECMAStore.GetOrAddTypeDescriptor(typeStr).ToDisplayName();
        }

        public static string ToSpecId(this string typeStr, List<string> knownTypeParamsOnType = null, List<string> knownTypeParamsOnMember = null)
        {
            if (!NeedParseByECMADesc(typeStr))
            {
                return typeStr;
            }
            return ECMAStore.GetOrAddTypeDescriptor(typeStr).ToSpecId(knownTypeParamsOnType, knownTypeParamsOnMember) ?? typeStr;
        }

        public static string ToSpecId(this EcmaDesc desc, List<string> knownTypeParamsOnType = null, List<string> knownTypeParamsOnMember = null)
        {
            if (desc == null)
            {
                return null;
            }
            var typeStr = string.IsNullOrEmpty(desc.Namespace) ? desc.TypeName : desc.Namespace + "." + desc.TypeName;
            if (desc.GenericTypeArgumentsCount > 0)
            {
                var typeparameterPart = string.Join(",", desc.GenericTypeArguments.Select(ta =>
                {
                    var i = knownTypeParamsOnType?.IndexOf(ta.TypeName);
                    if (i.HasValue && i.Value >= 0)
                    {
                        return $"`{i.Value}";
                    }
                    i = knownTypeParamsOnMember?.IndexOf(ta.TypeName);
                    if (i.HasValue && i.Value >= 0)
                    {
                        return $"``{i.Value}";
                    }
                    return ta.ToSpecId(knownTypeParamsOnType, knownTypeParamsOnMember);
                }));
                typeStr = string.Format("{0}{{{1}}}", typeStr, typeparameterPart);
            }
            if (desc.ArrayDimensions?.Count > 0)
            {
                for (int i = 0; i < desc.ArrayDimensions?.Count; i++)
                {
                    typeStr = typeStr + "[]";
                }
            }
            if (desc.DescModifier == EcmaDesc.Mod.Pointer)
            {
                typeStr += "*";
            }
            if (desc.NestedType != null)
            {
                typeStr += ("." + desc.NestedType.ToSpecId(knownTypeParamsOnType, knownTypeParamsOnMember));
            }
            return typeStr;
        }

        public static string ToOuterTypeUid(this string typeStr)
        {
            if (!NeedParseByECMADesc(typeStr))
            {
                return typeStr;
            }
            return ECMAStore.GetOrAddTypeDescriptor(typeStr).ToOuterTypeUid();
        }

        public static string ToOuterTypeUid(this EcmaDesc desc)
        {
            if (desc == null)
            {
                return null;
            }
            var typeStr = string.IsNullOrEmpty(desc.Namespace) ? desc.TypeName : (desc.Namespace + "." + desc.TypeName);
            if (desc.GenericTypeArgumentsCount > 0)
            {
                typeStr += "`" + desc.GenericTypeArgumentsCount;
            }

            return typeStr;
        }

        public static string ToDisplayName(this EcmaDesc desc)
        {
            if (desc == null)
            {
                return null;
            }

            string name = null;
            if (desc.GenericTypeArgumentsCount == 0)
            {
                name = desc.TypeName;
            }
            else
            {
                name = string.Format("{0}<{1}>", desc.TypeName, string.Join(",", desc.GenericTypeArguments.Select(d => d.ToDisplayName())));
            }
            if (desc.NestedType != null)
            {
                name += ("." + desc.NestedType.TypeName);
            }
            if (desc.ArrayDimensions?.Count > 0)
            {
                for (int i = 0; i < desc.ArrayDimensions?.Count; i++)
                {
                    name += "[]";
                }
            }
            if (desc.DescModifier == EcmaDesc.Mod.Pointer)
            {
                name += "*";
            }

            return name;
        }

        public static ReflectionItem ResolveCommentId(this string commentId, ECMAStore store)
        {
            if (string.IsNullOrEmpty(commentId))
            {
                return null;
            }
            if (store.ItemsByDocId.TryGetValue(commentId, out var item))
            {
                return item;
            }
            var (prefix, uid) = commentId.ParseCommentId();
            if (string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(uid))
            {
                return null;
            }
            switch (prefix)
            {
                case "N":
                    return store.Namespaces.ContainsKey(uid) ? store.Namespaces[uid] : null;
                case "T":
                    return store.TypesByUid.ContainsKey(uid) ? store.TypesByUid[uid] : null;
                default:
                    return store.MembersByUid.ContainsKey(uid) ? store.MembersByUid[uid] : null;
            }
        }

        public static (string, string) ParseCommentId(this string commentId)
        {
            var parts = commentId.Split(':');
            if (parts?.Length != 2)
            {
                OPSLogger.LogUserWarning(LogCode.ECMA2Yaml_CommentID_ParseFailed, null, commentId);
                return (null, null);
            }

            return (parts[0], parts[1]);
        }

        private static Regex GenericPartTypeStrRegex = new Regex("<[\\w,\\s]+>", RegexOptions.Compiled);
        public static bool TryResolveSimpleTypeString(string typeStr, ECMAStore store, out string uid)
        {
            if (!string.IsNullOrEmpty(typeStr))
            {
                if (store.TypesByFullName.TryGetValue(typeStr, out var t))
                {
                    uid = t.Uid;
                    return true;
                }
                if (typeStr.Contains('<'))
                {
                    bool simpleGeneric = false;
                    var result = GenericPartTypeStrRegex.Replace(typeStr, match =>
                    {
                        simpleGeneric = true;
                        return "`" + (match.Value.Count(c => c == ',') + 1);
                    });
                    if (simpleGeneric)
                    {
                        uid = result;
                        return true;
                    }
                }
            }
            uid = typeStr;
            return false;
        }

        private static bool NeedParseByECMADesc(string typeStr)
        {
            return (!string.IsNullOrEmpty(typeStr) && (typeStr.Contains('<') || typeStr.Contains('+')));
        }
    }

    public class TypeIdComparer : IComparer<string>
    {
        public int Compare(string stringA, string stringB)
        {
            String[] valueA = stringA.Split('`');
            String[] valueB = stringB.Split('`');

            if (valueA.Length != 2 || valueB.Length != 2)
                return String.Compare(stringA, stringB);

            int iA = 0, iB = 0;
            if (valueA[0] == valueB[0] && int.TryParse(valueA[1], out iA) && int.TryParse(valueB[1], out iB))
            {
                return iA.CompareTo(iB);
            }
            else
            {
                return String.Compare(valueA[0], valueB[0]);
            }

        }

    }
}
