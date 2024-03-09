using System;

namespace zsemlebot.hota.Events
{
	public class IncomingMessage : HotaEvent
	{
		public uint SourceUserId { get; }
		public string Message { get; }

		public IncomingMessage(uint sourceUserId, string message)
		{
			SourceUserId = sourceUserId;
			Message = message;
		}
	}
}
