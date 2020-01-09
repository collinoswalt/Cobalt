using System;

using Cobalt.Database;

namespace Cobalt.Models.Posts
{
    public abstract class Post
    {
        [Indexed]
        public int Id { get; set; }

        [Indexed]
        public string Title { get; set; }
        public DateTime Time { get; set; }
        public string Body { get; set; }

        public static Post Get(int Id) {

        }
    }
}