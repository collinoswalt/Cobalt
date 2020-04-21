using System.Collections.Generic;
using System.Data.SQLite;

using Cobalt.Database;

namespace Cobalt.Models
{
    public abstract class DatabaseObject
    {
        [Indexed]
        [AutoIncrement]
        [PrimaryKey]
        public long? Id { get; set; }

        public DatabaseObject() {

        }

        public DatabaseObject(SQLiteDataReader Reader) {

        }

        public int Save() {
            return SqliteDatabase.Save(this);
        }

        public void Update() {
            SqliteDatabase.Update(this);
        }

        public void Delete() {
            SqliteDatabase.Delete(this);
        }

        public static List<T> GetStatic<T>(int? Limit = null, int? Offset = null) where T : DatabaseObject, new() {
            return SqliteDatabase.Get<T>(null, Limit, Offset);
        }

        public List<T> Get<T>(int? Limit = null, int? Offset = null) where T : DatabaseObject, new(){
            return SqliteDatabase.Get<T>(this, Limit, Offset);
        }
    }
}