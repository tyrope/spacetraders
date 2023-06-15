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

        private readonly IDbConnection sqlConnection = new SqliteConnection();

        private readonly SqlLogVerbosity sendSqlToLog = SqlLogVerbosity.EVERYTHING; //TODO Sql Verbosity lives here.
        private enum SqlLogVerbosity { NONE, ERROR_ONLY, WRITE_ONLY, WRITE_AND_SCALAR, EVERYTHING }

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
                sqlCommand.Connection = (SqliteConnection) sqlConnection;
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

        public async Task<List<List<object>>> SelectQuery( string query, CancellationToken cancel ) {
            SqliteCommand command = new SqliteCommand(query);
            List<List<object>> returnValues = new List<List<object>>();
            List<object> row = new List<object>();
            sqlConnection.Open();
            command.Connection = (SqliteConnection) sqlConnection;
            IDataReader reader;
            try {
                reader = await command.ExecuteReaderAsync(cancel);
            } catch(SqliteException e) {
                await command.DisposeAsync();
                sqlConnection.Close();
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
            sqlConnection.Close();
            if(cancel.IsCancellationRequested) { return default; } // Don't log or return anything useful on cancellations.
            if(sendSqlToLog >= SqlLogVerbosity.EVERYTHING)
                Debug.Log($"DatabaseManager::SelectQuery() -- Query parsed. ({returnValues.Count} rows)\n{query}");
            return returnValues;
        }

        public async Task<int> WriteQuery( string query, CancellationToken cancel ) {
            SqliteCommand command = new SqliteCommand(query);
            sqlConnection.Open();
            command.Connection = (SqliteConnection) sqlConnection;
            int result;
            try {
                result = await command.ExecuteNonQueryAsync(cancel);
            } catch(SqliteException e) {
                await command.DisposeAsync();
                sqlConnection.Close();
                if(sendSqlToLog >= SqlLogVerbosity.ERROR_ONLY) {
                    Debug.LogError($"SQL Exception from query:\n{query}");
                    Debug.LogException(e);
                }
                return default;
            }
            await command.DisposeAsync();
            sqlConnection.Close();
            if(cancel.IsCancellationRequested) { return default; } // Don't log or return anything useful on cancellations.
            if(sendSqlToLog >= SqlLogVerbosity.WRITE_ONLY)
                Debug.Log($"DatabaseManager::WriteQuery() -- Query parsed. ({result} rows)\n{query}");
            return result;
        }

        public async Task<int> GetLatestRowid( CancellationToken cancel ) {
            SqliteCommand command = new SqliteCommand("select last_insert_rowid()");
            sqlConnection.Open();
            command.Connection = (SqliteConnection) sqlConnection;
            int result;
            try {
                long result64 = (long) await command.ExecuteScalarAsync();
                result = (int) result64;
            } catch(SqliteException e) {
                await command.DisposeAsync();
                sqlConnection.Close();
                if(sendSqlToLog >= SqlLogVerbosity.ERROR_ONLY) {
                    Debug.LogError("SQL Exception from grabbing last_insert_rowid()");
                    Debug.LogException(e);
                }
                return 0;
            }
            await command.DisposeAsync();
            sqlConnection.Close();
            if(cancel.IsCancellationRequested) { return default; }
            if(sendSqlToLog >= SqlLogVerbosity.WRITE_AND_SCALAR)
                Debug.Log($"DatabaseManager::GetLatestRowid() -- Success! {result}");
            return result;
        }
    }
}
