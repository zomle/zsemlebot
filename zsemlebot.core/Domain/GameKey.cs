﻿namespace zsemlebot.core.Domain
{
    public record GameKey
    {
        public uint HostUserId { get; }
        public int GameId { get; }

        public GameKey(uint hostUserId, int gameId)
        {
            HostUserId = hostUserId;
            GameId = gameId;
        }
    }
}