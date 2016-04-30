//namespace Fos.Middleware
//{
//	using System;
//	using System.Collections.Generic;
//	using System.Linq;
//	using System.Reflection;
//	using System.Threading.Tasks;
//	using Logging;
//	using Microsoft.Owin;
//	using SaveTrees.Logging;

//	internal class OwinMiddleware
//	{
//		public OwinMiddleware(Type middlewareType, IServerLogger logger, params object[] args)
//			: this(logger, args)
//		{
//			if (logger == null)
//			{
//				throw new ArgumentNullException("logger");
//			}

//			_middlewareType = middlewareType;
//			Log.CurrentLogger.Debug()("_middlewareType: " + _middlewareType);
//		}

//		public OwinMiddleware(Delegate handler, IServerLogger logger, params object[] args)
//			: this(logger, args)
//		{
//			if (logger == null)
//			{
//				throw new ArgumentNullException("logger");
//			}

//			_untypedHandler = handler;
//			Log.CurrentLogger.Debug()("_untypedHandler: " + _untypedHandler);
//		}

//		private OwinMiddleware(IServerLogger logger, params object[] args)
//		{
//			if (logger == null)
//			{
//				throw new ArgumentNullException("logger");
//			}

//			_logger = logger;
//			_args = args ?? new object[0];
//		}

//		private IServerLogger _logger;

//		private static readonly List<Type> ValidInvokeParameterTypes = new List<Type>
//																		{
//																			typeof (IDictionary<string, object>),
//																			typeof (IOwinContext)
//																		};
//		/// <summary>
//		/// The type of this middleware. An instance of this type will be created and Invoke() will be called on it if this middleware
//		/// is not a simple Delegate type.
//		/// </summary>
//		private readonly Type _middlewareType;

//		/// <summary>
//		/// If this middleware is a simple Delegate type, then this field contains it.
//		/// </summary>
//		private readonly Delegate _untypedHandler;

//		/// <summary>
//		/// The args to be passed to this middleware, if any.
//		/// </summary>
//		private readonly object[] _args;

//		/// <summary>
//		/// Set this to define the next middleware to be called after the invocation of this middleware. If there is no next middleware, this is null.
//		/// </summary>
//		public OwinMiddleware Next;

//		#region Dictionary argument

//		private Func<IDictionary<string, object>, Task> _dictionaryBuiltHandler;
//		private Func<IDictionary<string, object>, Task> DictionaryHandler
//		{
//			get
//			{
//				return _dictionaryBuiltHandler ?? (_dictionaryBuiltHandler = BuildDictionaryHandler());
//			}
//		}

//		private Func<IDictionary<string, object>, Task> BuildDictionaryHandler()
//		{
//			Func<IDictionary<string, object>, Task> handler;
//			// Is it a delegate or a type middleware?
//			if (_middlewareType == null)
//			{
//				if (TryMatchUntypedMiddleware(out handler))
//				{
//					return handler;
//				}

//				throw new NotSupportedException("Delegate type " + _untypedHandler + " is not supported");
//			}

//			// Instantiate the constructor with most arguments, limited by the amount of arguments we have available
//			Log.CurrentLogger.Debug()("isDictionaryArgument");
//			var middleware = GetMiddlewareInstance();

//			// Now call the Invoke method, which has to be Invoke(IDictionary<string, object>...)
//			handler = (Func<IDictionary<string, object>, Task>) Delegate.CreateDelegate(typeof (Func<IDictionary<string, object>, Task>), middleware, "Invoke");
//			return handler;
//		}

//		public Task Invoke(IDictionary<string, object> owinParameters)
//		{
//			return DictionaryHandler(owinParameters);
//		}

//		#endregion

//		#region OwinContext argument.

//		private Func<IOwinContext, Task> _owinContextBuiltHandler;
//		private Func<IOwinContext, Task> OwinContextHandler
//		{
//			get
//			{
//				return _owinContextBuiltHandler ?? (_owinContextBuiltHandler = BuildOwinContextHandler());
//			}
//		}

//		private Func<IOwinContext, Task> BuildOwinContextHandler()
//		{
//			// Is it a delegate or a type middleware?
//			if (_middlewareType == null)
//			{
//				Func<IOwinContext, Task> handler;
//				if (TryMatchUntypedMiddleware(out handler))
//				{
//					return handler;
//				}

//				Func<IDictionary<string, object>, Task> dictionaryHandler;
//				if (TryMatchUntypedMiddleware(out dictionaryHandler))
//				{
//					Func<IOwinContext, Task> wrapper = context =>
//													   {
//														   return dictionaryHandler(context.Environment);
//													   };
//					return wrapper;
//				}

//				throw new NotSupportedException("Delegate type " + _untypedHandler + " is not supported");
//			}

//			// Instantiate the constructor with most arguments, limited by the amount of arguments we have available
//			Log.CurrentLogger.Debug()("isOwinContextArgument");
//			var middleware = GetMiddlewareInstance();

