using System.Diagnostics;

namespace zsemlebot.twitch
{
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public class Tag
    {
        public string Key { get; }
        public string Value { get; }

        public Tag(string key, string value)
        {
            Key = key;
            Value = value;
        }

        private string GetDebuggerDisplay()
        {
            return $"key: {Key}, value: {Value}";
        }
    }
}
