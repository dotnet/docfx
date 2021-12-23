using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Mono.Documentation.Updater
{
    public class TypeMap
    {
        /// <summary>This is the core lookup data structure ... main key is language, secondary key is typename "from"</summary>
        Dictionary<string, Dictionary<string,TypeMapItem>> map;

        public List<TypeMapItem> Items { get; private set; }

        public string GetTypeName(string lang, string typename)
        {
            if (map == null) InitializeMap();
            if (!nonsinitialized && typename.IndexOf(".") == -1)
                InitializeNoNamespace();

            Dictionary<string, TypeMapItem> val;
            if (map.TryGetValue(lang, out val))
            {
                TypeMapItem itemMap;
                if (val.TryGetValue(typename, out itemMap))
                {
                    return itemMap.To;
                }
            }

            return typename;
        }

        bool nonsinitialized = false;
        private void InitializeNoNamespace()
        {
            if (nonsinitialized) return;

            Func<string, string> chopType = (orig) =>
            {
                if (orig.Contains("."))
                {
                    var lastDot = orig.LastIndexOf(".");
                    var choppedKey = orig.Substring(lastDot+1, orig.Length - lastDot -1);

                    return choppedKey;
                }
                return orig;
            };

            foreach (var langitem in map.Values)
            {
                var iitem = langitem.ToArray();
                foreach (var item in iitem)
                {
                    string choppedKey = chopType(item.Key);
                    TypeMapItem choppedItem = new TypeMapItem
                    {
                        From = choppedKey,
                        To = chopType(item.Value.To),
                        Langs = item.Value.Langs
                    };

                    langitem[choppedKey] = choppedItem;
                }
            }
        }

        private void InitializeMap()
        {
            map = new Dictionary<string, Dictionary<string, TypeMapItem>>();

            // init the map
            foreach (var item in Items)
            {

                // for each language that this item applies to, make a separate entry in the map for that language
                foreach (var itemLang in item.LangList)
                {
                    Dictionary<string, TypeMapItem> langList;
                    if (!map.TryGetValue(itemLang, out langList))
                    {
                        langList = new Dictionary<string, TypeMapItem>();
                        map.Add(itemLang, langList);
                    }

                    // sanitize the inputs
                    int genIndex = item.From.IndexOf("`");
                    if (genIndex > 0)
                        item.From = item.From.Substring(0, genIndex);

                    genIndex = item.To.IndexOf("`");
                    if (genIndex > 0)
                        item.To = item.To.Substring(0, genIndex);

                    // add to the list if it's not already there
                    if (!langList.ContainsKey(item.From))
                        langList.Add(item.From, item);
                }
            }
        }

        public static TypeMap FromXml(string path)
        {
            var doc = XDocument.Load(path);

            return FromXDocument(doc);
        }

        public static TypeMap FromXDocument(XDocument doc)
        {
            TypeMap map = new TypeMap
            {
                Items = doc.Root.Elements()
                   .Select(e =>
                   {
                       switch (e.Name.LocalName)
                       {
                           case "TypeReplace":
                               return ItemFromElement<TypeMapItem>(e);
                           case "InterfaceReplace":
                               var item = ItemFromElement<TypeMapInterfaceItem>(e);
                               item.Members = e.Element("Members");
                               return item;
                           default:
                               Console.WriteLine($"\tUnknown element: {e.Name.LocalName}");
                               break;
                       }
                       return new TypeMapItem();
                   })
                   .ToList()
            };

            return map;
        }

        private static T ItemFromElement<T>(XElement e) where T: TypeMapItem, new()
        {
            return new T
            {
                From = e.Attribute("From").Value,
                To = e.Attribute("To").Value,
                Langs = e.Attribute("Langs").Value
            };
        }

        public TypeMapInterfaceItem HasInterfaceReplace(string lang, string facename)
        {
            Dictionary<string, TypeMapItem> typemap;
            if (map.TryGetValue(lang, out typemap))
            {
                TypeMapItem item;
                if (typemap.TryGetValue(facename, out item))
                {
                    var ifaceItem = item as TypeMapInterfaceItem;
                    if (ifaceItem != null)
                        return ifaceItem;
                }
            }

            return null;
        }
    }

    public class TypeMapItem
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Langs { get; set; }

        public IEnumerable<string> LangList { get
            {
                foreach (var l in Langs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()))
                    yield return l;
            }
        }
    }

    public class TypeMapInterfaceItem : TypeMapItem
    {
        public XElement Members { get; set; }

        public XmlElement ToXmlElement(XElement el)
        {
            var doc = new XmlDocument();
            doc.Load(el.CreateReader());
            var xel = doc.DocumentElement;
            xel.ParentNode.RemoveChild(xel);
            return xel;
        }
    }
}
