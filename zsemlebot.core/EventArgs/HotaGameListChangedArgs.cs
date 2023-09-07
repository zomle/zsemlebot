namespace zsemlebot.core.EventArgs
{
    public class HotaGameListChangedArgs
    {
        public int GamesNotFull { get; }
        public int GamesNotStarted { get; }
        public int GamesInProgress { get; }

        public HotaGameListChangedArgs(int gamesNotFull, int gamesNotStarted, int gamesInProgress)
        {
            GamesNotFull = gamesNotFull;
            GamesNotStarted = gamesNotStarted;
            GamesInProgress = gamesInProgress;
        }
    }
}
