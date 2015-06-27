namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;

    public class YamlViewModel
    {
        public List<OnePageViewModel> Pages { get; set; } 
        public OnePageViewModel Toc { get; set; }
    }
}