//			// Now call the Invoke method, which has to be Invoke(IOwinContext...)
//			var d = Delegate.CreateDelegate(typeof (Func<IOwinContext, Task>), middleware, "Invoke");
//			return (Func<IOwinContext, Task>) d;
//		}

//		public Task Invoke(IOwinContext owinContext)
//		{
//			return OwinContextHandler(owinContext);
//		}

//		#endregion

//		private object GetMiddlewareInstance(OwinMiddleware owinMiddleware)
//		{
//			Log.CurrentLogger.Debug()("Middleware: ", _middlewareType);

//			GetInvokeMethodsParameters();

//			try
//			{
//				// filtered by matching all with Args type(except null Args)
//				var ctor = _middlewareType
//					.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).ToList()
//					.Where(c => c.GetParameters().Length > 0 && c.GetParameters().Length - 1 <= _args.Length).ToList()
//					.OrderByDescending(c => c.GetParameters().Length).ToList()
//					.First(ct => ct.GetParameters()
//								   .Skip(1)
//								   .Zip(_args, (parameterInfo, arg) =>
//											   {
//												   //_logger.Debug("Null Arg: {0}", arg == null);
//												   //_logger.Debug("Arg type: {0}, pinfo type: {1}", arg?.GetType(), pinfo.ParameterType);
//												   //_logger.Debug("Arg type: {0}, pinfo type: {1}", pinfo.ParameterType.IsInstanceOfType(arg));

//												   var result = arg == null || parameterInfo.ParameterType.IsInstanceOfType(arg);
//												   //_logger.Debug("Result: {0}", result);

//												   return result;
//											   })
//								   .All(b => b));

//				var ctorArgs = new object[ctor.GetParameters().Length];
//				ctorArgs[0] = GetNextInvokeHandler();
//				Array.Copy(_args, 0, ctorArgs, 1, ctorArgs.Length - 1);

//				var middleware = ctor.Invoke(ctorArgs);
//				Log.CurrentLogger.Debug()("middlewareInstance type: " + middleware.GetType());

//				return middleware;
//			}
//			catch (Exception exception)
//			{
//				throw new Exception("Couldn't find a constructor that takes at least one argument in type " + _middlewareType, exception);
//			}
//		}

//		private void GetInvokeMethodParameterType()
//		{
//			var invokeMethod = _middlewareType
//				.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
//				.FirstOrDefault(
//					m =>
//					{
//						var parameters = m.GetParameters().ToList();
//						foreach (var parameterInfo in parameters)
//						{
//							Log.CurrentLogger.Debug()("parameterInfo: {ParameterType}, {Name}", parameterInfo.ParameterType, parameterInfo.Name);
//						}

//						var parameterTypeMatches = parameters
//							.Any(parameterInfo => ValidInvokeParameterTypes.Any(parameterType =>
//							{
//								//Log.CurrentLogger.Debug()("parameterInfo.ParameterType: {ParameterType}", parameterInfo.ParameterType);
//								//Log.CurrentLogger.Debug()("parameterType: {parameterType}", parameterType);
//								//Log.CurrentLogger.Debug()("Equal: {0}", parameterInfo.ParameterType == parameterType);
//								//Log.CurrentLogger.Debug()("pipt.IsInstanceOfType(pt): {0}", parameterInfo.ParameterType.IsInstanceOfType(parameterType));
//								//Log.CurrentLogger.Debug()("pt.IsInstanceOfType(pipt): {0}", parameterType.IsInstanceOfType(parameterInfo.ParameterType));
//								var typesEqual = parameterInfo.ParameterType == parameterType;
//								return typesEqual;
//							}));

//						var matches = m.Name == "Invoke" && m.ReturnType == typeof(Task) && parameterTypeMatches;

//						Log.CurrentLogger.Debug()("matches: {matches}", matches);
//						return matches;
//					});

//			if (invokeMethod == null)
//			{
//				throw new Exception("The next middleware does not have an appropriate Invoke method");
//			}
//		}

//		private IEnumerable<ParameterInfo[]> GetInvokeMethodsParameters()
//		{
//			return _middlewareType
//				.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
//				.Where(
//					m =>
//					{
//						var matches = m.Name == "Invoke" && m.ReturnType == typeof (Task);
//						//Log.CurrentLogger.Debug()("matches: {matches}", matches);
//						return matches;
//					})
//				.Select(mi => mi.GetParameters());
//		}

//		private bool TryMatchUntypedMiddleware<T>(out Func<T, Task> handler)
//		{
//			Log.CurrentLogger.Debug()("Handler type: {T} ", typeof(T));

//			var nextInvoke = GetNextInvokeHandler();

//			// NancyFx uses the delegate below
//			var typedHandler1 = _untypedHandler as Func<Func<T, Task>, Func<T, Task>>;

//			if (typedHandler1 != null)
//			{
//				Log.CurrentLogger.Debug()("handler is the NancyFX handler.");

//				handler = owinParameters =>
//				{
//					var func = typedHandler1(nextInvoke);

//					return func(owinParameters);
//				};

//				return true;
//			}

