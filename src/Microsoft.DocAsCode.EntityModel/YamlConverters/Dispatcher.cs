namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System;
    using System.Collections.Generic;

    internal sealed class Dispatcher<TSource, TResult>
    {
        private readonly List<Tuple<Func<TSource, bool>, Func<TSource, TResult>>> _list =
            new List<Tuple<Func<TSource, bool>, Func<TSource, TResult>>>();

        public OnMatchDispatcher Where(Func<TSource, bool> predicate)
        {
            return new OnMatchDispatcher(this, predicate);
        }

        public IEnumerable<TResult> Dispatch(IEnumerable<TSource> source)
        {
            var list = new List<Tuple<Func<TSource, bool>, Func<TSource, TResult>>>(_list);
            foreach (var item in source)
            {
                foreach (var tuple in list)
                {
                    if (tuple.Item1(item))
                    {
                        yield return tuple.Item2(item);
                        break;
                    }
                }
            }
        }

        internal sealed class OnMatchDispatcher
        {
            private readonly Dispatcher<TSource, TResult> _dispatcher;
            private readonly Func<TSource, bool> _predicate;

            internal OnMatchDispatcher(Dispatcher<TSource, TResult> dispatcher, Func<TSource, bool> predicate)
            {
                _dispatcher = dispatcher;
                _predicate = predicate;
            }

            public OnMatchDispatcher Where(Func<TSource, bool> predicate)
            {
                return new OnMatchDispatcher(_dispatcher, s=>_predicate(s) && predicate(s));
            }

            public Dispatcher<TSource, TResult> Select(Func<TSource, TResult> selector)
            {
                _dispatcher._list.Add(Tuple.Create(_predicate, selector));
                return _dispatcher;
            }
        }
    }
}
