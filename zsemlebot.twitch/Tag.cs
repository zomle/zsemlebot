using System.Diagnostics;

namespace zsemlebot.twitch
{
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public class Tag
    {
        public string Key { get; set; }
        public string Value { get; set; }

        private string GetDebuggerDisplay()
        {
            return $"key: {Key}, value: {Value}";
        }
    }
}
