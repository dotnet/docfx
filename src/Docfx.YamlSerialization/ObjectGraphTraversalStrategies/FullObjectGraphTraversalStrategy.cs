// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Docfx.YamlSerialization.ObjectDescriptors;
using YamlDotNet.Core;
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

    private readonly int _maxRecursion;
    private readonly ITypeInspector _typeDescriptor;
    private readonly ITypeResolver _typeResolver;
    private readonly INamingConvention _namingConvention;
    private readonly IObjectFactory _objectFactory;

    // private readonly Dictionary<Tuple<Type, Type>, Action<IPropertyDescriptor?, IObjectDescriptor, IObjectGraphVisitor, Type, Type, IObjectGraphVisitorContext, Stack<FullObjectGraphTraversalStrategy.ObjectPathSegment>, ObjectSerializer>> _behaviorCache = new();
    private readonly Dictionary<Tuple<Type, Type, Type>, Action<FullObjectGraphTraversalStrategy, object, IObjectGraphVisitor, INamingConvention, IObjectGraphVisitorContext, Stack<ObjectPathSegment>, ObjectSerializer>> _traverseGenericDictionaryCache = new();

    protected YamlSerializer Serializer { get; }

    public FullObjectGraphTraversalStrategy(YamlSerializer serializer, ITypeInspector typeDescriptor, ITypeResolver typeResolver, int maxRecursion, INamingConvention namingConvention, IObjectFactory objectFactory)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRecursion);

        Serializer = serializer;

        _typeDescriptor = typeDescriptor;
        _typeResolver = typeResolver;

        _maxRecursion = maxRecursion;
        _namingConvention = namingConvention;
        _objectFactory = objectFactory;
    }

    void IObjectGraphTraversalStrategy.Traverse<TContext>(IObjectDescriptor graph, IObjectGraphVisitor<TContext> visitor, TContext context, ObjectSerializer serializer)
    {
        Traverse(null, "<root>", graph, visitor, context, new Stack<ObjectPathSegment>(_maxRecursion), serializer);
    }

    protected virtual void Traverse<TContext>(IPropertyDescriptor? propertyDescriptor, object name, IObjectDescriptor value, IObjectGraphVisitor<TContext> visitor, TContext context, Stack<ObjectPathSegment> path, ObjectSerializer serializer)
    {
        if (path.Count >= _maxRecursion)
        {
            var message = new StringBuilder();
            message.AppendLine("Too much recursion when traversing the object graph.");
            message.AppendLine("The path to reach this recursion was:");

            var lines = new Stack<KeyValuePair<string, string>>(path.Count);
            var maxNameLength = 0;
            foreach (var segment in path)
            {
                var segmentName = segment.Name?.ToString() ?? string.Empty;
                maxNameLength = Math.Max(maxNameLength, segmentName.Length);
                lines.Push(new KeyValuePair<string, string>(segmentName, segment.Value.Type.FullName!));
            }

            foreach (var line in lines)
            {
                message
                    .Append(" -> ")
                    .Append(line.Key.PadRight(maxNameLength))
                    .Append("  [")
                    .Append(line.Value)
                    .AppendLine("]");
            }

            throw new MaximumRecursionLevelReachedException(message.ToString());
        }

        if (!visitor.Enter(propertyDescriptor, value, context, serializer))
        {
            return;
        }

        path.Push(new ObjectPathSegment(name, value));

        try
        {
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
                    visitor.VisitScalar(value, context, serializer);
                    break;

                case TypeCode.DBNull:
                    visitor.VisitScalar(new BetterObjectDescriptor(null, typeof(object), typeof(object)), context, serializer);
                    break;

                case TypeCode.Empty:
                    throw new NotSupportedException($"TypeCode.{typeCode} is not supported.");

                case TypeCode.Object:
                default:
                    if (value.Value == null || value.Type == typeof(TimeSpan))
                    {
                        visitor.VisitScalar(value, context, serializer);
                        break;
                    }

                    var underlyingType = Nullable.GetUnderlyingType(value.Type);
                    if (underlyingType != null)
                    {
                        // This is a nullable type, recursively handle it with its underlying type.
                        // Note that if it contains null, the condition above already took care of it
                        Traverse(
                            propertyDescriptor,
                            "Value",
                            new BetterObjectDescriptor(value.Value, underlyingType, value.Type, value.ScalarStyle),
                            visitor,
                            context,
                            path,
                            serializer
                        );
                    }
                    else
                    {
                        TraverseObject(propertyDescriptor, value, visitor, context, path, serializer);
                    }
                    break;
            }
        }
        finally
        {
            path.Pop();
        }
    }

    protected virtual void TraverseObject<TContext>(
        IPropertyDescriptor? propertyDescriptor,
        IObjectDescriptor value,
        IObjectGraphVisitor<TContext> visitor,
        TContext context,
        Stack<ObjectPathSegment> path,
        ObjectSerializer serializer)
    {
        Debug.Assert(context != null);

        var key = Tuple.Create(value.Type, typeof(TContext));

        if (typeof(IDictionary).IsAssignableFrom(value.Type))
        {
            TraverseDictionary(propertyDescriptor, value, visitor, typeof(object), typeof(object), context, path, serializer);
            return;
        }

        if (_objectFactory.GetDictionary(value, out var adaptedDictionary, out var genericArguments))
        {
            Debug.Assert(genericArguments != null);

            var objectDescriptor = new ObjectDescriptor(adaptedDictionary, value.Type, value.StaticType, value.ScalarStyle);
            var dictionaryKeyType = genericArguments[0];
            var dictionaryValueType = genericArguments[1];

            TraverseDictionary(propertyDescriptor, objectDescriptor, visitor, dictionaryKeyType, dictionaryValueType, context, path, serializer);
            return;
        }

        if (typeof(IEnumerable).IsAssignableFrom(value.Type))
        {
            TraverseList(propertyDescriptor, value, visitor, context, path, serializer);
            return;
        }

        TraverseProperties(value, visitor, context, path, serializer);
    }

    protected virtual void TraverseDictionary<TContext>(
        IPropertyDescriptor? propertyDescriptor,
        IObjectDescriptor dictionary,
        IObjectGraphVisitor<TContext> visitor,
        Type keyType,
        Type valueType,
        TContext context,
        Stack<ObjectPathSegment> path,
        ObjectSerializer serializer)
    {
        visitor.VisitMappingStart(dictionary, keyType, valueType, context, serializer);

        var isDynamic = dictionary.Type.FullName!.Equals("System.Dynamic.ExpandoObject");
        foreach (DictionaryEntry? entry in (IDictionary)dictionary.NonNullValue())
        {
            var entryValue = entry!.Value;
            var keyValue = isDynamic ? _namingConvention.Apply(entryValue.Key.ToString()!) : entryValue.Key;
            var key = GetObjectDescriptor(keyValue, keyType);
            var value = GetObjectDescriptor(entryValue.Value, valueType);

            if (visitor.EnterMapping(key, value, context, serializer))
            {
                Traverse(propertyDescriptor, keyValue, key, visitor, context, path, serializer);
                Traverse(propertyDescriptor, keyValue, value, visitor, context, path, serializer);
            }
        }

        visitor.VisitMappingEnd(dictionary, context, serializer);
    }

    private void TraverseGenericDictionary<TContext>(
        IObjectDescriptor dictionary,
        Type dictionaryType,
        IObjectGraphVisitor<TContext> visitor,
        TContext context,
        Stack<ObjectPathSegment> path,
        ObjectSerializer serializer)
    {
        Debug.Assert(dictionary.Value != null);

        var entryTypes = dictionaryType.GetGenericArguments();

        // dictionaryType is IDictionary<TKey, TValue>
        visitor.VisitMappingStart(dictionary, entryTypes[0], entryTypes[1], context, serializer);

        var key = Tuple.Create(entryTypes[0], entryTypes[1], typeof(TContext));
        if (!_traverseGenericDictionaryCache.TryGetValue(key, out var action))
        {
            action = GetTraverseGenericDictionaryHelper(entryTypes[0], entryTypes[1], typeof(TContext));
            _traverseGenericDictionaryCache[key] = action;
        }
        action(this, dictionary.Value, visitor, _namingConvention ?? NullNamingConvention.Instance, context!, path, serializer);

        visitor.VisitMappingEnd(dictionary, context, serializer);
    }

    private static Action<FullObjectGraphTraversalStrategy, object, IObjectGraphVisitor, INamingConvention, IObjectGraphVisitorContext, Stack<ObjectPathSegment>, ObjectSerializer> GetTraverseGenericDictionaryHelper(Type tkey, Type tvalue, Type tcontext)
    {
        var dm = new DynamicMethod(
            string.Empty,
            returnType: typeof(void),
            parameterTypes:
            [
                typeof(FullObjectGraphTraversalStrategy),
                typeof(object),
                typeof(IObjectGraphVisitor),
                typeof(INamingConvention),
                typeof(IObjectGraphVisitorContext),
                typeof(Stack<ObjectPathSegment>),
                typeof(ObjectSerializer),
            ]);
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
        return (Action<FullObjectGraphTraversalStrategy, object, IObjectGraphVisitor, INamingConvention, IObjectGraphVisitorContext, Stack<ObjectPathSegment>, ObjectSerializer>)dm.CreateDelegate(typeof(Action<FullObjectGraphTraversalStrategy, object, IObjectGraphVisitor, INamingConvention, IObjectGraphVisitorContext, Stack<ObjectPathSegment>, ObjectSerializer>));
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    private static void TraverseGenericDictionaryHelper<TKey, TValue, TContext>(
        FullObjectGraphTraversalStrategy self,
        IPropertyDescriptor propertyDescriptor,
        IDictionary<TKey, TValue> dictionary,
        IObjectGraphVisitor<TContext> visitor,
        INamingConvention namingConvention,
        TContext context,
        Stack<ObjectPathSegment> paths,
        ObjectSerializer serializer)
    {

        var isDynamic = dictionary.GetType().FullName!.Equals("System.Dynamic.ExpandoObject");

        foreach (var entry in dictionary)
        {
            Debug.Assert(entry.Key != null);
            Debug.Assert(entry.Value != null);

            var keyString = isDynamic
                              ? namingConvention.Apply(entry.Key!.ToString()!)!
                              : entry.Key.ToString()!;
            var key = self.GetObjectDescriptor(keyString, typeof(TKey));
            var value = self.GetObjectDescriptor(entry.Value, typeof(TValue));

            if (visitor.EnterMapping(key, value, context, serializer))
            {
                self.Traverse(propertyDescriptor, propertyDescriptor.Name, key, visitor, context, paths, serializer);
                self.Traverse(propertyDescriptor, propertyDescriptor.Name, key, visitor, context, paths, serializer);
            }
        }
    }

    private void TraverseList<TContext>(
        IPropertyDescriptor? propertyDescriptor,
        IObjectDescriptor value,
        IObjectGraphVisitor<TContext> visitor,
        TContext context,
        Stack<ObjectPathSegment> path,
        ObjectSerializer serializer)
    {
        var itemType = _objectFactory.GetValueType(value.Type);

        visitor.VisitSequenceStart(value, itemType, context, serializer);

        var index = 0;

        foreach (var item in (IEnumerable)value.NonNullValue())
        {
            Traverse(propertyDescriptor, index, GetObjectDescriptor(item, itemType), visitor, context, path, serializer);
            ++index;
        }

        visitor.VisitSequenceEnd(value, context, serializer);
    }

    protected virtual void TraverseProperties<TContext>(
        IObjectDescriptor value,
        IObjectGraphVisitor<TContext> visitor,
        TContext context,
        Stack<ObjectPathSegment> path,
        ObjectSerializer serializer)
    {
        Debug.Assert(visitor != null);
        Debug.Assert(context != null);
        Debug.Assert(value.Value != null);

        if (context.GetType() != typeof(Nothing))
        {
            _objectFactory.ExecuteOnSerializing(value.Value);
        }

        visitor.VisitMappingStart(value, typeof(string), typeof(object), context, serializer);

        var source = value.NonNullValue();
        foreach (var propertyDescriptor in _typeDescriptor.GetProperties(value.Type, source))
        {
            var propertyValue = propertyDescriptor.Read(source);
            if (visitor.EnterMapping(propertyDescriptor, propertyValue, context, serializer))
            {
                Traverse(null, propertyDescriptor.Name, new BetterObjectDescriptor(propertyDescriptor.Name, typeof(string), typeof(string), ScalarStyle.Plain), visitor, context, path, serializer);
                Traverse(propertyDescriptor, propertyDescriptor.Name, propertyValue, visitor, context, path, serializer);
            }
        }

        visitor.VisitMappingEnd(value, context, serializer);

        if (context.GetType() != typeof(Nothing))
        {
            _objectFactory.ExecuteOnSerialized(value.Value);
        }
    }

    private IObjectDescriptor GetObjectDescriptor(object? value, Type staticType)
    {
        return new BetterObjectDescriptor(value, _typeResolver.Resolve(staticType, value), staticType);
    }

    protected internal readonly struct ObjectPathSegment
    {
        public readonly object Name;
        public readonly IObjectDescriptor Value;

        public ObjectPathSegment(object name, IObjectDescriptor value)
        {
            this.Name = name;
            this.Value = value;
        }
    }
}
