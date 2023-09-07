namespace zsemlebot.core.Domain
{
    public class FakeHotaGame : HotaGame
    {
        public override bool IsRealGame { get { return false; } }

        public FakeHotaGame(GameKey gameKey)
            : base(gameKey, new FakeHotaUser(gameKey.HostUserId), $"game not found ({gameKey.HostUserId}; {gameKey.GameId})")
        {
        }
    }
}