namespace Microsoft.DocAsCode
{
    using System;

    class SubCommandFactory
    {
        public static ISubCommand GetCommand(SubCommandType type)
        {
            switch (type)
            {
                case SubCommandType.Init:
                    return new InitSubCommand();
                case SubCommandType.Help:
                    return new HelpSubCommand();
                case SubCommandType.Metadata:
                    return new MetadataSubCommand();
                case SubCommandType.Website:
                    return new WebsiteSubCommand();
                default:
                    throw new NotSupportedException("SubCommandType: " + type.ToString(), null);
            }
        }
    }
}
