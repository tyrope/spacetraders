using System.Data;
using System.IO;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STCommander
{
    public class DatabaseManager : MonoBehaviour
    {
        public static DatabaseManager instance;

        private readonly string filePath = Path.Combine(Application.persistentDataPath, "database.sqlite3");
        private readonly IDbConnection sqlConnection = new SqliteConnection();

        private readonly bool sendSqlToLog = true; //TODO Sql Verbosity lives here.

        /// <summary>
        /// Generates an Connection object to the database, and if the database doesn't exist yet, creates it.
        /// </summary>
        /// <returns>A closed connection to the SQL database.</returns>
        private void CreateDatabaseConnection() {
            sqlConnection.ConnectionString = filePath;
            if(!File.Exists(filePath)) {
                SqliteConnection.CreateFile(filePath);
                SqliteCommand sqlCommand = new SqliteCommand(Resources.Load<TextAsset>("stCommanderSchema.sql").text);
                sqlConnection.Open();
                sqlCommand.ExecuteNonQuery();
                sqlCommand.Dispose();
                sqlConnection.Close();
            }
        }

        private void Start() {
            if(instance != null && instance != this) {
                Debug.LogError("Two DatabaseManagers exist at the same time.");
                DestroyImmediate(this);
                return;
            }else if (instance == null) {
                instance = this;
            }
            CreateDatabaseConnection();
        }

        void OnDestroy() {
            sqlConnection.Dispose();
        }

        private void OnApplicationQuit() {
            OnDestroy();
        }

        public async Task<List<List<object>>> SelectQuery( string query ) {
            SqliteCommand command = new SqliteCommand(query);
            List<List<object>> returnValues = new List<List<object>>();
            List<object> row = new List<object>();
            sqlConnection.Open();
            IDataReader reader = await command.ExecuteReaderAsync();
            while(reader.Read()) {
                row.Clear();
                for(int i = 0; i < reader.FieldCount; i++) {
                    row.Add(reader.GetValue(i));
                }
                returnValues.Add(row);
            }
            reader.Dispose();
            await command.DisposeAsync();
            sqlConnection.Close();
            if(sendSqlToLog)
                Debug.Log($"DatabaseManager::SelectQuery() -- Query parsed. \nIn: {query}\nOut:{returnValues}");
            return returnValues;
        }

        public async Task<int> WriteQuery( string query ) {
            SqliteCommand command = new SqliteCommand(query);
            sqlConnection.Open();
            int result = await command.ExecuteNonQueryAsync();
            await command.DisposeAsync();
            sqlConnection.Close();
            if(sendSqlToLog)
                Debug.Log($"DatabaseManager::WriteQuery() -- Query parsed. \nIn: {query}\nOut:{result}");
            return result;
        }
    }
}
