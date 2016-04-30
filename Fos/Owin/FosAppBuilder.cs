//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using Owin;

//namespace Fos.Owin
//{
//    using Logging;
//    using Microsoft.Owin;
//    using Middleware;
//    using OwinMiddleware = Middleware.OwinMiddleware;

//    internal class FosAppBuilder : IAppBuilder
//    {
//        private readonly IServerLogger _logger;
//        private readonly Dictionary<string, object> _properties;

//        public CancellationToken OnAppDisposing { get; }

//        private readonly FosOwinRoot _rootMiddleware;

//        /// <summary>
//        /// This is the last middleware added through <see cref="Use"/>, or the <see cref="_rootMiddleWare"/> in case <see cref="Use"/> has not been called yet.
//        /// </summary>
//        private OwinMiddleware _lastMiddleware;
//        private static readonly Action<Delegate> MiddlewareBaseTypeConversion =
//            d =>
//            {
//                //Log.CurrentLogger.Debug()("Type: ", d.GetType());
//            };

//        public FosAppBuilder(CancellationToken cancelToken, IServerLogger logger)
//        {
//            if (logger == null)
//            {
//                logger = new NullLogger();
//            }

//            _logger = logger;
//            _properties = new Dictionary<string, object>();
//            _rootMiddleware = new FosOwinRoot();

//            //WARN: Non standard Owin header. Used by Nancy
//            OnAppDisposing = cancelToken;
//            _properties.Add("host.OnAppDisposing", cancelToken);

//            _properties.Add("builder.AddSignatureConversion", MiddlewareBaseTypeConversion);
//        }

//        public IAppBuilder Use(object middleware, params object[] args)
//        {
//            var delegateMiddleware = middleware as Delegate;
//            OwinMiddleware newMiddleware;
//            if (delegateMiddleware != null)
//            {
//                newMiddleware = new OwinMiddleware(delegateMiddleware, _logger, args);
//            }
//            else
//            {
//                var typeMiddleware = middleware as Type;

//                if (typeMiddleware != null)
//                    newMiddleware = new OwinMiddleware(typeMiddleware, _logger, args);
//                else
//                    throw new ArgumentException("The middleware to be used needs either to be a Type or a Delegate");
//            }

//            // Update the chain of middleware
//            if (_lastMiddleware == null)
//                _rootMiddleware.Next = newMiddleware;
//            else
//                _lastMiddleware.Next = newMiddleware;

//            _lastMiddleware = newMiddleware;

//            return this;
//        }

//        public object Build (Type returnType)
//        {
//            if (returnType == typeof(Func<IDictionary<string, object>, Task>))
//            {
//                return (Func<IDictionary<string, object>, Task>)_rootMiddleware.Invoke;
//            }

//            if (returnType == typeof(Func<IOwinContext, Task>))
//            {
//                return (Func<IOwinContext, Task>)_rootMiddleware.Invoke;
//            }

//            throw new NotSupportedException("Only Func<IDictionary<string, object>, Task> is supported right now");
//        }

//        public IAppBuilder New()
//        {
//            return new FosAppBuilder(OnAppDisposing, _logger);
//        }

//        public IDictionary<string, object> Properties {
//            get
//            {
//                return _properties;
//            }
//        }
//    }
//}
