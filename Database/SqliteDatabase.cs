using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Data.SQLite;
using System.Linq;
using Cobalt.Models;

namespace Cobalt.Database
{
    public class SqliteDatabase
    {
        private static string ConnectionString = $"Data Source={Directory.GetCurrentDirectory()}\\mydb.db;Version=3;"; 

        private static Dictionary<string, List<PropertyInfo>> IndexedFieldCache { get; set; } = new Dictionary<string, List<PropertyInfo>>();
        private static Dictionary<string, Tuple<string, List<Tuple<string, PropertyInfo>>>> InsertedFieldCache { get; set; }
            = new Dictionary<string, Tuple<string, List<Tuple<string, PropertyInfo>>>>();

        public static SQLiteConnection Connect() {
            var Connection = new SQLiteConnection(ConnectionString);
            Connection.Open();
            return Connection;
        }

        public static void Initialize() {
            using(var Connection = Connect()) {
                var Types = typeof(DatabaseObject).Assembly.GetTypes().Where(T => T.IsSubclassOf(typeof(DatabaseObject)));
                String InsertQueries = "";
                foreach(var T in Types) {
                    InsertQueries += GetCreateTableQuery(T);
                }
                var CreateTable = new SQLiteCommand(Connection){
                    CommandText = InsertQueries
                };
                CreateTable.ExecuteNonQuery();
            }
        }

        public static List<T> Get<T>(Object O, int? Limit = null, int? Offset = null) where T : DatabaseObject, new(){
            using(var Connection = Connect()){
                var Indexers = GetIndexedFields(O);
                var QueryText = $"SELECT * FROM {typeof(T).Name}";
                if(Indexers.Count() != 0) {
                    QueryText += " WHERE " + String.Join(
                        " AND ",
                        Indexers.Select(Indexer => $"{Indexer.Key} = @{Indexer.Key}").ToList()
                    );
                }
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
                var Reader = QueryCommand.ExecuteReader();
                List<T> Output = new List<T>();
                while(Reader.Read()){
                    Output.Add(DeserializeSQLite<T>(Reader));
                }
                return Output;
            }
        }

        private static T DeserializeSQLite<T>(SQLiteDataReader Reader) where T : DatabaseObject, new() {
            if(!Reader.HasRows)
                return null;

            var Deserialized = new T(); 
            var Properties = GetProperties(Deserialized);
            Dictionary<string, PropertyInfo> PropertyMap = new Dictionary<string, PropertyInfo>();
            foreach(var Property in Properties.Item2) {
                PropertyMap[Property.Item1] = Property.Item2;
            }
            for(int i = 0; i < Reader.FieldCount; i++) {
                var Name = Reader.GetName(i);
                var Property = PropertyMap[Name];
                if(Property.PropertyType.IsEnum){
                    var EnumType = Property.PropertyType;
                    var IntegerValue = Reader.GetValue(i);
                    var EnumName = Enum.GetName(EnumType, IntegerValue);
                    Property.GetSetMethod(true).Invoke(Deserialized, new object[]{
                        Enum.TryParse(Property.PropertyType, EnumName, true, out var EnumValue)
                            ? EnumValue 
                            : null
                    });
                    continue;
                }
                Property.GetSetMethod(true).Invoke(Deserialized, new object[]{Reader.GetValue(i)});
            }
            return Deserialized;
        }

        public static Dictionary<string, object> GetIndexedFields (object O) {
            if(O == null) {
                return new Dictionary<string, object>();
            }
            if(!IndexedFieldCache.TryGetValue(O.GetType().AssemblyQualifiedName, out var Properties)) {
                Properties = O.GetType().GetProperties().Where(
                    P => P.GetCustomAttribute(typeof(IndexedAttribute)) != null
                ).ToList();
                IndexedFieldCache[O.GetType().AssemblyQualifiedName] = Properties;
            }
            var IndexedFields = new Dictionary<string, object>();
            foreach(var Property in Properties) {
                var Value = Property.GetGetMethod(true).Invoke(O, null);
                if(Value != null) {
                    IndexedFields[Property.Name] = Value;
                }
            }
            return IndexedFields;
        }

        public static int Save(object O) {
            using(var Connection = Connect()){
                if(!InsertedFieldCache.TryGetValue(O.GetType().Name, out var Properties)) {
                    Properties = PopulateTypeFieldDictionary(O);
                }
                string CommandText = $"INSERT INTO {O.GetType().Name}({Properties.Item1.Replace("@", "")}) VALUES({Properties.Item1});";
                var Command = new SQLiteCommand(Connection) {
                    CommandText = CommandText
                };
                foreach(var P in Properties.Item2){
                    if(P.Item2.GetCustomAttribute(typeof(AutoIncrementAttribute)) != null)
                        continue;
                    Command.Parameters.AddWithValue(P.Item1, P.Item2.GetGetMethod(true).Invoke(O, null));
                }
                Command.ExecuteNonQuery();
                return LastInsertRowId(Connection);

            }
        }

        public static void Delete(Object O) {
            using(var Connection = Connect()){
                var Properties = GetProperties(O);
                var PrimaryKey = GetPrimaryKey(Properties.Item2);

                var Command = new SQLiteCommand(Connection);
                Command.CommandText = $"DELETE FROM {O.GetType().Name} WHERE {PrimaryKey.Name} = @{PrimaryKey.Name};";
                Command.Parameters.AddWithValue(PrimaryKey.Name, PrimaryKey.GetGetMethod(true).Invoke(O, null));
                Command.ExecuteNonQuery();
            }
        }

