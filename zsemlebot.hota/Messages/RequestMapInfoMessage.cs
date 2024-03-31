using System.Collections.Generic;
using zsemlebot.hota.Extensions;

namespace zsemlebot.hota.Messages
{
	public class RequestMapInfoMessage : HotaMessageBase
	{
		public IReadOnlyList<uint> MapIds { get; }

		public RequestMapInfoMessage(IReadOnlyList<uint> targetMapIds)
			: base(MessageType.RequestMapInfo, (short)(0x8 + (0x4 * targetMapIds.Count)))
		{
			MapIds = new List<uint>(targetMapIds);
		}

		public override DataPackage AsDataPackage()
		{
			var buffer = CreateMessageBuffer();
			buffer.WriteInt(4, (uint)MapIds.Count);
			for (int i = 0; i < MapIds.Count; ++i)
			{
				buffer.WriteInt(8 + i * 4, MapIds[i]);
			}
			return new DataPackage(buffer);
		}
	}
}
