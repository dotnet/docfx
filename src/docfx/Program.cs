// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;

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
                var result = ExecSubCommand(args);
                return Logger.HasError ? 1 : result;
            }
            finally
            {
                Logger.Flush();
                Logger.UnregisterAllListeners();
            }
        }

        internal static int ExecSubCommand(string[] args)
        {
            EnvironmentContext.SetVersion(typeof(Program).Assembly.GetName().Version.ToString());

            var consoleLogListener = new ConsoleLogListener();
            Logger.RegisterListener(consoleLogListener);

            CommandController controller = null;
            ISubCommand command;
            try
            {
                controller = ArgsParser.Instance.Parse(args);
                command = controller.Create();
            }
            catch (Exception e) when (e is System.IO.FileNotFoundException fe || e is DocfxException)
            {
                Logger.LogError(e.Message);
                return 1;
            }
            catch (Exception e) when (e is OptionParserException || e is InvalidOptionException)
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

            if (command.AllowReplay)
            {
                Logger.RegisterAsyncListener(new AggregatedLogListener());
            }

            var context = new SubCommandRunningContext();
            PerformanceScope scope = null;
            try
            {
                // TODO: For now reuse AllowReplay for overall elapsed time statistics
                if (command.AllowReplay)
                {
                    scope = new PerformanceScope(string.Empty, LogLevel.Info);
                }

                command.Exec(context);
                return 0;
            }
            catch (Exception e) when (e is DocumentException || e is DocfxException)
            {
                Logger.LogError(e.Message);
                return 1;
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
                return 1;
            }
            finally
            {
                scope?.Dispose();
            }
        }
    }
}
