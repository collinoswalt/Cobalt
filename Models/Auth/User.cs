using Cobalt.Models.Editor;
using Cobalt.Database;

namespace Cobalt.Models.Auth
{
    public class User : DatabaseObject
    {
        [Indexed]
        public string Username { get; set; }
        
        [Ignored]
        [Hidden]
        public string Password { get; set; }

        [SQLite]
        private string Hash { get; set; }

        public Role UserRoles { get; set; }

        public User() {

        }

        public User(string Username, string Password) {
            this.Username = Username;
            this.Hash = BCrypt.Net.BCrypt.HashPassword(Password);
        }

        public bool IsUser(string Attempt) {
            return BCrypt.Net.BCrypt.Verify(Attempt, Hash);
        }
    }

    public enum Role {
        [DisplayName("No Permission")]
        NoPermission,
        Developer,
        Administrator,
        Editor
    }
}
