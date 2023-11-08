// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using System.Composition.Hosting;
using System.Reflection;

using Docfx.Plugins;

namespace Docfx.Common;

[Export(typeof(ICompositionContainer))]
class CompositionContainer : ICompositionContainer
{
    public static CompositionHost DefaultContainer { get; private set; }

    public CompositionHost Container { get; }

    public CompositionContainer()
        : this(null)
    {
    }

    public CompositionContainer(CompositionHost container)
    {
        Container = container;
    }

    public static T GetExport<T>(CompositionHost container, string name) =>
        (T)GetExport(container, typeof(T), name);

    public static object GetExport(CompositionHost container, Type type, string name)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(name);

        object exportedObject = null;
        try
        {
            exportedObject = container.GetExport(type, name);
        }
        catch (CompositionFailedException ex)
        {
            Logger.LogWarning($"Can't import: {name}, {ex}");
        }
        return exportedObject;
    }

    public static CompositionHost GetContainer(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var configuration = new ContainerConfiguration();
        foreach (var assembly in assemblies)
        {
            if (assembly != null)
            {
                configuration.WithAssembly(assembly);
            }
        }

        try
        {
            DefaultContainer = configuration.CreateContainer();
            return DefaultContainer;
        }
        catch (ReflectionTypeLoadException ex)
        {
            Logger.LogError($"Error when get composition container: {ex.Message}, loader exceptions: {(ex.LoaderExceptions != null ? string.Join(", ", ex.LoaderExceptions.Select(e => e.Message)) : "none")}");
            throw;
        }
    }

    public T GetExport<T>() => (Container ?? DefaultContainer).GetExport<T>();

    public T GetExport<T>(string name) => (Container ?? DefaultContainer).GetExport<T>(name);

    public IEnumerable<T> GetExports<T>() => (Container ?? DefaultContainer).GetExports<T>();

    public IEnumerable<T> GetExports<T>(string name) => (Container ?? DefaultContainer).GetExports<T>(name);
}
