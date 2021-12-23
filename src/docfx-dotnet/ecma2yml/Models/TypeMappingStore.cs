using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ECMA2Yaml.Models
{
    public class TypeMappingStore
    {
        public Dictionary<string, Dictionary<string, string>> TypeMappingPerLanguage { get; set; }

        /// <summary>
        /// Do type mapping per language, then aggregate based on the mapped value
        /// </summary>
        /// <param name="typeString">original type string</param>
        /// <param name="totalLangs">HashSet of the total languages this repo supports</param>
        /// <returns></returns>
        public List<PerLanguageString> TranslateTypeString(
            string typeString,
            HashSet<string> totalLangs)
        {
            var defaultValue = new PerLanguageString() { Value = typeString, Langs = new HashSet<string>(totalLangs) };
            var rval = new List<PerLanguageString>() { defaultValue };
            if (TypeMappingPerLanguage?.Count > 0)
            {
                // workaround for https://ceapex.visualstudio.com/Engineering/_workitems/edit/438233
                // revert type mapping made by mdoc before applying them again
                if (TypeMappingPerLanguage.TryGetValue("csharp", out var csharpDict))
                {
                    var revertTypeString = typeString;
                    foreach (var mapping in csharpDict)
                    {
                        revertTypeString = revertTypeString.Replace(mapping.Value, mapping.Key);
                    }
                    if (revertTypeString != typeString)
                    {
                        Console.WriteLine(typeString + " reverted to " + revertTypeString);
                        typeString = revertTypeString;
                        defaultValue.Value = revertTypeString;
                    }
                }
                foreach (var mappingPerLang in TypeMappingPerLanguage)
                {
                    var lang = mappingPerLang.Key;
                    var mappingDict = mappingPerLang.Value;
                    var newTypeString = typeString;
                    if (totalLangs.Contains(lang))
                    {
                        foreach (var mapping in mappingDict)
                        {
                            newTypeString = newTypeString.Replace(mapping.Key, mapping.Value);
                        }
                        if (newTypeString != typeString)
                        {
                            rval.Add(new PerLanguageString() { Langs = new HashSet<string> { lang }, Value = newTypeString });
                            defaultValue.Langs.Remove(lang);
                        }
                        if (defaultValue.Langs.Count == 0)
                        {
                            rval.Remove(defaultValue);
                        }
                    }
                }
            }
            if (rval.Count > 2)
            {
                rval = rval.GroupBy(v => v.Value)
                    .Select(g => g.Count() == 1 ? g.First() : new PerLanguageString() { Value = g.Key, Langs = g.SelectMany(v => v.Langs).ToHashSet() })
                    .ToList();
            }
            else if (rval.Count == 1)
            {
                rval.First().Langs = null;
            }
            return rval;
        }
    }
}
