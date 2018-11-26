// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Threading;

    [Serializable]
    public struct AmbientContext : IDisposable
    {
        private const string AMBCTX_NAME = nameof(AmbientContext);

        // auto increment counter maintained during branch and trace entry creation.
        private long[] _counterRef;

        private object[] _originalAmbientContext;

        public string Id { get; private set; }

        private AmbientContext(string id) : this()
        {
            Id = id;
            _counterRef =  new long[] { 0 };
            _originalAmbientContext = GetCurrentContextRaw();
            SetCurrentContextRaw(ToObjectArray());
        }

        internal AmbientContext(AmbientContext context) : this()
        {
            Id = context.Id;
            _counterRef = context._counterRef;
            _originalAmbientContext = GetCurrentContextRaw();
            SetCurrentContextRaw(ToObjectArray());
        }

        public static AmbientContext? CurrentContext => GetCurrentContext();

        public static AmbientContext GetOrCreateAmbientContext()
        {
            // no thread safety issue here because the TLS can only be initialized by the thread itself
            var context = GetCurrentContext() ?? (AmbientContext?)InitializeAmbientContext(null);
            return new AmbientContext(context.Value);
        }

        /// <summary>
        /// Initializes an ambient context and set the correlation id as current.
        /// </summary>
        /// <param name="serializedContext">Optional string of serialized context from upstream. The contents are copied to current context.</param>
        /// <returns>The current context which is just initialized.</returns>
        /// <exception cref="System.InvalidOperationException">An exception is thrown if the ambient context is initialized more than once.</exception>
        public static AmbientContext InitializeAmbientContext(string id)
        {
            var context = GetCurrentContext();

            if (context != null)
            {
                throw new InvalidOperationException("Cannot initialize an ambient context because it has already been initialized.");
            }

            return string.IsNullOrEmpty(id) ? new AmbientContext(Guid.NewGuid().ToString().ToUpperInvariant()) : new AmbientContext(id);
        }

        /// <summary>
        /// Generates the next id which correlates with other ids hierarchically.
        /// </summary>
        /// <returns>The generated correlation id.</returns>
        public string GenerateNextCorrelationId()
        {
            return string.Format("{0}.{1}", Id, Interlocked.Increment(ref _counterRef[0]).ToString());
        }

        /// <summary>
        /// Creates a branch context to be passed to downstream. A new correlation id is assigned to the branch context.
        /// </summary>
        /// <returns>The branch context.</returns>
        public AmbientContext CreateBranch()
        {
            return new AmbientContext(GenerateNextCorrelationId());
        }

        /// <summary>
        /// Removes the current ambient context.
        /// </summary>
        public void Dispose()
        {
            if (_originalAmbientContext == null)
            {
                RemoveAmbientContext();
            }
            else
            {
                SetCurrentContextRaw(_originalAmbientContext);
            }
        }

        private static AmbientContext? GetCurrentContext()
        {
            return ToAmbientContext(GetCurrentContextRaw());
        }

        private static object[] GetCurrentContextRaw()
        {
            return LogicalCallContext.GetData(AMBCTX_NAME) as object[];
        }

        private static void SetCurrentContextRaw(object[] raw)
        {
            LogicalCallContext.SetData(AMBCTX_NAME, raw);
        }

        private static void RemoveAmbientContext()
        {
            LogicalCallContext.FreeData(AMBCTX_NAME);
        }

        private object[] ToObjectArray()
        {
            return new object[]
            {
                Id,
                _counterRef,
                _originalAmbientContext,
            };
        }

        private static AmbientContext? ToAmbientContext(object[] objs)
        {
            if (objs == null || objs.Length < 3)
            {
                return null;
            }

            return new AmbientContext()
            {
                Id = objs[0] as string,
                _counterRef = objs[1] as long[],
                _originalAmbientContext = objs[2] as object[],
            };
        }
    }
}
