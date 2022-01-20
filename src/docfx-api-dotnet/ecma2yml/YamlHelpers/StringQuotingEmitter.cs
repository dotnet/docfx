using System;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace ECMA2Yaml.YamlHelpers
{
    //https://github.com/cloudbase/powershell-yaml/pull/41/files#diff-e4c14acd05e286e165f6b75e7a30d165
    public class StringQuotingEmitter : ChainedEventEmitter
    {
        // Patterns from https://yaml.org/spec/1.2/spec.html#id2804356
        private static Regex quotedRegex = new Regex("^([nN]ull|NULL|[tT]rue|TRUE|[fF]alse|FALSE|-?(0|[1-9][0-9]*)(\\.[0-9]*)?([eE][-+]?[0-9]+)?)?$", RegexOptions.Compiled);
        public StringQuotingEmitter(IEventEmitter next) : base(next) { }
        public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
        {
            var typeCode = eventInfo.Source.Value != null
            ? Type.GetTypeCode(eventInfo.Source.Type)
            : TypeCode.Empty;
            switch (typeCode)
            {
                case TypeCode.Char:
                    if (Char.IsDigit((char)eventInfo.Source.Value))
                    {
                        eventInfo.Style = ScalarStyle.DoubleQuoted;
                    }
                    break;
                case TypeCode.String:
                    var val = eventInfo.Source.Value.ToString();
                    if (quotedRegex.IsMatch(val))
                    {
                        eventInfo.Style = ScalarStyle.DoubleQuoted;
                    }
                    break;
            }
            base.Emit(eventInfo, emitter);
        }
        public static SerializerBuilder Add(SerializerBuilder builder)
        {
            return builder.WithEventEmitter(next => new StringQuotingEmitter(next));
        }
    }
}
