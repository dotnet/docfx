// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.Remoting.Messaging;
    using System.Threading;

    [Serializable]
    public sealed class AmbientContext : IDisposable
    {
        private static readonly string AMBCTX_NAME = nameof(AmbientContext);
        
        // auto increment counter maintained during branch and trace entry creation.
        private long _counter = 0;

        private readonly AmbientContext _originalAmbientContext;
        private readonly string _id;

        private AmbientContext() : this(Guid.NewGuid().ToString().ToUpperInvariant())
        {
        }

        private AmbientContext(string id)
        {
            _id = id;
            _originalAmbientContext = GetCurrentContext();
            SetAmbientContext(this);
        }

        public static AmbientContext CurrentContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // no thread safety issue here because the TLS can only be initialized by the thread itself
                var context = GetCurrentContext();
                if (context == null)
                {
                    context = InitializeAmbientContext(null);
                }

                return context;
            }
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

            return string.IsNullOrEmpty(id) ? new AmbientContext() : new AmbientContext(id);
        }

        public static void RemoveAmbientContext()
        {
            CallContext.FreeNamedDataSlot(AMBCTX_NAME);
        }

        public static AmbientContext GetCurrentContext()
        {
            return CallContext.LogicalGetData(AMBCTX_NAME) as AmbientContext;
        }

        /// <summary>
        /// Generates the next id which correlates with other ids hierarchically.
        /// </summary>
        /// <returns>The generated correlation id.</returns>
        public string GenerateNextCorrelationId()
        {
            return string.Format("{0}.{1}", _id, Interlocked.Increment(ref _counter));
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
                SetAmbientContext(_originalAmbientContext);
            }
        }

        private static void SetAmbientContext(AmbientContext context)
        {
            CallContext.LogicalSetData(AMBCTX_NAME, context);
        }
    }
}
