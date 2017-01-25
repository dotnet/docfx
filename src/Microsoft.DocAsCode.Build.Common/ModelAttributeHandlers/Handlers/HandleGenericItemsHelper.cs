// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class HandleGenericItemsHelper
    {
        public static bool EnumerateIEnumerable(object currentObj, Func<object, object> handler)
        {
            if (currentObj == null)
            {
                throw new ArgumentNullException(nameof(currentObj));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return HandleItems(typeof(IEnumerable<>), typeof(EnumerateIEnumerableItems<>), currentObj, handler);
        }

        public static bool EnumerateIDictionary(object currentObj, Func<object, object> handler)
        {
            if (currentObj == null)
            {
                throw new ArgumentNullException(nameof(currentObj));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return HandleItems(typeof(IDictionary<,>), typeof(EnumerateIDictionaryItems<,>), currentObj, handler);
        }

        public static bool EnumerateIReadonlyDictionary(object currentObj, Func<object, object> handler)
        {
            if (currentObj == null)
            {
                throw new ArgumentNullException(nameof(currentObj));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return HandleItems(typeof(IReadOnlyDictionary<,>), typeof(EnumerateIReadonlyDictionaryItems<,>), currentObj, handler);
        }

        public static bool HandleIList(object currentObj, Func<object, object> handler)
        {
            if (currentObj == null)
            {
                throw new ArgumentNullException(nameof(currentObj));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return HandleItems(typeof(IList<>), typeof(HandleIListItems<>), currentObj, handler);
        }

        public static bool HandleIDictionary(object currentObj, Func<object, object> handler)
        {
            if (currentObj == null)
            {
                throw new ArgumentNullException(nameof(currentObj));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return HandleItems(typeof(IDictionary<,>), typeof(HandleIDictionaryItems<,>), currentObj, handler);
        }

        private static bool HandleItems(Type genericInterface, Type implHandlerType, object currentObj, Func<object, object> handler)
        {
            if (currentObj == null)
            {
                throw new ArgumentNullException(nameof(currentObj));
            }
            var type = currentObj.GetType();
            var genericType = ReflectionHelper.GetGenericType(type, genericInterface);
            if (genericType != null)
            {
                var instance = (IHandleItems)ReflectionHelper.CreateInstance(implHandlerType, genericType.GenericTypeArguments, new[] { genericType }, new object[] { currentObj });
                if (instance != null)
                {
                    instance.Handle(handler);
                    return true;
                }
            }
            return false;
        }

        private interface IHandleItems
        {
            void Handle(Func<object, object> handler);
        }

        public sealed class EnumerateIEnumerableItems<TValue> : IHandleItems
        {
            private readonly IList<TValue> _list;

            public EnumerateIEnumerableItems(IEnumerable<TValue> list)
            {
                _list = list.ToList();
            }

            public void Handle(Func<object, object> enumerate)
            {
                Enumerate(s => (TValue)enumerate(s));
            }

            private void Enumerate(Func<TValue, TValue> enumerate)
            {
                foreach (var item in _list)
                {
                    enumerate(item);
                }
            }
        }

        public sealed class EnumerateIDictionaryItems<TKey, TValue> : IHandleItems
        {
            private readonly EnumerateIEnumerableItems<TValue> _enumerateItems;

            public EnumerateIDictionaryItems(IDictionary<TKey, TValue> dict)
            {
                _enumerateItems = new EnumerateIEnumerableItems<TValue>(dict.Values);
            }

            public void Handle(Func<object, object> enumerate)
            {
                _enumerateItems.Handle(s => (TValue)enumerate(s));
            }
        }

        public sealed class EnumerateIReadonlyDictionaryItems<TKey, TValue> : IHandleItems
        {
            private readonly EnumerateIEnumerableItems<TValue> _enumerateItems;

            public EnumerateIReadonlyDictionaryItems(IReadOnlyDictionary<TKey, TValue> dict)
            {
                _enumerateItems = new EnumerateIEnumerableItems<TValue>(dict.Values);
            }

            public void Handle(Func<object, object> enumerate)
            {
                _enumerateItems.Handle(s => (TValue)enumerate(s));
            }
        }

        public sealed class HandleIListItems<T> : IHandleItems
        {
            private readonly IList<T> _list;

            public HandleIListItems(IList<T> list)
            {
                _list = list;
            }

            public void Handle(Func<object, object> handler)
            {
                Handle(s => (T)handler((T)s));
            }

            private void Handle(Func<T, T> handler)
            {
                for (int i = 0; i < _list.Count; i++)
                {
                    _list[i] = handler(_list[i]);
                }
            }
        }

        public sealed class HandleIDictionaryItems<TKey, TValue> : IHandleItems
        {
            private readonly IDictionary<TKey, TValue> _dict;

            public HandleIDictionaryItems(IDictionary<TKey, TValue> dict)
            {
                _dict = dict;
            }

            public void Handle(Func<object, object> handler)
            {
                Handle(s => (TValue)handler(s));
            }

            private void Handle(Func<TValue, TValue> handler)
            {
                foreach (var key in _dict.Keys.ToList())
                {
                    _dict[key] = handler(_dict[key]);
                }
            }
        }
    }
}
