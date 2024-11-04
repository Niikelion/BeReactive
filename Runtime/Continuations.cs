using System;
using JetBrains.Annotations;

namespace Utils.BR
{
    [PublicAPI] public static class Continuations
    {
        public delegate TResult Transformer<in TArg, out TResult>([NotNull] TArg arg);
        public delegate TResult Factory<out TResult>();
        public delegate void Action<in TArg>([NotNull] TArg arg);
        
        public static TResult Let<TArg, TResult>(this TArg obj, Transformer<TArg, TResult> transformer) => 
            obj is null ? default : transformer(obj);
        public static TResult Let<TArg, TResult>(this TArg obj, Factory<TResult> factory) =>
            obj is null ? default : factory();
        public static void Run<TArg>(this TArg obj, Action<TArg> action)
        {
            if (obj is not null)
                action(obj);
        }
        public static void Run<TArg>(this TArg obj, Action action)
        {
            if (obj is not null)
                action();
        }

        public static TArg Also<TArg>(this TArg obj, Action<TArg> action)
        {
            if (obj is not null)
                action(obj);
            
            return obj;
        }
        public static TArg Also<TArg>(this TArg obj, Action action)
        {
            if (obj is not null)
                action();
            
            return obj;
        }
        
        public static TArg When<TArg>(this TArg obj, bool condition, Transformer<TArg, TArg> transformer) =>
            obj is null ? default : condition ? transformer(obj) : obj;
        public static TRes When<TArg, TRes>(this TArg obj, bool condition, Transformer<TRes, TRes> transformer) where TArg : TRes =>
            obj is null ? default : condition ? transformer(obj) : obj;
    }
}