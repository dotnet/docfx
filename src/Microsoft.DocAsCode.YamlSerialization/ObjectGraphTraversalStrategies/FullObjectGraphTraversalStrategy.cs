// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.ObjectGraphTraversalStrategies
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
#if !NetCore
    using System.Globalization;
#endif
    using System.Reflection;
    using System.Reflection.Emit;

    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    using Microsoft.DocAsCode.YamlSerialization.Helpers;
    using Microsoft.DocAsCode.YamlSerialization.ObjectDescriptors;

    using IObjectGraphVisitor = System.Object;
    using IObjectGraphVisitorContext = System.Object;

    /// <summary>
    /// An implementation of <see cref="IObjectGraphTraversalStrategy"/> that traverses
    /// readable properties, collections and dictionaries.
    /// </summary>
    public class FullObjectGraphTraversalStrategy : IObjectGraphTraversalStrategy
    {
        private static MethodInfo TraverseGenericDictionaryHelperMethod { get; } =
#if NetCore
            typeof(FullObjectGraphTraversalStrategy).GetTypeInfo().GetDeclaredMethod(nameof(TraverseGenericDictionaryHelper));
#else
            typeof(FullObjectGraphTraversalStrategy).GetMethod(nameof(TraverseGenericDictionaryHelper));
#endif
        protected YamlSerializer Serializer { get; }
        private readonly int _maxRecursion;
        private readonly ITypeInspector _typeDescriptor;
        private readonly ITypeResolver _typeResolver;
        private INamingConvention _namingConvention;
        private readonly Dictionary<Tuple<Type, Type>, Action<IObjectDescriptor, IObjectGraphVisitor, int, IObjectGraphVisitorContext>> _behaviorCache =
            new Dictionary<Tuple<Type, Type>, Action<IObjectDescriptor, IObjectGraphVisitor, int, IObjectGraphVisitorContext>>();
        private readonly Dictionary<Tuple<Type, Type, Type>, Action<FullObjectGraphTraversalStrategy, object, IObjectGraphVisitor, int, INamingConvention, IObjectGraphVisitorContext>> _traverseGenericDictionaryCache =
            new Dictionary<Tuple<Type, Type, Type>, Action<FullObjectGraphTraversalStrategy, object, IObjectGraphVisitor, int, INamingConvention, IObjectGraphVisitorContext>>();

        public FullObjectGraphTraversalStrategy(YamlSerializer serializer, ITypeInspector typeDescriptor, ITypeResolver typeResolver, int maxRecursion, INamingConvention namingConvention)
        {
            if (maxRecursion <= 0)
            {
                throw new ArgumentOutOfRangeException("maxRecursion", maxRecursion, "maxRecursion must be greater than 1");
            }

            Serializer = serializer;

            if (typeDescriptor == null)
            {
                throw new ArgumentNullException("typeDescriptor");
            }

            _typeDescriptor = typeDescriptor;

            if (typeResolver == null)
            {
                throw new ArgumentNullException("typeResolver");
            }

            _typeResolver = typeResolver;

            _maxRecursion = maxRecursion;
            _namingConvention = namingConvention;
        }

        void IObjectGraphTraversalStrategy.Traverse<TContext>(IObjectDescriptor graph, IObjectGraphVisitor<TContext> visitor, TContext context)
        {
            Traverse(graph, visitor, 0, context);
        }

        protected virtual void Traverse<TContext>(IObjectDescriptor value, IObjectGraphVisitor<TContext> visitor, int currentDepth, TContext context)
        {
            if (++currentDepth > _maxRecursion)
            {
                throw new InvalidOperationException("Too much recursion when traversing the object graph");
            }

            if (!visitor.Enter(value, context))
            {
                return;
            }

#if NetCore
            if (value.Type == typeof(bool) ||
                value.Type == typeof(byte) ||
                value.Type == typeof(short) ||
                value.Type == typeof(int) ||
                value.Type == typeof(long) ||
                value.Type == typeof(sbyte) ||
                value.Type == typeof(ushort) ||
                value.Type == typeof(uint) ||
                value.Type == typeof(ulong) ||
                value.Type == typeof(float) ||
                value.Type == typeof(double) ||
                value.Type == typeof(decimal) ||
                value.Type == typeof(string) ||
                value.Type == typeof(char) ||
                value.Type == typeof(DateTime) ||
                value.Type == typeof(TimeSpan))
            {
                visitor.VisitScalar(value, context);
            }
            else if (value.Value == null)
            {
                visitor.VisitScalar(new BetterObjectDescriptor(null, typeof(object), typeof(object)), context);
            }
            else
            {
                var underlyingType = Nullable.GetUnderlyingType(value.Type);
                if (underlyingType != null)
                {
                    // This is a nullable type, recursively handle it with its underlying type.
                    // Note that if it contains null, the condition above already took care of it
                    Traverse(new BetterObjectDescriptor(value.Value, underlyingType, value.Type, value.ScalarStyle), visitor, currentDepth, context);
                }
                else
                {
                    TraverseObject(value, visitor, currentDepth, context);
                }
            }
#else
            var typeCode = Type.GetTypeCode(value.Type);
            switch (typeCode)
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.String:
                case TypeCode.Char:
                case TypeCode.DateTime:
                    visitor.VisitScalar(value, context);
                    break;
                case TypeCode.DBNull:
                    visitor.VisitScalar(new BetterObjectDescriptor(null, typeof(object), typeof(object)), context);
                    break;
                case TypeCode.Empty:
                    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "TypeCode.{0} is not supported.", typeCode));
                default:
                    if (value.Value == null || value.Type == typeof(TimeSpan))
                    {
                        visitor.VisitScalar(value, context);
                        break;
                    }

                    var underlyingType = Nullable.GetUnderlyingType(value.Type);
                    if (underlyingType != null)
                    {
                        // This is a nullable type, recursively handle it with its underlying type.
                        // Note that if it contains null, the condition above already took care of it
                        Traverse(new BetterObjectDescriptor(value.Value, underlyingType, value.Type, value.ScalarStyle), visitor, currentDepth, context);
                    }
                    else
                    {
                        TraverseObject(value, visitor, currentDepth, context);
                    }
                    break;
            }
