namespace Microsoft.DocAsCode.EntityModel
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Utility;

    public class MapFileViewModel : Dictionary<string, MapFileItemViewModel>
    {
    }

    public class ReferencesViewModel : Dictionary<string, MapFileItemViewModel>
    {
        /// <summary>
        /// TODO: Any possible scenarios requires MERGE?
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(MapFileItemViewModel item)
        {
            this.Add(item.Id, item);
        }

        public void AddRange(ReferencesViewModel references)
        {
            foreach (var reference in references)
            {
                this.AddItem(reference.Value);
            }
        }
    }

    public enum MapFileType
    {
        Markdown,
        Yaml,
        Link,
        CodeSnippet,
    }

    public class MapFileItemViewModel : ICloneable
    {
        [YamlDotNet.Serialization.YamlMember(Alias = "id")]
        [JsonProperty("id")]
        public string Id { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public MapFileType MapFileType { get; set; }

        /// <summary>
        /// Similar to yaml, href is the location of md in the generated website, currently it is copied to md folder
        /// </summary>
        [YamlDotNet.Serialization.YamlMember(Alias = "href")]
        [JsonProperty("href")]
        public string Href { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "startLine")]
        [JsonProperty("startLine")]
        public int Startline { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "endLine")]
        [JsonProperty("endLine")]
        public int Endline { get; set; }

        /// <summary>
        /// The start line in the referencing file that define the reference
        /// </summary>
        [YamlDotNet.Serialization.YamlMember(Alias = "Keys")]
        [JsonProperty("Keys")]
        public List<string> Keys
        {
            get
            {
                return this.ReferenceKeys?.Keys.ToList();
            }
            set
            {
                if (value != null)
                    ReferenceKeys = value.ToDictionary(s => s, s => new Section { Key = s });
            }
        }

        [YamlDotNet.Serialization.YamlIgnore]
        [JsonIgnore]
        public Dictionary<string, Section> ReferenceKeys { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "remote")]
        [JsonProperty("remote")]
        public GitDetail Remote { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "references")]
        [JsonProperty("references")]
        public ReferencesViewModel References { get; set; }

        /// <summary>
        /// To override yaml settings
        /// </summary>
        [YamlDotNet.Serialization.YamlMember(Alias = "override")]
        [JsonProperty("override")]
        public Dictionary<string, object> CustomProperties { get; set; }

        /// <summary>
        /// Message if resolve fails
        /// </summary>
        [YamlDotNet.Serialization.YamlMember(Alias = "message")]
        [JsonProperty("message")]
        public string Message { get; set; }

        public bool HasUsefulInfo()
        {
            return
                !(string.IsNullOrEmpty(Message) && CustomProperties == null && References == null
                  && (Remote == null || string.IsNullOrEmpty(Remote.RemoteRepositoryUrl)) && ReferenceKeys == null);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (object.ReferenceEquals(this, obj))
            {
                return true;
            }

            var other = obj as MapFileItemViewModel;
            if (other == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(this.Id) && string.IsNullOrEmpty(other.Id))
            {
                return true;
            }
            else if (string.IsNullOrEmpty(this.Id) || string.IsNullOrEmpty(other.Id))
            {
                return false;
            }

            return this.Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            if (this.Id == null)
            {
                return string.Empty.GetHashCode();
            }

            return this.Id.GetHashCode();
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public override string ToString()
        {
            using (StringWriter writer = new StringWriter())
            {
                YamlUtility.Serialize(writer, this);
                return writer.ToString();
            }
        }
    }

    public class Section
    {
        /// <summary>
        /// The raw content matching the regular expression, e.g. @ABC
        /// </summary>
        [YamlDotNet.Serialization.YamlMember(Alias = "key")]
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        /// Defines the Markdown Content Location Range
        /// </summary>
        [YamlDotNet.Serialization.YamlIgnore]
        [JsonIgnore]
        public List<Location> Locations { get; set; }
    }

    public struct Location
    {
        public Coordinate StartLocation { get; set; }

        public Coordinate EndLocation { get; set; }

        public static Location GetLocation(string input, int matchedStartIndex, int matchedLength)
        {
            if (matchedLength <= 0) return new Location();
            if (matchedStartIndex < 0) matchedStartIndex = 0;
            if (matchedStartIndex + matchedLength > input.Length)
            {
                matchedLength = input.Length - matchedStartIndex;
            }

            var beforeMatch = input.Substring(0, matchedStartIndex);
            Coordinate start = Coordinate.GetCoordinate(beforeMatch);

            var matched = input.Substring(matchedStartIndex, matchedLength);
            Coordinate startToEnd = Coordinate.GetCoordinate(matched);
            Coordinate end = start.Add(startToEnd);
            return new Location() { StartLocation = start, EndLocation = end };
        }

        public bool IsIn(Location wrapper)
        {
            return wrapper.StartLocation.CompareTo(this.StartLocation) <= 0 && wrapper.EndLocation.CompareTo(this.EndLocation) >= 0;
        }

        public override string ToString()
        {
            return string.Format("{Start{0}, End{1}}", StartLocation, EndLocation);
        }
    }

    public struct Coordinate : IComparable<Coordinate>
    {
        private const char NewLineCharacter = '\n';

        public int Line { get; set; }
        public int Column { get; set; }

        public readonly static Coordinate Default = new Coordinate();

        public Coordinate Add(Coordinate toAdd)
        {
            return new Coordinate() { Line = this.Line + toAdd.Line, Column = this.Column + toAdd.Column };
        }

        /// <summary>
        /// Lines & Columns start at 0 to leverage default value, NOTE that IDE start at 1, need to +1 at the outermost
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static Coordinate GetCoordinate(string content)
        {
            if (string.IsNullOrEmpty(content)) return Coordinate.Default;
            int index = content.Length - 1;
            int line = content.Split(NewLineCharacter).Length - 1;

            // Remove last new line character if it is last character of the content
            if (content[content.Length - 1] == NewLineCharacter)
            {
                content = content.Substring(0, content.Length - 1);
                line = line - 1;
                index -= 1;
            }
            int lineStart = content.LastIndexOf(NewLineCharacter);
            int col = index - lineStart - 1;
            return new Coordinate { Line = line, Column = col };
        }

        public int CompareTo(Coordinate other)
        {
            if (this.Line > other.Line) return 1;
            if (this.Line < other.Line) return -1;
            if (this.Column > other.Column) return 1;
            if (this.Column < other.Column) return -1;
            return 0;
        }

        public override string ToString()
        {
            return string.Format("{{line{0}, col{1}}}", Line, Column);
        }
    }
}
