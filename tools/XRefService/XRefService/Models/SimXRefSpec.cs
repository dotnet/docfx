// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XRefService.Models
{
    using XRefService.Common.Models;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using XRefService.Constants;

    public class SimXRefSpec
    {
        public string Uid { get; set; }

        public string Href { get; set; }

        public int Type = 1;

        public void Create(XRefSpecObject xso)
        {
            XRefSpec xf = JsonUtility.Deserialize<XRefSpec>(new System.IO.StringReader(xso.XRefSpecJson));
            this.Uid = xf.Uid;
            this.Href = xf.Href;
            string itemKind = "";
            if(xf.TryGetValue("Type",out itemKind))
            {
                switch(itemKind)
                {
                    case "Class":
                    case "Struct":
                    case "Delegate":
                        { 
                            this.Type = (int)UidCompletionItemKind.Class;
                            break;
                        }
                    case "Interface":
                        {
                            this.Type = (int)UidCompletionItemKind.Interface;
                            break;
                        }
                    case "Property":
                        {
                            this.Type = (int)UidCompletionItemKind.Property;
                            break;
                        }
                    case "Constructor":
                        {
                            this.Type = (int)UidCompletionItemKind.Constructor;
                            break;
                        }
                    case "Method":
                        {
                            this.Type = (int)UidCompletionItemKind.Method;
                            break;
                        }
                    case "Namespace":
                        {
                            this.Type = (int)UidCompletionItemKind.Module;
                            break;
                        }
                    default:
                        {
                            this.Type = (int)UidCompletionItemKind.Text;
                            break;
                        }
                }
            }
            else if(xf.TryGetValue("commentId", out itemKind))
            {
                int index = itemKind.IndexOf(':');
                if(index == -1)
                {
                    this.Type = (int)UidCompletionItemKind.Text;
                    return;
                }
                string startStr = itemKind.Substring(0, index);
                switch (startStr)
                {
                    case "T":
                        {
                            this.Type = (int)UidCompletionItemKind.Class;
                            break;
                        }
                    case "P":
                        {
                            this.Type = (int)UidCompletionItemKind.Property;
                            break;
                        }
                    case "Overload":
                    case "M":
                        {
                            this.Type = (int)UidCompletionItemKind.Method;
                            break;
                        }
                    case "N":
                        {
                            this.Type = (int)UidCompletionItemKind.Module;
                            break;
                        }
                    default:
                        {
                            this.Type = (int)UidCompletionItemKind.Text;
                            break;
                        }
                }
            }
            else
            {
                this.Type = (int)UidCompletionItemKind.Text;
            }
        }

    }
}
