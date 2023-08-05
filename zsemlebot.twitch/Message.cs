using System;
using System.Collections.Generic;
using System.Linq;

namespace zsemlebot.twitch
{
    public class Message
    {
        public IReadOnlyList<Tag> Tags { get; }
        public string Source { get; set; }
        public string Command { get; set; }
        public string Params { get; set; }

        public Message(IEnumerable<Tag> tags)
        {
            Tags = tags == null ? Array.Empty<Tag>() : tags.ToList();
        }
    }
}
