using System;
using System.Collections.Generic;
using System.Reflection;
using System.Data.SQLite;
using System.Linq;

namespace Cobalt.Database
{
    public class SqliteDatabase
    {
        private static bool Initialized { get; set; } = false;
        private const string ConnectionString = "sqlite://Database.sqlite"; 

        private static Dictionary<string, List<PropertyInfo>> IndexedFieldCache { get; set; } = new Dictionary<string, List<PropertyInfo>>();

        public static SQLiteConnection Connect() {
            var Connection = new SQLiteConnection(ConnectionString);
            if(!Initialized)
                Initialize(Connection);
            return Connection;
        }

        public static void Initialize(SQLiteConnection Connection) {
            var CreateTable = new SQLiteCommand(Connection){
                CommandText =
                    @"CREATE TABLE IF NOT EXISTS Post(
                        Title TEXT,
                        Time DATETIME,
                        Body TEXT
                    );"
            };
            CreateTable.ExecuteNonQuery();
            Initialized = true;
        }

        public static SQLiteDataReader Get(Object O, int? Limit = null, int? Offset = null) {
            var Connection = Connect();
            var Indexers = GetIndexedFields(O);
            var QueryText = $"SELECT * FROM {O.GetType().Name} WHERE ";
            QueryText += String.Join(
                " AND ",
                Indexers.Select(Indexer => $"${Indexer.Key} = @{Indexer.Key}").ToList()
            );
            if(Limit != null)
                QueryText += $" LIMIT {Limit.Value} ";
            if(Offset != null)
                QueryText += $" OFFSET {Offset.Value} ";

            var QueryCommand = new SQLiteCommand(Connection){
                CommandText = QueryText
            };
            foreach(var Indexer in Indexers) {
                QueryCommand.Parameters.AddWithValue(Indexer.Key, Indexer.Value);
            }
            return QueryCommand.ExecuteReader();
        }

        public static Dictionary<string, object> GetIndexedFields (object O) {
            if(!IndexedFieldCache.TryGetValue(O.GetType().AssemblyQualifiedName, out var Properties)) {
                Properties = O.GetType().GetProperties().Where(
                    P => P.GetCustomAttribute(typeof(IndexedAttribute)) != null
                ).ToList();
                IndexedFieldCache[O.GetType().AssemblyQualifiedName] = Properties;
            }
            var IndexedFields = new Dictionary<string, object>();
            foreach(var Property in Properties) {
                var Value = Property.GetGetMethod().Invoke(O, null);
                if(Value != null) {
                    IndexedFields[Property.Name] = Value;
                }
            }
            return IndexedFields;
        }
    }
}