namespace Fos.Middleware
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.Contracts;
	using System.Linq;
	using System.Reflection;
	using System.Threading.Tasks;
	using Logging;
	using SaveTrees.Logging;

	/// <summary>
	/// 
	/// </summary>
	internal class TypedMiddlewareBuilder : MiddlewareBuilder
	{
		public TypedMiddlewareBuilder(Type middlewareType, IServerLogger logger, IOwinParameterConverter owinParameterConverter = null, params object[] args)
			: base(logger, owinParameterConverter, args)
		{
			Contract.Requires(logger != null);

			_middlewareType = middlewareType;
			//Log.CurrentLogger.Debug()("_middlewareType: " + _middlewareType);

			if (InvokeHandler == null)
			{
				throw new NotSupportedException("The middleware type (" + _middlewareType + ") is missing an appropriate 'Invoke' method.");
			}
		}

		/// <summary>
		/// The type of this middleware. An instance of this type will be created and Invoke() will be called on it if this middleware
		/// is not a simple Delegate type.
		/// </summary>
		private readonly Type _middlewareType;

		private object _instance;
		protected override object Instance { get { return _instance ?? (_instance = GetMiddlewareInstance()); } }

		protected override Func<IDictionary<string, object>, Task> GetInvokeHandler()
		{
			var handler = GetCommonInvokeHandler(GetInvokeMethods());
			return handler;
		}

		private object GetMiddlewareInstance()
		{
			//Log.CurrentLogger.Debug()("Middleware: ", _middlewareType);

			try
			{
				// filtered by matching all with Args type(except null Args)
				var ctor = _middlewareType
					.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).ToList()
					.Where(c => c.GetParameters().Length > 0 && c.GetParameters().Length - 1 <= _args.Length).ToList()
					.OrderByDescending(c => c.GetParameters().Length).ToList()
					.First(ct => ct.GetParameters()
								   .Skip(1)
								   .Zip(_args, (parameterInfo, arg) => arg == null || parameterInfo.ParameterType.IsInstanceOfType(arg))
								   .All(b => b));

				var ctorArgs = new object[ctor.GetParameters().Length];
				object middlewareInstance = Next == null
					? null
					: Next.InvokeHandler;

				ctorArgs[0] = middlewareInstance;
				Array.Copy(_args, 0, ctorArgs, 1, ctorArgs.Length - 1);

				var middleware = ctor.Invoke(ctorArgs);
				//Log.CurrentLogger.Debug()("middlewareInstance type: " + middleware.GetType());

				return middleware;
			}
			catch (Exception exception)
			{
				var message = "Couldn't construct the middleware type " + _middlewareType;
				if (exception.InnerException == null)
				{
					message += ".  Check that the type has a constructor matching the requested parameters.";
				}
				else
				{
					message += ".  Check the inner exception for nested errors.";
				}
				throw new Exception(message, exception);
			}
		}

		/// <summary>
		/// Returns an enumerable of parameters for all methods on the type which match the expected OWIN invoke convention
		/// </summary>
		/// <remarks>
		/// The method must be called 'Invoke' and have a return type of <see cref="Task"/>.
		/// </remarks>
		/// <returns></returns>
		private IEnumerable<MethodInfo> GetInvokeMethods()
		{
			return _middlewareType
				.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
				.Where(
					m =>
					{
						var matches = m.Name == "Invoke" && m.ReturnType == typeof (Task);
						//Log.CurrentLogger.Debug()("matches: {matches}", matches);
						return matches;
					});
		}
	}
}