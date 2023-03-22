// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Exceptions;
using Microsoft.DocAsCode.Plugins;
using Microsoft.DocAsCode.SubCommands;

using Newtonsoft.Json;

namespace Microsoft.DocAsCode;

internal class Program
{
    internal static int Main(string[] args)
    {
        return ExecSubCommand(args);
    }

    internal static int ExecSubCommand(string[] args)
    {
        EnvironmentContext.SetVersion(typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

        var consoleLogListener = new ConsoleLogListener();
        Logger.RegisterListener(consoleLogListener);

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

            Logger.Flush();
            Logger.UnregisterAllListeners();

            if (command.AllowReplay)
            {
                Logger.PrintSummary();
            }

            return Logger.HasError ? -1 : 0;
        }
        catch (AggregateException ae)
        {
            foreach (var e in ae.Flatten().InnerExceptions)
            {
                LogExceptionError(e);
            }
            return 1;
        }
        catch (Exception e)
        {
            LogExceptionError(e);
            return 1;
        }
        finally
        {
            scope?.Dispose();
        }
    }

    private static void LogExceptionError(Exception exception)
    {
        if (exception is DocumentException)
        {
            return;
        }
        else if (exception is DocfxException docfxException)
        {
            Logger.LogError(docfxException.Message, code: ErrorCodes.Build.FatalError);
            return;
        }
        else
        {
            Logger.LogError(exception.ToString(), code: ErrorCodes.Build.FatalError);
        }
    }
}
