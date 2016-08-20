namespace Microsoft.DocAsCode.Build.Incrementals
{
    using System;

    public class FileAttributeItem
    {
        /// <summary>
        /// The file path
        /// </summary>
        public string File { get; set; }
        /// <summary>
        /// Last modify time
        /// </summary>
        public DateTime LastModifiedTime { get; set; }
        /// <summary>
        /// MD5 string of the file content
        /// </summary>
        public string MD5 { get; set; }
    }
}
