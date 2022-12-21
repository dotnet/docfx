// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.SubCommands
{
    internal sealed class ServeCommand : ISubCommand
    {
        private readonly ServeCommandOptions _options;
        public bool AllowReplay => false;

        public string Name { get; } = nameof(ServeCommand);

        public ServeCommand(ServeCommandOptions options)
        {
            _options = options;
        }

        public void Exec(SubCommandRunningContext context)
        {
            RunServe.Exec(
                _options.Folder,
                _options.Host,
                _options.Port.HasValue ? _options.Port.Value.ToString() : null);
        }
    }
}
