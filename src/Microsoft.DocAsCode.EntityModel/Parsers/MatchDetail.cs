namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;

    public class MatchDetailCollection : Dictionary<string, MatchDetail>
    {
        public MatchDetailCollection() : base() { }
        public MatchDetailCollection(IEqualityComparer<string> comparer) : base(comparer) { }
        public MatchDetailCollection Merge(IEnumerable<MatchSingleDetail> singleMatch)
        {
            foreach (var detail in singleMatch)
            {
                if (detail == null) continue;
                MatchDetail existingDetail;
                if (this.TryGetValue(detail.Id, out existingDetail))
                {
                    existingDetail.MatchedSections.AddOrMerge(detail.MatchedSection);
                }
                else
                {
                    existingDetail = new MatchDetail
                                         {
                                             Id = detail.Id,
                                             MatchedSections = new SectionCollection(detail.MatchedSection),
                                             StartLine = detail.StartLine,
                                             EndLine = detail.EndLine,
                                             Properties = detail.Properties,
                                             Path = detail.Path
                                         };
                    this.Add(existingDetail.Id, existingDetail);
                }
            }

            return this;
        }
    }

    public class SectionCollection : Dictionary<string, Section>
    {
        public SectionCollection()
            : base()
        {
        }

        public SectionCollection(Section section)
            : base()
        {
            this.Add(section.Key, section);
        }

        public void AddOrMerge(Section section)
        {
            Section existingSection;
            if (this.TryGetValue(section.Key, out existingSection))
            {
                existingSection.Locations.AddRange(section.Locations);
            }
            else
            {
                this.Add(section.Key, section);
            }
        }
    }

    public class MatchDetail
    {
        public SectionCollection MatchedSections { get; set; }

        /// <summary>
        /// The Id from regular expression's content group, e.g. ABC from @ABC
        /// </summary>
        public string Id { get; set; }
        public string Path { get; set; }

        public int StartLine { get; set; }

        public int EndLine { get; set; }

        /// <summary>
        /// Store all the other customized properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; }

        public override int GetHashCode()
        {
            return string.IsNullOrEmpty(Id) ? string.Empty.GetHashCode() : Id.GetHashCode();
        }
    }

    public class MatchSingleDetail
    {
        public Section MatchedSection { get; set; }

        /// <summary>
        /// The Id from regular expression's content group, e.g. ABC from @ABC
        /// </summary>
        public string Id { get; set; }
        public string Path { get; set; }

        public int StartLine { get; set; }

        public int EndLine { get; set; }

        public Dictionary<string, object> Properties { get; set; } 

        public override int GetHashCode()
        {
            return string.IsNullOrEmpty(Id) ? string.Empty.GetHashCode() : Id.GetHashCode();
        }
    }
}
