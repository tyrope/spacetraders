using System.Data;
using System.IO;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace STCommander
{
    public class DatabaseManager : MonoBehaviour
    {
        public static DatabaseManager instance;
        public static string RowIDQuery => "SELECT last_insert_rowid();";

        private readonly SqliteConnection sqlConnection = new SqliteConnection();

        private enum SqlLogVerbosity { NONE, ERROR_ONLY, WRITE_NONSCALAR, WRITE_ALL, EVERYTHING }
        private readonly SqlLogVerbosity sendSqlToLog = SqlLogVerbosity.ERROR_ONLY; //TODO Sql Verbosity lives here.

        /// <summary>
        /// Generates an Connection object to the database, and if the database doesn't exist yet, creates it.
        /// </summary>
        /// <returns>A closed connection to the SQL database.</returns>
        private void CreateDatabaseConnection() {
            string filePath = Path.Combine(Application.persistentDataPath, "stCommander.db");
            sqlConnection.ConnectionString = "URI=file:" + filePath;
            if(!File.Exists(filePath)) {
                SqliteConnection.CreateFile(filePath);
                string query = Resources.Load<TextAsset>("stCommanderSchema.sql").text;
                SqliteCommand sqlCommand = new SqliteCommand(query);
                sqlConnection.Open();
                sqlCommand.Connection = sqlConnection;
                sqlCommand.ExecuteNonQuery();
                sqlCommand.Dispose();
                sqlConnection.Close();
            }
        }

        private void Awake() {
            if(instance != null && instance != this) {
                Debug.LogError("Two DatabaseManagers exist at the same time.");
                DestroyImmediate(this);
                return;
            } else if(instance == null) {
                CreateDatabaseConnection();
                instance = this;
            }
        }

        void OnDestroy() {
            instance = null; // Idk if destroyed objects are nulled. Be explicit.
            sqlConnection.Dispose();
        }

        private void OnApplicationQuit() {
            OnDestroy();
        }

        public async Task<List<List<object>>> SelectQuery( string query, CancellationToken cancel ) {
            SqliteCommand command = new SqliteCommand(query);
            List<List<object>> returnValues = new List<List<object>>();
            List<object> row = new List<object>();
           await sqlConnection.OpenAsync();
            command.Connection = sqlConnection;
            IDataReader reader;
            try {
                reader = await command.ExecuteReaderAsync(cancel);
            } catch(SqliteException e) {
                await command.DisposeAsync();
                await sqlConnection.CloseAsync();
                if(sendSqlToLog >= SqlLogVerbosity.ERROR_ONLY) {
                    Debug.LogError($"SQL Exception from query:\n{query}");
                    Debug.LogException(e);
                }
                return default;
            }
            while(reader.Read()) {
                if(cancel.IsCancellationRequested) {
                    // Stop reading if we've cancelled.
                    break;
                }
                row.Clear();
                for(int i = 0; i < reader.FieldCount; i++) {
                    row.Add(reader.GetValue(i));
                }
                returnValues.Add(row);
            }
            reader.Dispose();
            await command.DisposeAsync();
            await sqlConnection.CloseAsync();
            if(cancel.IsCancellationRequested) { return default; } // Don't log or return anything useful on cancellations.
            if(sendSqlToLog >= SqlLogVerbosity.EVERYTHING)
                Debug.Log($"DatabaseManager::SelectQuery() -- Query parsed. ({returnValues.Count} rows)\n{query}");
            return returnValues;
        }

        public async Task<int> WriteQuery( string query, CancellationToken cancel, bool requestingRowId = false ) {
            SqliteCommand command = new SqliteCommand(query);
            await sqlConnection.OpenAsync();
            command.Connection = sqlConnection;
            int result;
            try {
                if(requestingRowId) {
                    result = (int) (long) await command.ExecuteScalarAsync(cancel);
                } else {
                result = await command.ExecuteNonQueryAsync(cancel);
                }
            } catch(SqliteException e) {
                await command.DisposeAsync();
                await sqlConnection.CloseAsync();
                if(sendSqlToLog >= SqlLogVerbosity.ERROR_ONLY) {
                    Debug.LogError($"SQL Exception from query:\n{query}");
                    Debug.LogException(e);
                }
                return default;
            }
            await command.DisposeAsync();
            await sqlConnection.CloseAsync();
            if(cancel.IsCancellationRequested) { return default; } // Don't log or return anything useful on cancellations.
            if(requestingRowId) {
                if(sendSqlToLog >= SqlLogVerbosity.WRITE_ALL) {
                    Debug.Log($"DatabaseManager::WriteQuery() -- Query parsed. (Row ID: {result})\n{query}");
                }
            } else if(sendSqlToLog >= SqlLogVerbosity.WRITE_NONSCALAR) {
                Debug.Log($"DatabaseManager::WriteQuery() -- Query parsed. ({result} rows)\n{query}");
            }
            return result;
        }
    }
}
