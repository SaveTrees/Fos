namespace Fos.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Reflection;
    using System.Threading.Tasks;
    using Logging;

    /// <summary>
    /// 
    /// </summary>
    internal class UntypedMiddlewareBuilder : MiddlewareBuilder
    {
        public UntypedMiddlewareBuilder(Delegate untypedHandler, IServerLogger logger, IOwinParameterConverter owinParameterConverter, params object[] args)
            : base(logger, owinParameterConverter, args)
        {
            Contract.Requires(logger == null);
            Contract.Requires(untypedHandler == null);

            Contract.Ensures(_untypedHandler == null);

            _untypedHandler = untypedHandler;
            //Log.CurrentLogger.Debug()("_untypedHandler: " + _untypedHandler);

            if (InvokeHandler == null)
            {
                throw new NotSupportedException("The delegate middleware (" + _untypedHandler + ") is missing an appropriate 'Invoke' method.");
            }
        }

        /// <summary>
        /// If this middleware is a simple Delegate type, then this field contains it.
        /// </summary>
        private readonly Delegate _untypedHandler;

        protected override object Instance { get { return _untypedHandler; } }

        protected override Func<IDictionary<string, object>, Task> GetInvokeHandler()
        {
            var handler = GetCommonInvokeHandler(new List<MethodInfo>
                                                 {
                                                     _untypedHandler.Method
                                                 });
            if (handler == null)
            {
                // NancyFx uses the delegate below
                var nancyInvokeHandler = _untypedHandler as Func<Func<IDictionary<string, object>, Task>, Func<IDictionary<string, object>, Task>>;
                if (nancyInvokeHandler != null)
                {
                    //Log.CurrentLogger.Debug()("Using the untyped NancyFX handler.");

                    return environment =>
                           {
                               return nancyInvokeHandler(Next == null ? null : Next.InvokeHandler)(environment);
                           };
                }

                // Simple.Web uses this type of delegate
                var simpleWebHandler = _untypedHandler as Func<IDictionary<string, object>, Func<IDictionary<string, object>, Task>, Task>;
                if (simpleWebHandler != null)
                {
                    //Log.CurrentLogger.Debug()("Using the untyped SimpleWeb handler.");

                    return environment =>
                           {
                               return simpleWebHandler(environment, Next == null ? null : Next.InvokeHandler);
                           };
                }
            }

            throw new NotSupportedException("No suitable untyped handler could be mapped.");
        }
    }
}