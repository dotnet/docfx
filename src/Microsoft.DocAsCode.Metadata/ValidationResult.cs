namespace Microsoft.DocAsCode.Metadata
{
    public class ValidationResult
    {
        private ValidationResult()
        {
        }

        public static readonly ValidationResult Success = new ValidationResult { IsSuccess = true };

        public static ValidationResult Fail(string code, string message, string path)
        {
            return new ValidationResult
            {
                Code = code,
                Message = message,
                Path = path,
            };
        }

        public bool IsSuccess { get; private set; }
        public string Code { get; private set; }
        public string Message { get; private set; }
        public string Path { get; private set; }
    }
}
