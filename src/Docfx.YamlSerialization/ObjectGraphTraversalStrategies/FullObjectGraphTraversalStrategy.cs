// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Docfx.YamlSerialization.Helpers;
using Docfx.YamlSerialization.ObjectDescriptors;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using IObjectGraphVisitor = System.Object;
using IObjectGraphVisitorContext = System.Object;

namespace Docfx.YamlSerialization.ObjectGraphTraversalStrategies;

/// <summary>
/// An implementation of <see cref="IObjectGraphTraversalStrategy"/> that traverses
/// readable properties, collections and dictionaries.
/// </summary>
public class FullObjectGraphTraversalStrategy : IObjectGraphTraversalStrategy
{
    private static MethodInfo TraverseGenericDictionaryHelperMethod { get; } =
        typeof(FullObjectGraphTraversalStrategy).GetMethod(nameof(TraverseGenericDictionaryHelper))!;
    protected YamlSerializer Serializer { get; }
    private readonly int _maxRecursion;
    private readonly ITypeInspector _typeDescriptor;
    private readonly ITypeResolver _typeResolver;
    private readonly INamingConvention _namingConvention;
    private readonly Dictionary<Tuple<Type, Type>, Action<IObjectDescriptor, IObjectGraphVisitor, int, IObjectGraphVisitorContext>> _behaviorCache = [];
    private readonly Dictionary<Tuple<Type, Type, Type>, Action<FullObjectGraphTraversalStrategy, object?, IObjectGraphVisitor, int, INamingConvention, IObjectGraphVisitorContext>> _traverseGenericDictionaryCache = [];

    public FullObjectGraphTraversalStrategy(YamlSerializer serializer, ITypeInspector typeDescriptor, ITypeResolver typeResolver, int maxRecursion, INamingConvention? namingConvention)
    {
        if (maxRecursion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRecursion), maxRecursion, "maxRecursion must be greater than 1");
        }

        ArgumentNullException.ThrowIfNull(typeDescriptor);
        ArgumentNullException.ThrowIfNull(typeResolver);

        Serializer = serializer;
        _typeDescriptor = typeDescriptor;
        _typeResolver = typeResolver;
        _maxRecursion = maxRecursion;
        _namingConvention = namingConvention ?? NullNamingConvention.Instance;
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
    }

    protected virtual void TraverseObject<TContext>(IObjectDescriptor value, IObjectGraphVisitor<TContext> visitor, int currentDepth, TContext context)
    {
        var key = Tuple.Create(value.Type, typeof(TContext));
        if (!_behaviorCache.TryGetValue(key, out var action))
        {
            if (typeof(IDictionary).IsAssignableFrom(value.Type))
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
                else if (typeof(IEnumerable).IsAssignableFrom(value.Type))
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
        action(value, visitor, currentDepth, context!);
    }

    protected virtual void TraverseDictionary<TContext>(IObjectDescriptor dictionary, object visitor, int currentDepth, object context)
    {
        var v = (IObjectGraphVisitor<TContext>)visitor;
        var c = (TContext)context;
        v.VisitMappingStart(dictionary, typeof(object), typeof(object), c);

        foreach (DictionaryEntry entry in (IDictionary)dictionary.NonNullValue())
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
        var entryTypes = dictionaryType.GetGenericArguments();

        // dictionaryType is IDictionary<TKey, TValue>
        v.VisitMappingStart(dictionary, entryTypes[0], entryTypes[1], c);

        var key = Tuple.Create(entryTypes[0], entryTypes[1], typeof(TContext));
        if (!_traverseGenericDictionaryCache.TryGetValue(key, out var action))
        {
            action = GetTraverseGenericDictionaryHelper(entryTypes[0], entryTypes[1], typeof(TContext));
            _traverseGenericDictionaryCache[key] = action;
        }
        action(this, dictionary.Value, v, currentDepth, _namingConvention, c);

        v.VisitMappingEnd(dictionary, c);
    }

    private static Action<FullObjectGraphTraversalStrategy, object?, IObjectGraphVisitor, int, INamingConvention, IObjectGraphVisitorContext> GetTraverseGenericDictionaryHelper(Type tkey, Type tvalue, Type tcontext)
    {
        var dm = new DynamicMethod(string.Empty, typeof(void), [typeof(FullObjectGraphTraversalStrategy), typeof(object), typeof(IObjectGraphVisitor), typeof(int), typeof(INamingConvention), typeof(IObjectGraphVisitorContext)]);
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
        return (Action<FullObjectGraphTraversalStrategy, object?, IObjectGraphVisitor, int, INamingConvention, IObjectGraphVisitorContext>)dm.CreateDelegate(typeof(Action<FullObjectGraphTraversalStrategy, object?, IObjectGraphVisitor, int, INamingConvention, IObjectGraphVisitorContext>));
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
        var isDynamic = dictionary.GetType().FullName!.Equals("System.Dynamic.ExpandoObject");
        foreach (var entry in dictionary)
        {
            var keyString = isDynamic ? namingConvention.Apply(entry.Key!.ToString()!) : entry.Key!.ToString();
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
            enumerableType.GetGenericArguments()[0]
            : typeof(object);

        v.VisitSequenceStart(value, itemType, c);

        foreach (var item in (IEnumerable)value.NonNullValue())
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

        var source = value.NonNullValue();
        foreach (var propertyDescriptor in _typeDescriptor.GetProperties(value.Type, value.Value))
        {
            var propertyValue = propertyDescriptor.Read(source);

            if (v.EnterMapping(propertyDescriptor, propertyValue, c))
            {
                Traverse(new BetterObjectDescriptor(propertyDescriptor.Name, typeof(string), typeof(string)), v, currentDepth, c);
                Traverse(propertyValue, v, currentDepth, c);
            }
        }

        v.VisitMappingEnd(value, c);
    }

    private IObjectDescriptor GetObjectDescriptor(object? value, Type staticType)
    {
        return new BetterObjectDescriptor(value, _typeResolver.Resolve(staticType, value), staticType);
    }
}
