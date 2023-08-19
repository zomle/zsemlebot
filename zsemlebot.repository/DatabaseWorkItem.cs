namespace zsemlebot.repository
{
    internal class DatabaseWorkItem
    {
        public string Query { get; }
        public object? Parameters { get; }

        public DatabaseWorkItem(string query, object? parameters)
        {
            Query = query;
            Parameters = parameters;
        }
    }
}
