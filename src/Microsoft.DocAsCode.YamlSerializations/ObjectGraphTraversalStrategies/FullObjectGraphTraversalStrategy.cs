// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerializations.ObjectGraphTraversalStrategies
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Reflection;
    using System.Reflection.Emit;

    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    using Microsoft.DocAsCode.YamlSerializations.Helpers;
    using Microsoft.DocAsCode.YamlSerializations.ObjectDescriptors;

    /// <summary>
    /// An implementation of <see cref="IObjectGraphTraversalStrategy"/> that traverses
    /// readable properties, collections and dictionaries.
    /// </summary>
    public class FullObjectGraphTraversalStrategy : IObjectGraphTraversalStrategy
    {
        private static MethodInfo TraverseGenericDictionaryHelperMethod { get; } =
            typeof(FullObjectGraphTraversalStrategy).GetMethod(nameof(TraverseGenericDictionaryHelper));
        protected YamlSerializer Serializer { get; }
        private readonly int _maxRecursion;
        private readonly ITypeInspector _typeDescriptor;
        private readonly ITypeResolver _typeResolver;
        private INamingConvention _namingConvention;
        private readonly Dictionary<Type, Action<IObjectDescriptor, IObjectGraphVisitor, int>> _behaviorCache =
            new Dictionary<Type, Action<IObjectDescriptor, IObjectGraphVisitor, int>>();
        private readonly Dictionary<Tuple<Type, Type>, Action<FullObjectGraphTraversalStrategy, object, IObjectGraphVisitor, int, INamingConvention>> _traverseGenericDictionaryCache =
            new Dictionary<Tuple<Type, Type>, Action<FullObjectGraphTraversalStrategy, object, IObjectGraphVisitor, int, INamingConvention>>();

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

        void IObjectGraphTraversalStrategy.Traverse(IObjectDescriptor graph, IObjectGraphVisitor visitor)
        {
            Traverse(graph, visitor, 0);
        }

        protected virtual void Traverse(IObjectDescriptor value, IObjectGraphVisitor visitor, int currentDepth)
        {
            if (++currentDepth > _maxRecursion)
            {
                throw new InvalidOperationException("Too much recursion when traversing the object graph");
            }

            if (!visitor.Enter(value))
            {
                return;
            }

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
                    visitor.VisitScalar(value);
                    break;

                case TypeCode.DBNull:
                    visitor.VisitScalar(new BetterObjectDescriptor(null, typeof(object), typeof(object)));
                    break;

                case TypeCode.Empty:
                    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "TypeCode.{0} is not supported.", typeCode));

                default:
                    if (value.Value == null || value.Type == typeof(TimeSpan))
                    {
                        visitor.VisitScalar(value);
                        break;
                    }

                    var underlyingType = Nullable.GetUnderlyingType(value.Type);
                    if (underlyingType != null)
                    {
                        // This is a nullable type, recursively handle it with its underlying type.
                        // Note that if it contains null, the condition above already took care of it
                        Traverse(new BetterObjectDescriptor(value.Value, underlyingType, value.Type, value.ScalarStyle), visitor, currentDepth);
                    }
                    else
                    {
                        TraverseObject(value, visitor, currentDepth);
                    }
                    break;
            }
        }

        protected virtual void TraverseObject(IObjectDescriptor value, IObjectGraphVisitor visitor, int currentDepth)
        {
            Action<IObjectDescriptor, IObjectGraphVisitor, int> action;
            if (!_behaviorCache.TryGetValue(value.Type, out action))
            {
                if (typeof(IDictionary).IsAssignableFrom(value.Type))
                {
                    action = TraverseDictionary;
                }
                else
                {
                    var dictionaryType = ReflectionUtility.GetImplementedGenericInterface(value.Type, typeof(IDictionary<,>));
                    if (dictionaryType != null)
                    {
                        action = (v, vi, d) => TraverseGenericDictionary(v, dictionaryType, vi, d);
                    }
                    else if (typeof(IEnumerable).IsAssignableFrom(value.Type))
                    {
                        action = TraverseList;
                    }
                    else
                    {
                        action = TraverseProperties;
                    }
                }
                _behaviorCache[value.Type] = action;
            }
            action(value, visitor, currentDepth);
        }

        protected virtual void TraverseDictionary(IObjectDescriptor dictionary, IObjectGraphVisitor visitor, int currentDepth)
        {
            visitor.VisitMappingStart(dictionary, typeof(object), typeof(object));

            foreach (DictionaryEntry entry in (IDictionary)dictionary.Value)
            {
                var key = GetObjectDescriptor(entry.Key, typeof(object));
                var value = GetObjectDescriptor(entry.Value, typeof(object));

                if (visitor.EnterMapping(key, value))
                {
                    Traverse(key, visitor, currentDepth);
                    Traverse(value, visitor, currentDepth);
                }
            }

            visitor.VisitMappingEnd(dictionary);
        }

        private void TraverseGenericDictionary(IObjectDescriptor dictionary, Type dictionaryType, IObjectGraphVisitor visitor, int currentDepth)
        {
            var entryTypes = dictionaryType.GetGenericArguments();

            // dictionaryType is IDictionary<TKey, TValue>
            visitor.VisitMappingStart(dictionary, entryTypes[0], entryTypes[1]);

            var key = Tuple.Create(entryTypes[0], entryTypes[1]);
            Action<FullObjectGraphTraversalStrategy, object, IObjectGraphVisitor, int, INamingConvention> action;
            if (!_traverseGenericDictionaryCache.TryGetValue(key, out action))
            {
                action = GetTraverseGenericDictionaryHelper(entryTypes[0], entryTypes[1]);
                _traverseGenericDictionaryCache[key] = action;
            }
            action(this, dictionary.Value, visitor, currentDepth, _namingConvention ?? new NullNamingConvention());

            visitor.VisitMappingEnd(dictionary);
        }

        private static Action<FullObjectGraphTraversalStrategy, object, IObjectGraphVisitor, int, INamingConvention> GetTraverseGenericDictionaryHelper(Type tkey, Type tvalue)
        {
            var dm = new DynamicMethod(string.Empty, typeof(void), new[] { typeof(FullObjectGraphTraversalStrategy), typeof(object), typeof(IObjectGraphVisitor), typeof(int), typeof(INamingConvention) });
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, typeof(IDictionary<,>).MakeGenericType(tkey, tvalue));
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Ldarg_S, (byte)4);
            il.Emit(OpCodes.Call, TraverseGenericDictionaryHelperMethod.MakeGenericMethod(tkey, tvalue));
            il.Emit(OpCodes.Ret);
            return (Action<FullObjectGraphTraversalStrategy, object, IObjectGraphVisitor, int, INamingConvention>)dm.CreateDelegate(typeof(Action<FullObjectGraphTraversalStrategy, object, IObjectGraphVisitor, int, INamingConvention>));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void TraverseGenericDictionaryHelper<TKey, TValue>(
            FullObjectGraphTraversalStrategy self,
            IDictionary<TKey, TValue> dictionary,
            IObjectGraphVisitor visitor, int currentDepth, INamingConvention namingConvention)
        {
            var isDynamic = dictionary.GetType().FullName.Equals("System.Dynamic.ExpandoObject");
            foreach (var entry in dictionary)
            {
                var keyString = isDynamic ? namingConvention.Apply(entry.Key.ToString()) : entry.Key.ToString();
                var key = self.GetObjectDescriptor(keyString, typeof(TKey));
                var value = self.GetObjectDescriptor(entry.Value, typeof(TValue));

                if (visitor.EnterMapping(key, value))
                {
                    self.Traverse(key, visitor, currentDepth);
                    self.Traverse(value, visitor, currentDepth);
                }
            }
        }

        private void TraverseList(IObjectDescriptor value, IObjectGraphVisitor visitor, int currentDepth)
        {
            var enumerableType = ReflectionUtility.GetImplementedGenericInterface(value.Type, typeof(IEnumerable<>));
            var itemType = enumerableType != null ? enumerableType.GetGenericArguments()[0] : typeof(object);

            visitor.VisitSequenceStart(value, itemType);

            foreach (var item in (IEnumerable)value.Value)
            {
                Traverse(GetObjectDescriptor(item, itemType), visitor, currentDepth);
            }

            visitor.VisitSequenceEnd(value);
        }

        protected virtual void TraverseProperties(IObjectDescriptor value, IObjectGraphVisitor visitor, int currentDepth)
        {
            visitor.VisitMappingStart(value, typeof(string), typeof(object));

            foreach (var propertyDescriptor in _typeDescriptor.GetProperties(value.Type, value.Value))
            {
                var propertyValue = propertyDescriptor.Read(value.Value);

                if (visitor.EnterMapping(propertyDescriptor, propertyValue))
                {
                    Traverse(new BetterObjectDescriptor(propertyDescriptor.Name, typeof(string), typeof(string)), visitor, currentDepth);
                    Traverse(propertyValue, visitor, currentDepth);
                }
            }

            visitor.VisitMappingEnd(value);
        }

        private IObjectDescriptor GetObjectDescriptor(object value, Type staticType)
        {
            return new BetterObjectDescriptor(value, _typeResolver.Resolve(staticType, value), staticType);
        }
    }
}