using ECMA2Yaml.Models;
using ECMA2Yaml.Models.SDP;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ECMA2Yaml
{
    public sealed class ApiScanGenerator
    {
        private static readonly IDictionary<ItemType, Func<ReflectionItem, IEnumerable<string>>> ApiNameMapper = new Dictionary<ItemType, Func<ReflectionItem, IEnumerable<string>>>
        {
            { ItemType.Class, item => GetTypeApiNamesCore(item, string.Empty) },
            { ItemType.Struct, item => GetTypeApiNamesCore(item, string.Empty) },
            { ItemType.Interface, item => GetTypeApiNamesCore(item, string.Empty) },
            { ItemType.Enum, item => GetEnumApiNames(item) },
            { ItemType.Delegate, item => GetTypeApiNamesCore(item, string.Empty, "..ctor", ".Invoke", ".BeginInvoke", ".EndInvoke") },
            { ItemType.Constructor, item => GenerateMemberApiNames(item, ".") },
            { ItemType.Method, item => GenerateMemberApiNames(item, ".") },
            { ItemType.Field, item => GenerateMemberApiNames(item, ".") },
            { ItemType.Operator, item => GenerateMemberApiNames(item, ".", ".op_") },
            { ItemType.Property, item => GenerateMemberApiNames(item, ".", new Separator(".get_", i => i.Modifiers?.Count > 0 && i.Modifiers["csharp"].Contains("get")), new Separator(".set_", i => i.Modifiers?.Count > 0 && i.Modifiers["csharp"].Contains("set"))) },
            { ItemType.AttachedProperty, item => GenerateMemberApiNames(item, ".", new Separator(".get_", i => i.Modifiers?.Count > 0 && i.Modifiers["csharp"].Contains("get")), new Separator(".set_", i => i.Modifiers?.Count > 0 && i.Modifiers["csharp"].Contains("set"))) },
            { ItemType.Event, item => GenerateMemberApiNames(item, ".", ".add_", ".remove_") },
            { ItemType.AttachedEvent, item => GenerateMemberApiNames(item, ".", ".add_", ".remove_") },
        };

        public const string APISCAN_APINAME = "api_name";
        public const string APISCAN_APILOCATION = "api_location";
        public const string APISCAN_TOPICTYPE = "topic_type";
        public const string APISCAN_APITYPE = "api_type";

        public static void Generate(ItemSDPModelBase model, ReflectionItem item)
        {
            var apiNames = GetApiNames(item).ToList();
            var assemblies = item.AssemblyInfo?.Select(asm => asm.Name).Distinct().ToList();
            if (apiNames.Count > 0)
            {
                if (!model.Metadata.ContainsKey(APISCAN_APINAME))
                {
                    model.Metadata[APISCAN_APINAME] = apiNames;
                }
                if (!model.Metadata.ContainsKey(APISCAN_APILOCATION))
                {
                    model.Metadata[APISCAN_APILOCATION] = assemblies.Select(a => a + ".dll").ToList();
                }
                if (!model.Metadata.ContainsKey(APISCAN_TOPICTYPE))
                {
                    model.Metadata[APISCAN_TOPICTYPE] = new List<string> { "apiref" };
                }
                if (!model.Metadata.ContainsKey(APISCAN_APITYPE))
                {
                    model.Metadata[APISCAN_APITYPE] = new List<string> { "Assembly" };
                }
            }
        }

        private static IEnumerable<string> GetApiNames(ReflectionItem item)
        {
            if (ApiNameMapper.TryGetValue(item.ItemType, out Func<ReflectionItem, IEnumerable<string>> func))
            {
                return func(item);
            }

            return Enumerable.Empty<string>();
        }

        private static IEnumerable<string> GetTypeApiNamesCore(ReflectionItem item, params Separator[] separators)
        {
            string type = item.Uid;
            foreach (var separator in separators)
            {
                if (separator.Condition(item))
                {
                    yield return $"{type}{separator}";
                }
            }
        }

        private static IEnumerable<string> GetEnumApiNames(ReflectionItem item)
        {
            List<string> names = new List<string>();
            names.Add(item.Uid);
            var t = item as Models.Type;
            foreach (var f in t.Members)
            {
                names.Add(f.Uid);
            }
            return names;
        }

        private static IEnumerable<string> GenerateMemberApiNames(ReflectionItem item, params Separator[] separators)
        {
            var name = item.Name;
            var prefix = "";
            var dotIndex = name.LastIndexOf('.');
            if (dotIndex > 0)
            {
                prefix = "." + name.Substring(0, dotIndex);
                name = name.Substring(dotIndex + 1);
            }
            if (name.StartsWith("op_"))
            {
                name = name.Substring("op_".Length);
            }
            // methods with generics do not add any generics information
            // (neither type parameters in angled brackets nor `[type parameter count])
            if (name[name.Length - 1] == '>')
            {
                var index = name.LastIndexOf('<');
                if (index != -1)
                {
                    name = name.Remove(index);
                }
            }

            foreach (var separator in separators)
            {
                if (separator.Condition(item))
                {
                    yield return $"{item.Parent.Uid}{prefix}{separator}{name}";
                }
            }
        }

        internal class Separator
        {
            public Func<ReflectionItem, bool> Condition { get; set; }

            public string Value { get; set; }

            public Separator(string value = "", Func<ReflectionItem, bool> func = null)
            {
                Value = value;
                Condition = func ?? (model => true);
            }

            public static implicit operator Separator(string value)
            {
                return new Separator(value);
            }

            public override string ToString()
            {
                return Value;
            }
        }
    }
}
