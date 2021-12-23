using Mono.Cecil;

namespace Mono.Documentation.Updater.Formatters
{
    class CppWinRtAttributeFormatter : AttributeFormatter
    {
        public override string PrefixBrackets { get; } = "/// [";
        public override string SurfixBrackets { get; } = "]";
        public override string Language => Consts.CppWinRt;

        public override string MakeAttributesValueString(object v, TypeReference valueType)
        {
            string baseValue = base.MakeAttributesValueString(v, valueType);
            return baseValue.StartsWith("typeof(") ? baseValue.Substring(7, baseValue.Length - 8) : baseValue;
        }
    }
}
