namespace Microsoft.DocAsCode.EntityModel.ExtractSearchData
{
    using Newtonsoft.Json;
    public class SearchItem
    {
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("display")]
        public string Display { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("keywords")]
        public string Keywords { get; set; }
    }
}