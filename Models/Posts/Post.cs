using System;
using System.Collections.Generic;
using Cobalt.Database;
using Cobalt.Models.Editor;

namespace Cobalt.Models.Posts
{
    public class Post : DatabaseObject
    {
        [Indexed]
        public string Title { get; set; }

        [ReadOnly]
        public DateTime Time { get; set; }
        
        [RichText]
        public string Body { get; set; }

        public static Post Get(int Id) {
            return (new Post(){Id = Id}).Get<Post>()?[0];
        }

        public static List<Post> GetPage(int Page) {
            return DatabaseObject.GetStatic<Post>(10, (Page - 1) * 10);
        }
    }
}
