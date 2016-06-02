using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Owin;

namespace Fos.Owin
{
	using Logging;
	using Middleware;

	internal class FosAppBuilder : IAppBuilder
	{
		private readonly IServerLogger _logger;
		private readonly Dictionary<string, object> _properties;

		public CancellationToken OnAppDisposing { get; }

		private readonly IMiddlewareBuilder _rootMiddleware;

		/// <summary>
		/// This is the last middleware added through <see cref="Use"/>, or the <see cref="_rootMiddleWare"/> in case <see cref="Use"/> has not been called yet.
		/// </summary>
		private IMiddlewareBuilder _lastMiddleware;

		private static readonly Action<Delegate> MiddlewareBaseTypeConversion =
			d =>
			{
				//Log.CurrentLogger.Debug()("Type: ", d.GetType());
			};

		public FosAppBuilder(CancellationToken cancelToken, IServerLogger logger)
		{
			if (logger == null)
			{
				logger = new NullLogger();
			}

			_logger = logger;
			_properties = new Dictionary<string, object>();
			_rootMiddleware = new RootMiddleware();

			//WARN: Non standard Owin header. Used by Nancy
			OnAppDisposing = cancelToken;
			_properties.Add("host.OnAppDisposing", cancelToken);
			// Allows automatic conversion between the MS OWIN base middleware signature and the standard app func signature.
			_properties.Add("builder.AddSignatureConversion", MiddlewareBaseTypeConversion);
		}

		/// <summary>
		/// FOS intercepts existing 'Use' calls and wraps them in the <see cref="FosAppBuilder"/>.
		/// </summary>
		/// <param name="middleware"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public IAppBuilder Use(object middleware, params object[] args)
		{
			var delegateMiddleware = middleware as Delegate;
			IMiddlewareBuilder newMiddleware;
			if (delegateMiddleware == null)
			{
				var typeMiddleware = middleware as Type;

				if (typeMiddleware == null)
				{
					throw new ArgumentException("The middleware to be used needs either to be a Type or a Delegate");
				}

				newMiddleware = new TypedMiddlewareBuilder(typeMiddleware, _logger, null, args);
			}
			else
			{
				newMiddleware = new UntypedMiddlewareBuilder(delegateMiddleware, _logger, null, args);
			}

			// Update the chain of middleware
			if (_lastMiddleware == null)
			{
				_rootMiddleware.Next = newMiddleware;
			}
			else
			{
				_lastMiddleware.Next = newMiddleware;
			}

			_lastMiddleware = newMiddleware;

			return this;
		}

		public object Build(Type returnType)
		{
			if (returnType == typeof (Func<IDictionary<string, object>, Task>))
			{
				return (Func<IDictionary<string, object>, Task>)_rootMiddleware.Invoke;
			}

			//if (returnType == typeof (Func<IOwinContext, Task>))
			//{
			//	return (Func<IOwinContext, Task>) _rootMiddleware.Invoke;
			//}

			throw new NotSupportedException("Only Func<IDictionary<string, object>, Task> and Func<IOwinContext, Task> are currently supported.");
		}

		public IAppBuilder New()
		{
			return new FosAppBuilder(OnAppDisposing, _logger);
		}

		public IDictionary<string, object> Properties { get { return _properties; } }
	}
}