//			// Simple.Web uses this type of delegate
//			var typedHandler2 = _untypedHandler as Func<T, Func<T, Task>, Task>;
//			if (typedHandler2 != null)
//			{
//				Log.CurrentLogger.Debug()("handler is the SimpleWeb handler.");

//				handler = owinParameters =>
//				{
//					var returnTask = typedHandler2(owinParameters, nextInvoke);

//					return returnTask;
//				};

//				return true;
//			}

//			// General untyped anonymous delegate from Owin.AppBuilderUseExtensions.Use
//			var typedAnonymousDelegate = _untypedHandler as Func<T, Func<Task>, Task>;

//			if (typedAnonymousDelegate != null)
//			{
//				Log.CurrentLogger.Debug()("handler is the anonymous Owin extension handler.");

//				handler = owinParameters =>
//				{
//					var func = typedAnonymousDelegate(owinParameters, nextInvoke);

//					return func(owinParameters);
//				};

//				return true;
//			}

//			Log.CurrentLogger.Debug()("No delegate match for handler: {_untypedHandler} ", _untypedHandler);

//			handler = null;
//			return false;
//		}

//		private dynamic GetHandler<T>(OwinMiddleware owinMiddleware)
//		{
//			//Func<IDictionary<string, object>, Task>
//			Func<T, Task> invokeHandler;
//			// Is it a delegate or a type middleware?
//			if (owinMiddleware._middlewareType == null)
//			{
//				if (TryMatchUntypedMiddleware(out invokeHandler))
//				{
//					return invokeHandler;
//				}

//				throw new NotSupportedException("Delegate type " + _untypedHandler + " is not supported");
//			}

//			// Instantiate the constructor with most arguments, limited by the amount of arguments we have available
//			Log.CurrentLogger.Debug()("isDictionaryArgument");
//			var middlewareInstance = GetMiddlewareInstance(owinMiddleware);

//			// Now call the Invoke method, which has to be Invoke(IDictionary<string, object>...)
//			invokeHandler = (Func<T, Task>)Delegate.CreateDelegate(typeof(Func<T, Task>), middlewareInstance, "Invoke");
//			return invokeHandler;
//		}

//		private IEnumerable<object> GetNextInvokeHandlers()
//		{
//			var handlers = new List<object>();
//			if (Next == null)
//			{
//				Log.CurrentLogger.Debug()("No handlers on the next invoke, as there is no next middleware in the pipline.");
//			}
//			else
//			{
//				var invokeMethodsParameters = Next.GetInvokeMethodsParameters();
//				foreach (var invokeMethodParameters in invokeMethodsParameters)
//				{
//					if (invokeMethodParameters.Any(p => p.ParameterType == typeof (IDictionary<string, object>)))
//					{
//						handlers.Add(GetHandler<IDictionary<string, object>>(Next));
//						nextInvokeHandler = Next.DictionaryHandler;
//						Log.CurrentLogger.Debug()("nextInvoke is a dictionary parameter.");
//					}

//					if (invokeMethodParameters.Any(p => p.ParameterType == typeof(IOwinContext)))
//					{
//						nextInvokeHandler = Next.OwinContextHandler;
//						Log.CurrentLogger.Debug()("nextInvoke is an owinContext parameter.");
//					}
//					else
//					{
//						var invokeMethodParameterList = invokeMethodParameters.Aggregate("", (current, next) => current + ", ");
//						invokeMethodParameterList = invokeMethodParameterList.Remove(invokeMethodParameterList.LastIndexOf(", ", StringComparison.Ordinal));
//						throw new NotSupportedException("None of the following types (" + invokeMethodParameterList + ") are supported as an argument when calling the next middleware Invoke method");
//					}
//				}
//			}

//			return handlers;
//		}
//	}
//}

//var parameters = m.GetParameters().ToList();
//						foreach (var parameterInfo in parameters)
//						{
//							Log.CurrentLogger.Debug()("parameterInfo: {ParameterType}, {Name}", parameterInfo.ParameterType, parameterInfo.Name);
//						}

//						var parameterTypeMatches = parameters
//							.Any(parameterInfo => ValidInvokeParameterTypes.Any(parameterType =>
//							{
//								//Log.CurrentLogger.Debug()("parameterInfo.ParameterType: {ParameterType}", parameterInfo.ParameterType);
//								//Log.CurrentLogger.Debug()("parameterType: {parameterType}", parameterType);
//								//Log.CurrentLogger.Debug()("Equal: {0}", parameterInfo.ParameterType == parameterType);
//								//Log.CurrentLogger.Debug()("pipt.IsInstanceOfType(pt): {0}", parameterInfo.ParameterType.IsInstanceOfType(parameterType));
//								//Log.CurrentLogger.Debug()("pt.IsInstanceOfType(pipt): {0}", parameterType.IsInstanceOfType(parameterInfo.ParameterType));
//								var ii = parameterInfo.ParameterType == parameterType;
//								return ii;
//							}));
//			if (invokeMethod == null)
//			{
//				throw new Exception("The next middleware does not have an appropriate Invoke method");
//			}