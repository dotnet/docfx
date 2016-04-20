// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Threading;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.SubCommands;

    internal class Program
    {
        static int Main(string[] args)
        {
            try
            {
                return ExecSubCommand(args);
            }
            finally
            {
                Logger.Flush();
                Logger.UnregisterAllListeners();
            }
        }

        private static int ExecSubCommand(string[] args)
        {
            var consoleLogListener = new ConsoleLogListener();
            var replayListener = new ReplayLogListener();
            replayListener.AddListener(consoleLogListener);
            Logger.RegisterListener(replayListener);

            CommandController controller = null;
            ISubCommand command;
            try
            {
                controller = ArgsParser.Instance.Parse(args);
                command = controller.Create();
            }
            catch (System.IO.FileNotFoundException fe)
            {
                Logger.LogError(fe.Message);
                return 1;
            }
            catch (OptionParserException e)
            {
                Logger.LogError(e.Message);
                if (controller != null)
                {
                    Console.WriteLine(controller.GetHelpText());
                }
                return 1;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
                if (controller != null)
                {
                    Console.WriteLine(controller.GetHelpText());
                }
                return 1;
            }

            replayListener.Replay = command.AllowReplay;

            var context = new SubCommandRunningContext();
            try
            {
                ThreadPool.SetMinThreads(4, 4);
                using (new PerformanceScope("executing", LogLevel.Info))
                {
                    command.Exec(context);
                }

                return 0;
            }
            catch (DocumentException de)
            {
                Logger.LogError(de.Message);
                return 1;
            }
            catch (DocfxException de)
            {
                Logger.LogError(de.Message);
                return 1;
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
                return 1;
            }
        }
    }
}
