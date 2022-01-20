using Mono.Documentation.Updater.Formatters.CppFormatters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mono.Documentation.Updater.Formatters
{
    static class FormatterManager
    {
        public static List<MemberFormatter> TypeFormatters { get; private set; } = new List<MemberFormatter>
        {
            new CSharpMemberFormatter(MDocUpdater.Instance.TypeMap),
            new ILMemberFormatter(MDocUpdater.Instance.TypeMap),
        };

        public static List<MemberFormatter> MemberFormatters { get; private set; } = new List<MemberFormatter>
        {
            new CSharpFullMemberFormatter (MDocUpdater.Instance.TypeMap),
            new ILFullMemberFormatter (MDocUpdater.Instance.TypeMap),
        };

        public static AttributeFormatter CSharpAttributeFormatter { get; private set; } = new CSharpAttributeFormatter();
        public static List<AttributeFormatter> AdditionalAttributeFormatters { get; private set; } = new List<AttributeFormatter> { };

        public static DocIdFormatter DocIdFormatter { get; private set; } = new DocIdFormatter(MDocUpdater.Instance.TypeMap);

        public static MemberFormatter SlashdocFormatter { get; private set; } = new SlashDocMemberFormatter(MDocUpdater.Instance.TypeMap);

        public static void AddFormatter(string langId)
        {
            langId = langId.ToLower();
            var map = MDocUpdater.Instance.TypeMap;
            switch (langId)
            {
                case Consts.DocIdLowCase:
                    TypeFormatters.Add(DocIdFormatter);
                    MemberFormatters.Add(DocIdFormatter);
                    break;
                case Consts.VbNetLowCase:
                    TypeFormatters.Add(new VBMemberFormatter(map));
                    MemberFormatters.Add(new VBMemberFormatter(map));
                    break;
                case Consts.CppCliLowCase:
                    TypeFormatters.Add(new CppMemberFormatter(map));
                    MemberFormatters.Add(new CppFullMemberFormatter(map));
                    break;
                case Consts.CppCxLowCase:
                    TypeFormatters.Add(new CppCxMemberFormatter(map));
                    MemberFormatters.Add(new CppCxFullMemberFormatter(map));
                    break;
                case Consts.CppWinRtLowCase:
                    TypeFormatters.Add(new CppWinRtMemberFormatter(map));
                    MemberFormatters.Add(new CppWinRtFullMemberFormatter(map));
                    AdditionalAttributeFormatters.Add(new CppWinRtAttributeFormatter());
                    break;
                case Consts.FSharpLowCase:
                case "fsharp":
                    TypeFormatters.Add(new FSharpMemberFormatter(map));
                    MemberFormatters.Add(new FSharpFullMemberFormatter(map));
                    AdditionalAttributeFormatters.Add(new FSharpAttributeFormatter());
                    break;
                case Consts.JavascriptLowCase:
                    TypeFormatters.Add(new JsMemberFormatter(map));
                    MemberFormatters.Add(new JsMemberFormatter(map));
                    break;
                default:
                    throw new ArgumentException("Unsupported formatter id '" + langId + "'.");
            }
        }

        public static void UpdateTypeMap(TypeMap typeMap)
        {
            DocIdFormatter.TypeMap = typeMap;
            SlashdocFormatter.TypeMap = typeMap;
            foreach (var f in TypeFormatters.Union(MemberFormatters))
            {
                f.TypeMap = typeMap;
            }
        }
    }
}
