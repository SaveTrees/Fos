namespace Fos.Middleware
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;

	public interface IMiddlewareBuilder
	{
		/// <summary>
		/// Set this to define the next middleware to be called after the invocation of this middleware. If there is no next middleware, this is null.
		/// </summary>
		IMiddlewareBuilder Next { get; set; }

		Task Invoke(IDictionary<string, object> owinParameters);
		
		Func<IDictionary<string, object>, Task> InvokeHandler { get; }
	}
}