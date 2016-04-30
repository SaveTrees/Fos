//namespace Fos.Middleware
//{
//	using System;
//	using System.Collections.Generic;
//	using System.Threading.Tasks;
//	using Microsoft.Owin;

//	internal class FosOwinRoot
//	{
//		public OwinMiddleware Next;

//		public Task Invoke(IDictionary<string, object> owinParameters)
//		{
//			// Do nothing yet, just pass control to next middleware

//			if (Next != null)
//				return Next.Invoke(owinParameters);

//			throw new Exception("No middleware added to your app");
//		}

//		public Task Invoke(IOwinContext owinContext)
//		{
//			// Do nothing yet, just pass control to next middleware

//			if (Next != null)
//				return Next.Invoke(owinContext);

//			throw new Exception("No middleware added to your app");
//		}

//	}
//}
