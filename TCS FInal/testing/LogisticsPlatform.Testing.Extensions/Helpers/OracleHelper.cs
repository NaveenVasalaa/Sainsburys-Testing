using LogisticsPlatform.Testing.Extensions.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using System.Text;

namespace LogisticsPlatform.Testing.Extensions.Helpers
{
    public class OracleHelper(OracleOptions oracleOptions)
    {
        private readonly OracleOptions _oracleOptions = oracleOptions;
        private string connectionString = "";       

        public async Task InsertDataAsync(string tableName, Dictionary<string, object> fields)
        {
            FileHelper.AppendLine(_oracleOptions.LogFilePath, "Retrieving Connection String");
            var oracleconnectiostring = await GetConnectionStringAsync(_oracleOptions.DepotNumber);
            FileHelper.AppendLine(_oracleOptions.LogFilePath, "Retrieved Connection String");
            FileHelper.AppendLine(_oracleOptions.LogFilePath, "Started Inserting Data into DB");
            int rowsAffected = 0;

            try
            {
                using (var connection = new OracleConnection(oracleconnectiostring))
                {
                    connection.Open();
                    SetOracleSessionTimeZone(connection);
                    using var command = connection.CreateCommand();
                    command.CommandText = GenerateInsertCommandText(tableName, fields);
                    foreach (var field in fields)
                    {
                        command.Parameters.Add(new OracleParameter(field.Key, field.Value ?? DBNull.Value));
                    }

                    rowsAffected = await command.ExecuteNonQueryAsync();
                }

                FileHelper.AppendLine(_oracleOptions.LogFilePath, $"Inserted data into {tableName}, Rows affected : {rowsAffected}");
            }
            catch (Exception ex)
            {
                FileHelper.AppendLine(_oracleOptions.LogFilePath, $"Error while inserting data into DB: {ex.Message}");
                throw;
            }
        }

        public async Task ReadDataAndStoreInFileAsync(string query, string actualPath, string actualFilePrefix)
        {
            FileHelper.AppendLine(_oracleOptions.LogFilePath, "Retrieving Connection String");
            var oracleconnectiostring = await GetConnectionStringAsync(_oracleOptions.DepotNumber);
            FileHelper.AppendLine(_oracleOptions.LogFilePath, "Retrieved Connection String");
            FileHelper.AppendLine(_oracleOptions.LogFilePath, "Started Reading Data from DB");
            StringBuilder stringBuilder = new();
            try
            {
                using (var connection = new OracleConnection(oracleconnectiostring))
                {
                    connection.Open();
                    SetOracleSessionTimeZone(connection);
                    using var command = connection.CreateCommand();
                    command.CommandText = query;

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            object value = reader.GetValue(i);
                            FileHelper.AppendLine(_oracleOptions.LogFilePath, $"Column {i}: {reader.GetName(i)} - {reader.GetFieldType(i)}");
                            if (value is OracleBlob blob)
                            {
                                byte[] blobData = new byte[blob.Length];
                                blob.Read(blobData, 0, (int)blob.Length);
                                stringBuilder.Append(Convert.ToBase64String(blobData)); // or handle as needed
                            }
                            else if (value is OracleClob clob)
                            {
                                string clobData = clob.Value;
                                stringBuilder.Append(clobData);
                            }
                            else
                            {
                                stringBuilder.Append(value);
                            }

                            if (i < reader.FieldCount - 1)
                            {
                                stringBuilder.Append(',');
                            }
                        }
                        stringBuilder.AppendLine();
                    }
                }

                FileHelper.AppendLine($"{actualPath}/{actualFilePrefix}.csv", stringBuilder.ToString().TrimEnd());
                FileHelper.AppendLine(_oracleOptions.LogFilePath, $"Data read from DB and stored in {actualPath}");
            }
            catch (Exception ex)
            {
                FileHelper.AppendLine(_oracleOptions.LogFilePath, $"Error while reading data from DB: {ex.Message}");
                throw;
            }
        }

        private async Task<string> GetConnectionStringAsync(string depotNo)
        {
            MongoHelper<WarehouseConnectionLookup> mongoHelper = new(_oracleOptions.ConnectionString, _oracleOptions.DatabaseName, _oracleOptions.CollectionName, _oracleOptions.LogFilePath);
            var builder = Builders<WarehouseConnectionLookup>.Filter;

            var filter = builder.Eq(x => x.DepotNumber, depotNo) & builder.Eq(x => x.IsEnabled, true);
            var projection = Builders<WarehouseConnectionLookup>.Projection.Exclude(x => x.Id);

            var findOptions = new FindOptions<WarehouseConnectionLookup, BsonDocument>
            {
                Projection = projection
            };

            var document = await mongoHelper.GetAsync(filter, findOptions);
            var message = BsonSerializer.Deserialize<WarehouseConnectionLookup>(document);
            var oraclePwd = AzureHelper.GetSecret(message.KeyVaultName, message.Password);
            connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=tcp)(HOST={message.Server})(PORT=1521))(CONNECT_DATA=(SERVICE_NAME={message.DataBase})));User Id={message.UserName};Password={oraclePwd};Connection Timeout=60;";

            return connectionString;
        }

        private static string GenerateInsertCommandText(string tableName, Dictionary<string, object> parameters)
        {
            var columns = string.Join(", ", parameters.Keys);
            var values = string.Join(", ", parameters.Keys.Select(key => $":{key}"));
            return $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
        }

        // Helper method for timezone fix
        private static void SetOracleSessionTimeZone(OracleConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER SESSION SET TIME_ZONE = 'UTC'";
            cmd.ExecuteNonQuery();
        }
    }
}
