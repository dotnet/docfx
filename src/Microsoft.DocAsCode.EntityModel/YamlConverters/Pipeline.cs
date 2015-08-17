using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    public static class Pipeline
    {
        public static Pipeline<TArg, TContext, TArg> StartWith<TArg, TContext>()
        {
            return new StartWithPipeline<TArg, TContext>();
        }

        private sealed class StartWithPipeline<TArg, TContext> : Pipeline<TArg, TContext, TArg>
        {
            public override TArg Run(TArg arg, TContext context) => arg;
        }
    }

    public abstract class Pipeline<TArg, TContext, TResult>
    {
        public abstract TResult Run(TArg arg, TContext context);

        public Pipeline<TArg, TContext, TNewResult> Append<TNewResult>(IPipelineItem<TResult, TContext, TNewResult> item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            return new Body<TNewResult>(this, item);
        }

        public IPipelineItem<TArg, TContext, TResult> AsSubpipeline()
        {
            return new Subpipeline(this);
        }

        public ParallelPipeline AsParallel()
        {
            return new ParallelPipeline(this);
        }

        public ParallelPipeline<TNewResult> AsParallel<TNewResult>(Func<TResult, TNewResult> seedFunc)
        {
            return new ParallelPipeline<TNewResult>(this, seedFunc);
        }

        private sealed class Body<TNewResult> : Pipeline<TArg, TContext, TNewResult>
        {
            private readonly Pipeline<TArg, TContext, TResult> _pipeline;
            private readonly IPipelineItem<TResult, TContext, TNewResult> _item;

            public Body(Pipeline<TArg, TContext, TResult> pipeline, IPipelineItem<TResult, TContext, TNewResult> item)
            {
                _pipeline = pipeline;
                _item = item;
            }

            public override TNewResult Run(TArg arg, TContext context) =>
                _item.Exec(_pipeline.Run(arg, context), context);
        }

        private sealed class Subpipeline : IPipelineItem<TArg, TContext, TResult>
        {
            private readonly Pipeline<TArg, TContext, TResult> _pipeline;

            public Subpipeline(Pipeline<TArg, TContext, TResult> pipeline)
            {
                _pipeline = pipeline;
            }

            public TResult Exec(TArg arg, TContext context)
            {
                return _pipeline.Run(arg, context);
            }
        }

        public sealed class ParallelPipeline : Pipeline<TArg, TContext, TResult>
        {
            private readonly Pipeline<TArg, TContext, TResult> _pipeline;
            private readonly IPipelineItem<TResult, TContext, TResult>[] _children;

            internal ParallelPipeline(Pipeline<TArg, TContext, TResult> pipeline, params IPipelineItem<TResult, TContext, TResult>[] children)
            {
                _pipeline = pipeline;
                _children = children;
            }

            public override TResult Run(TArg arg, TContext context)
            {
                var result = _pipeline.Run(arg, context);
                Parallel.ForEach(_children, c => c.Exec(result, context));
                return result;
            }

            public ParallelPipeline AppendParallel(IPipelineItem<TResult, TContext, TResult> item)
            {
                if (item == null)
                {
                    throw new ArgumentNullException(nameof(item));
                }
                var children = new IPipelineItem<TResult, TContext, TResult>[_children.Length + 1];
                Array.Copy(_children, children, _children.Length);
                children[children.Length - 1] = item;
                return new ParallelPipeline(_pipeline, children);
            }
        }

        public sealed class ParallelPipeline<TNewResult> : Pipeline<TArg, TContext, TNewResult>
        {
            private readonly Pipeline<TArg, TContext, TResult> _pipeline;
            private readonly Func<TResult, TNewResult> _seedFunc;
            private readonly Action<TResult, TContext, StrongBox<TNewResult>>[] _funcs;

            internal ParallelPipeline(Pipeline<TArg, TContext, TResult> pipeline, Func<TResult, TNewResult> seedFunc, params Action<TResult, TContext, StrongBox<TNewResult>>[] func)
            {
                _pipeline = pipeline;
                _seedFunc = seedFunc;
                _funcs = func;
            }

            public override TNewResult Run(TArg arg, TContext context)
            {
                var r = _pipeline.Run(arg, context);
                var seedBox = new StrongBox<TNewResult>(_seedFunc(r));
                Parallel.ForEach(_funcs, f => f(r, context, seedBox));
                return seedBox.Value;
            }

            public Pipeline<TArg, TContext, TResult>.ParallelPipeline<TNewResult> AppendParallel<TTempResult>(
                IPipelineItem<TResult, TContext, TTempResult> item,
                Func<TTempResult, TNewResult, TNewResult> merger)
            {
                if (item == null)
                {
                    throw new ArgumentNullException(nameof(item));
                }
                Action<TResult, TContext, StrongBox<TNewResult>> func = (a, c, s) =>
                {
                    var tr = item.Exec(a, c);
                    lock (s)
                    {
                        s.Value = merger(tr, s.Value);
                    }
                };
                var funcs = new Action<TResult, TContext, StrongBox<TNewResult>>[_funcs.Length + 1];
                Array.Copy(_funcs, funcs, _funcs.Length);
                funcs[funcs.Length - 1] = func;
                return new Pipeline<TArg, TContext, TResult>.ParallelPipeline<TNewResult>(_pipeline, _seedFunc, funcs);
            }
        }
    }

    public interface IPipelineItem<in TArg, in TContext, out TResult>
    {
        TResult Exec(TArg arg, TContext context);
    }
}
