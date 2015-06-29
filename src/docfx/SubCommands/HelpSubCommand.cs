namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using System;

    class HelpSubCommand : ISubCommand
    {
        public ParseResult Exec(Options options)
        {
            var helpOptions = options.HelpVerb;
            string text = HelpTextGenerator.GetHelpMessage(options, helpOptions.Command);
            options.CurrentSubCommand = SubCommandType.Help;
            Console.WriteLine(text);
            return ParseResult.SuccessResult;
        }
    }
}
