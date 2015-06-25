﻿namespace docfx
{
    using Microsoft.DocAsCode.EntityModel;

    interface ISubCommand
    {
        ParseResult Exec(Options options);
    }

    enum SubCommandType
    {
        Init,
        Help,
        Metadata,
        Website,
    }
}
