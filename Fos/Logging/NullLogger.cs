namespace Fos.Logging
{
	using System;
	using System.Net.Sockets;

	internal class NullLogger : IServerLogger
	{
		/// <summary>
		/// Does not need to be thread safe.
		/// </summary>
		public void ServerStart()
		{

		}

		/// <summary>
		/// Does not need to be thread safe.
		/// </summary>
		public void ServerStop()
		{
		}

		public void LogConnectionReceived(Socket createdSocket)
		{
		}

		/// <summary>
		/// Some times the connection is closed abruptly. For those cases, this method is called to log this occurrence.
		/// Be aware that sometimes <paramref name="req"/>'s members can be null or in invalid state, depending on the amount of data the server received before 
		/// the connection was closed by the other side.
		/// </summary>
		/// <param name="s">The socket that was closed abruptly.</param>
		/// <param name="req">The request info we had obtained so far. You should null check this object's members. The object itself will never be null.</param>
		public void LogConnectionClosedAbruptly(Socket s, RequestInfo req)
		{
		}

		public void LogConnectionEndedNormally(Socket s, RequestInfo req)
		{
		}

		public void LogApplicationError(Exception e, RequestInfo req)
		{
		}

		public void LogServerError(Exception e, string format, params object[] prms)
		{
		}

		public void LogSocketError(Socket s, Exception e, string format, params object[] prms)
		{
		}

		public void Debug(string messageTemplate, params object[] prms)
		{
		}
	}
}