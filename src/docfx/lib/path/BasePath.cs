namespace Microsoft.Docs.Build
{
    internal readonly struct BasePath
    {
        public readonly string Original;

        private readonly string _relativePath;

        public BasePath(string value)
        {
            Original = value;
            var path = string.IsNullOrEmpty(value) || value == "/" ? "." : value;
            _relativePath = path.StartsWith('/') ? path.TrimStart('/') : path;
        }

        public static implicit operator string(in BasePath basePath) => basePath._relativePath;
    }
}