#endif
        }

        protected virtual void TraverseObject<TContext>(IObjectDescriptor value, IObjectGraphVisitor<TContext> visitor, int currentDepth, TContext context)
        {
            var key = Tuple.Create(value.Type, typeof(TContext));
            Action<IObjectDescriptor, IObjectGraphVisitor, int, IObjectGraphVisitorContext> action;
            if (!_behaviorCache.TryGetValue(key, out action))
            {
#if NetCore
                if (typeof(IDictionary).GetTypeInfo().IsAssignableFrom(value.Type.GetTypeInfo()))
#else
                if (typeof(IDictionary).IsAssignableFrom(value.Type))
#endif
                {
                    action = TraverseDictionary<TContext>;
                }
                else
                {
                    var dictionaryType = ReflectionUtility.GetImplementedGenericInterface(value.Type, typeof(IDictionary<,>));
                    if (dictionaryType != null)
                    {
                        action = (v, vi, d, c) => TraverseGenericDictionary<TContext>(v, dictionaryType, vi, d, c);
                    }
#if NetCore
                    else if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(value.Type.GetTypeInfo()))
#else
                    else if (typeof(IEnumerable).IsAssignableFrom(value.Type))
#endif
                    {
                        action = TraverseList<TContext>;
                    }
                    else
                    {
                        action = TraverseProperties<TContext>;
                    }
                }
                _behaviorCache[key] = action;
            }
            action(value, visitor, currentDepth, context);
        }

        protected virtual void TraverseDictionary<TContext>(IObjectDescriptor dictionary, object visitor, int currentDepth, object context)
        {
            var v = (IObjectGraphVisitor<TContext>)visitor;
            var c = (TContext)context;
            v.VisitMappingStart(dictionary, typeof(object), typeof(object), c);

            foreach (DictionaryEntry entry in (IDictionary)dictionary.Value)
            {
                var key = GetObjectDescriptor(entry.Key, typeof(object));
                var value = GetObjectDescriptor(entry.Value, typeof(object));

                if (v.EnterMapping(key, value, c))
                {
                    Traverse(key, v, currentDepth, c);
                    Traverse(value, v, currentDepth, c);
                }
            }

            v.VisitMappingEnd(dictionary, c);
        }

        private void TraverseGenericDictionary<TContext>(IObjectDescriptor dictionary, Type dictionaryType, IObjectGraphVisitor visitor, int currentDepth, IObjectGraphVisitorContext context)
        {
            var v = (IObjectGraphVisitor<TContext>)visitor;
            var c = (TContext)context;
#if NetCore
            var entryTypes = dictionaryType.GetTypeInfo().GenericTypeParameters;
#else
            var entryTypes = dictionaryType.GetGenericArguments();
#endif

            // dictionaryType is IDictionary<TKey, TValue>
            v.VisitMappingStart(dictionary, entryTypes[0], entryTypes[1], c);

            var key = Tuple.Create(entryTypes[0], entryTypes[1], typeof(TContext));
            Action<FullObjectGraphTraversalStrategy, object, IObjectGraphVisitor, int, INamingConvention, IObjectGraphVisitorContext> action;
            if (!_traverseGenericDictionaryCache.TryGetValue(key, out action))
            {
                action = GetTraverseGenericDictionaryHelper(entryTypes[0], entryTypes[1], typeof(TContext));
                _traverseGenericDictionaryCache[key] = action;
            }
            action(this, dictionary.Value, v, currentDepth, _namingConvention ?? new NullNamingConvention(), c);

            v.VisitMappingEnd(dictionary, c);
        }

        private static Action<FullObjectGraphTraversalStrategy, object, IObjectGraphVisitor, int, INamingConvention, IObjectGraphVisitorContext> GetTraverseGenericDictionaryHelper(Type tkey, Type tvalue, Type tcontext)
        {
            var dm = new DynamicMethod(string.Empty, typeof(void), new[] { typeof(FullObjectGraphTraversalStrategy), typeof(object), typeof(IObjectGraphVisitor), typeof(int), typeof(INamingConvention), typeof(IObjectGraphVisitorContext) });
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, typeof(IDictionary<,>).MakeGenericType(tkey, tvalue));
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Ldarg_S, (byte)4);
            il.Emit(OpCodes.Ldarg_S, (byte)5);
            il.Emit(OpCodes.Call, TraverseGenericDictionaryHelperMethod.MakeGenericMethod(tkey, tvalue, tcontext));
            il.Emit(OpCodes.Ret);
            return (Action<FullObjectGraphTraversalStrategy, object, IObjectGraphVisitor, int, INamingConvention, IObjectGraphVisitorContext>)dm.CreateDelegate(typeof(Action<FullObjectGraphTraversalStrategy, object, IObjectGraphVisitor, int, INamingConvention, IObjectGraphVisitorContext>));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void TraverseGenericDictionaryHelper<TKey, TValue, TContext>(
            FullObjectGraphTraversalStrategy self,
            IDictionary<TKey, TValue> dictionary,
            IObjectGraphVisitor visitor,
            int currentDepth,
            INamingConvention namingConvention,
            IObjectGraphVisitorContext context)
        {
            var v = (IObjectGraphVisitor<TContext>)visitor;
            var c = (TContext)context;
            var isDynamic = dictionary.GetType().FullName.Equals("System.Dynamic.ExpandoObject");
            foreach (var entry in dictionary)
            {
                var keyString = isDynamic ? namingConvention.Apply(entry.Key.ToString()) : entry.Key.ToString();
                var key = self.GetObjectDescriptor(keyString, typeof(TKey));
                var value = self.GetObjectDescriptor(entry.Value, typeof(TValue));

                if (v.EnterMapping(key, value, c))
                {
                    self.Traverse(key, v, currentDepth, c);
                    self.Traverse(value, v, currentDepth, c);
                }
            }
        }

        private void TraverseList<TContext>(IObjectDescriptor value, IObjectGraphVisitor visitor, int currentDepth, IObjectGraphVisitorContext context)
        {
            var v = (IObjectGraphVisitor<TContext>)visitor;
            var c = (TContext)context;
            var enumerableType = ReflectionUtility.GetImplementedGenericInterface(value.Type, typeof(IEnumerable<>));
            var itemType = enumerableType != null ?
#if NetCore
                enumerableType.GetTypeInfo().GenericTypeParameters[0]
#else
                enumerableType.GetGenericArguments()[0]
#endif
                : typeof(object);

            v.VisitSequenceStart(value, itemType, c);

            foreach (var item in (IEnumerable)value.Value)
            {
                Traverse(GetObjectDescriptor(item, itemType), v, currentDepth, c);
            }

            v.VisitSequenceEnd(value, c);
        }

        protected virtual void TraverseProperties<TContext>(IObjectDescriptor value, IObjectGraphVisitor visitor, int currentDepth, IObjectGraphVisitorContext context)
        {
            var v = (IObjectGraphVisitor<TContext>)visitor;
            var c = (TContext)context;
            v.VisitMappingStart(value, typeof(string), typeof(object), c);

            foreach (var propertyDescriptor in _typeDescriptor.GetProperties(value.Type, value.Value))
            {
                var propertyValue = propertyDescriptor.Read(value.Value);

                if (v.EnterMapping(propertyDescriptor, propertyValue, c))
                {
                    Traverse(new BetterObjectDescriptor(propertyDescriptor.Name, typeof(string), typeof(string)), v, currentDepth, c);
                    Traverse(propertyValue, v, currentDepth, c);
                }
            }

            v.VisitMappingEnd(value, c);
        }

        private IObjectDescriptor GetObjectDescriptor(object value, Type staticType)
        {
            return new BetterObjectDescriptor(value, _typeResolver.Resolve(staticType, value), staticType);
        }
    }
}