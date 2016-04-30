namespace Fos.Middleware
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.Contracts;
	using System.Reflection;
	using System.Threading.Tasks;
	using Logging;
	using Microsoft.Owin;
	using SaveTrees.Logging;

	/// <summary>
	/// 
	/// </summary>
	public abstract class MiddlewareBuilder : IMiddlewareBuilder
	{
		protected IServerLogger _logger;
		protected IOwinParameterConverter _owinParameterConverter;

		/// <summary>
		/// The args to be passed to this middleware, if any.
		/// </summary>
		protected object[] _args;

		protected MiddlewareBuilder(IServerLogger logger, IOwinParameterConverter owinParameterConverter, object[] args)
		{
			Contract.Requires(logger != null);

			Contract.Ensures(_logger != null);
			Contract.Ensures(_args != null);


			_logger = logger;
			_owinParameterConverter = owinParameterConverter ?? new DefaultOwinParameterConverter();
			_args = args ?? new object[0];
		}

		private Func<IDictionary<string, object>, Task> _invokeHandler;

		protected abstract object Instance { get; }

		/// <summary>
		/// All the invoke methods on the middleware.
		/// </summary>
		public Func<IDictionary<string, object>, Task> InvokeHandler { get { return _invokeHandler ?? (_invokeHandler = GetInvokeHandler()); } }

		/// <summary>
		/// Set this to define the next middleware to be called after the invocation of this middleware. If there is no next middleware, this is null.
		/// </summary>
		public IMiddlewareBuilder Next { get; set; }

		public virtual Task Invoke(IDictionary<string, object> owinParameters)
		{
			// Try known conversions.
			// Todo: consider priority?
			Func<IDictionary<string, object>, Task> handler;
			if (_owinParameterConverter.TryConvert(InvokeHandler, owinParameters, out handler))
			{
				return handler(owinParameters);
			}

			throw new NotSupportedException("The parameter type " + owinParameters.GetType() + " is not supported.  No converter is registered, which is necessary here when calling Invoke on a middleware instance.");
		}

		protected abstract Func<IDictionary<string, object>, Task> GetInvokeHandler();

		protected Func<IDictionary<string, object>, Task> GetCommonInvokeHandler(IEnumerable<MethodInfo> invokeMethods)
		{
			foreach (var invokeMethod in invokeMethods)
			{
				foreach (var parameter in invokeMethod.GetParameters())
				{
					if (parameter.ParameterType == typeof (IDictionary<string, object>))
					{
						Func<IDictionary<string, object>, Task> handler =
							environment =>
							{
								return (Task) invokeMethod.Invoke(Instance, new[] {environment});
							};
						//Log.CurrentLogger.Debug()("Added dictionary handler to current type");
						return handler;
					}

					if (parameter.ParameterType == typeof (IOwinContext))
					{
						Func<IDictionary<string, object>, Task> wrappedHandler =
							owinContext =>
							{
								var msOwinContext = new OwinContext(owinContext);
								return (Task) invokeMethod.Invoke(Instance, new[] {msOwinContext});
							};
						//Log.CurrentLogger.Debug()("Added owinContext handler to current type.");
						return wrappedHandler;
					}
				}
			}

			return null;
		}
	}
}