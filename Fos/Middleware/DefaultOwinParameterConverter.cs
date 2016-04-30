namespace Fos.Middleware
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Microsoft.Owin;

	/// <summary>
	/// 
	/// </summary>
	public class DefaultOwinParameterConverter : IOwinParameterConverter
	{
		/// <summary>
		/// 
		/// </summary>
		/// /// <typeparam name="TOwinParameter"></typeparam>
		/// <param name="handler"></param>
		/// <param name="owinParameters"></param>
		/// <param name="wrappedHandler"></param>
		/// <returns></returns>
		public bool TryConvert<TOwinParameter>(Func<IDictionary<string, object>, Task> handler, TOwinParameter owinParameters, out Func<TOwinParameter, Task> wrappedHandler)
		{
			var dictionaryParameters = owinParameters as IDictionary<string, object>;
			if (dictionaryParameters == null)
			{
				if (typeof (TOwinParameter) == typeof (IOwinContext))
				{
					wrappedHandler =
						owinParameter =>
						{
							var owinContextParameter = (IOwinContext) owinParameter;
							return handler(owinContextParameter.Environment);
						};

					return true;
				}

				wrappedHandler = null;
				return false;
			}

			wrappedHandler = owinParameter => handler(owinParameter as IDictionary<string, object>);
			return true;
		}
	}
}