        private static Tuple<string, List<Tuple<string, PropertyInfo>>> GetProperties(Object O) {
            if(!InsertedFieldCache.TryGetValue(O.GetType().Name, out var Properties)) {
                Properties = PopulateTypeFieldDictionary(O);
            }
            return Properties;
        }

        private static PropertyInfo GetPrimaryKey(List<Tuple<string, PropertyInfo>> Properties) {
            return Properties
                    .Where(P => P.Item2.GetCustomAttribute(typeof(PrimaryKeyAttribute)) != null)
                    .Select(T => T.Item2)
                    .First();
        }

        public static void Update(Object O) {
            using(var Connection = Connect()){
                var Properties = GetProperties(O);
                var PrimaryKey = GetPrimaryKey(Properties.Item2);
                string CommandText = @$"UPDATE {O.GetType().Name} SET {
                    Properties
                        .Item2
                        .Where(P => P.Item2.GetCustomAttribute(typeof(PrimaryKeyAttribute)) == null)
                        .Select(T => T.Item1 + " = @" + T.Item1)
                        .Aggregate((A, B) => A + ", " + B)
                } WHERE {PrimaryKey.Name} = @{PrimaryKey.Name}";
                var Command = new SQLiteCommand(Connection) {
                    CommandText = CommandText
                };
                foreach(var P in Properties.Item2){
                    if(P.Item2.GetCustomAttribute(typeof(PrimaryKeyAttribute)) != null)
                        continue;
                    Command.Parameters.AddWithValue(P.Item1, P.Item2.GetGetMethod(true).Invoke(O, null));
                }
                Command.Parameters.AddWithValue(PrimaryKey.Name, PrimaryKey.GetGetMethod(true).Invoke(O, null));
                Command.ExecuteNonQuery();
            }
        }

        private static int LastInsertRowId(SQLiteConnection Connection) {
            var Command = new SQLiteCommand(Connection);
            Command.CommandText = "SELECT last_insert_rowid()";

            var Reader = Command.ExecuteReader();
            Reader.Read();
            return Reader.GetInt32(0);
        }

        private static Tuple<string, List<Tuple<string, PropertyInfo>>> PopulateTypeFieldDictionary(object O) {
            List<Tuple<string, PropertyInfo>> Properties
                = new List<Tuple<string, PropertyInfo>>();
            List<string> InsertValues = new List<string>();
            foreach(var P in O.GetType().GetProperties(
                BindingFlags.Public    |
                BindingFlags.NonPublic |
                BindingFlags.Instance
            )) {
                if(P.GetCustomAttribute(typeof(IgnoredAttribute)) != null)
                    continue;
                if(!P.GetGetMethod(true).IsPublic && P.GetCustomAttribute(typeof(SQLiteAttribute)) == null)
                    continue;
                Properties.Add(new Tuple<string, PropertyInfo>(
                    P.Name,
                    P
                ));
                if(P.GetCustomAttribute(typeof(AutoIncrementAttribute)) != null)
                    continue;
                InsertValues.Add("@" + P.Name);
            }
            var InsertText = String.Join(", ", InsertValues);
            var Cache = new Tuple<string, List<Tuple<string, PropertyInfo>>>(InsertText, Properties);
            InsertedFieldCache.Add(
                O.GetType().Name,
                Cache
            );

            return Cache;
        }

        public static String GetCreateTableQuery(Type T) {
            string Insert = $"CREATE TABLE IF NOT EXISTS {T.Name} (";
            var Lines = new List<string>();
            foreach(var P in T.GetProperties(
                BindingFlags.Public    |
                BindingFlags.NonPublic |
                BindingFlags.Instance
            )) {
                if(P.GetCustomAttribute(typeof(IgnoredAttribute)) != null)
                    continue;
                if(!P.GetGetMethod(true).IsPublic && P.GetCustomAttribute(typeof(SQLiteAttribute)) == null)
                    continue;
                string Line = $"{P.Name} {GetSQLiteTypeFromCSharpType(P.PropertyType)} ";

                var PrimaryKey = P.GetCustomAttribute(typeof(PrimaryKeyAttribute)) != null;
                if(PrimaryKey){
                    Line += " PRIMARY KEY";
                    if(PrimaryKey && P.GetCustomAttribute(typeof(AutoIncrementAttribute)) != null)
                        Line += " AUTOINCREMENT";
                }
                Lines.Add(Line);
            }
            return Insert + String.Join(",", Lines) + ");";
        }

        public static string GetSQLiteTypeFromCSharpType(Type T) {
            if(T.IsEnum)
                return "INTEGER";
            string Name = Nullable.GetUnderlyingType(T)?.Name ?? T.Name;
            return Name.ToLower() switch {
                "sbyte"     => "TINYINT",
                "byte"      => "TINYINT UNSIGNED",

                "int16"     => "SMALLINT",
                "short"     => "SMALLINT",
                "uint16"    => "SMALLINT UNSIGNED",
                "ushort"    => "SMALLINT UNSIGNED",

                "int32"     => "INTEGER",
                "int"       => "INTEGER",
                "uint32"    => "INTEGER UNSIGNED",
                "uint"      => "INTEGER UNSIGNED",

                "int64"     => "INTEGER",
                "long"      => "INTEGER",
                "uint64"    => "INTEGER UNSIGNED",
                "ulong"     => "INTEGER UNSIGNED",

                "decimal"   => "REAL",
                "float"     => "FLOAT",
                "double"    => "DOUBLE",
                
                "string"    => "TEXT",
                "char"      => "CHAR",
                "bool"      => "BOOLEAN",

                "date"      => "DATE",
                "datetime"  => "DATETIME",
                _           => "TEXT"
            };
        }
    }
}