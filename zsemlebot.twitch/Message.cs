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

        public string SourceUserName
        {
            get
            {
                if (Source == null)
                {
                    return null;
                }

                var tokens = Source.Split('!');
                if (tokens.Length == 0)
                {
                    return Source;
                }

                return tokens[0];
            }
        }

        public int SourceUserId
        {
            get
            {
                var userIdTag = Tags.FirstOrDefault(t => t.Key == "user-id");
                if (userIdTag == null)
                {
                    return 0;
                }

                if (!int.TryParse(userIdTag.Value, out var result))
                {
                    return 0;
                }

                return result;
            }
        }


        public Message(IEnumerable<Tag> tags)
        {
            Tags = tags == null ? Array.Empty<Tag>() : tags.ToList();
        }
    }
}
