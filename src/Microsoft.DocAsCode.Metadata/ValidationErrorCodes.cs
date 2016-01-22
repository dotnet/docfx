namespace Microsoft.DocAsCode.Metadata
{
    public static class ValidationErrorCodes
    {
        public static class Schema
        {
            public const string BadSchema = "S.BadSchema";
            public const string UnexpectedType = "S.UnexpectedType";
        }

        public static class WellknownMetadata
        {
            public const string FieldRequired = "W.FieldRequired";
            public const string UnexpectedType = "W.UnexpectedType";
            public const string UnexpectedItemType = "W.UnexpectedItemType";
            public const string UndefinedValue = "W.UndefinedValue";
        }

        public static class UnknownMetadata
        {
            public const string BadNaming = "U.BadNaming";
            public const string UnexpectedType = "U.UnexpectedType";
        }
    }
}
