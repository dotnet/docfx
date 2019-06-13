// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Net;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.SubCommands;

    using Newtonsoft.Json;

    internal class Program
    {
        static int Main(string[] args)
        {
            try
            {
                // TLS best practices for .NET: https://docs.microsoft.com/en-us/dotnet/framework/network-programming/tls
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

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
            var aggregatedLogListener = new AggregatedLogListener();
            Logger.RegisterListener(consoleLogListener);
            Logger.RegisterListener(aggregatedLogListener);

            CommandController controller = null;
            ISubCommand command;
            try
            {
                controller = ArgsParser.Instance.Parse(args);
                command = controller.Create();
            }
            catch (Exception e) when (e is System.IO.FileNotFoundException fe || e is DocfxException || e is JsonSerializationException)
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
                Logger.LogError(ex.ToString(), code: ErrorCodes.Build.FatalError);
                if (controller != null)
                {
                    Console.WriteLine(controller.GetHelpText());
                }
                return 1;
            }

            if (command.AllowReplay)
            {
                Logger.RegisterAsyncListener(new AggregatedLogListener(aggregatedLogListener));
            }

            Logger.UnregisterListener(aggregatedLogListener);

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
                Logger.LogError(e.Message, code: ErrorCodes.Build.FatalError);
                return 1;
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString(), code: ErrorCodes.Build.FatalError);
                return 1;
            }
            finally
            {
                scope?.Dispose();
            }
        }
    }
}
