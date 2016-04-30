namespace Fos.Middleware
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;

	/// <summary>
	/// 
	/// </summary>
	public interface IOwinParameterConverter
	{
		bool TryConvert<TOwinParameter>(Func<IDictionary<string, object>, Task> handler, TOwinParameter owinParameters, out Func<TOwinParameter, Task> wrappedHandler);
	}
}