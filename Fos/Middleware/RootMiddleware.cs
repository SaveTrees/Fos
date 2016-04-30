namespace Fos.Middleware
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;

	/// <summary>
	/// The entry-point for the FOS middleware.
	/// </summary>
	internal class RootMiddleware : IMiddlewareBuilder
	{
		/// <summary>
		/// The next middleware to be called after the invocation of this middleware. If there is no next middleware, this is null.
		/// </summary>
		public IMiddlewareBuilder Next { get; set; }

		public Task Invoke(IDictionary<string, object> owinParameters)
		{
			if (Next == null)
			{
				throw new Exception("No middleware added to your application.");
			}

			// Do nothing yet, just pass control to next middleware
			return Next.Invoke(owinParameters);
		}

		public object Instance { get { return new RootMiddleware(); } }

		public Func<IDictionary<string, object>, Task> InvokeHandler { get { return null; } }
	}
}
