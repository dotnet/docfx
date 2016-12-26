﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Composition.Hosting;
    using System.Linq;
    using System.Reflection;

    public static class CompositionUtility
    {
        public static T GetExport<T>(CompositionHost container, string name) where T : class =>
            (T)GetExport(container, typeof(T), name);

        public static object GetExport(CompositionHost container, Type type, string name)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

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
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

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
                return configuration.CreateContainer();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Logger.LogError($"Error when get composition container: {ex.Message}, loader exceptions: {(ex.LoaderExceptions != null ? string.Join(", ", ex.LoaderExceptions.Select(e => e.Message)) : "none")}");
                throw;
            }
        }
    }
}
