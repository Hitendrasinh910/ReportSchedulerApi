using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
namespace ReportSchedulerApi.Helpers
{

    public interface IDapperHelper
    {
        Task<IEnumerable<T>> QueryAsync<T>(
            string sp,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            string connectionName = "ReportSchedulerDb");

        Task<T?> QueryFirstOrDefaultAsync<T>(
            string sp,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            string connectionName = "ReportSchedulerDb");

        Task<T?> QuerySingleOrDefaultAsync<T>(
            string sp,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            string connectionName = "ReportSchedulerDb");

        Task<T> QuerySingleAsync<T>(
            string sp,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            string connectionName = "ReportSchedulerDb");

        Task<int> ExecuteAsync(
            string sp,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            string connectionName = "ReportSchedulerDb");

        Task<(SqlMapper.GridReader Reader, SqlConnection Conn)> QueryMultipleAsync(
            string sp,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            string connectionName = "ReportSchedulerDb");

        Task<T?> ExecuteScalarAsync<T>(
            string sp,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            string connectionName = "ReportSchedulerDb");
    }

    public class DapperHelper : IDapperHelper
    {
        private readonly IConfiguration _config;
        private readonly int _defaultTimeout = 60;

        public DapperHelper(IConfiguration config)
        {
            _config = config;
        }

        private SqlConnection CreateConnection(string connectionName)
        {
            var connectionString = _config.GetConnectionString(connectionName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception($"Connection string '{connectionName}' not found.");
            }

            return new SqlConnection(connectionString);
        }

        public async Task<int> ExecuteAsync(
            string sp,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            string connectionName = "ReportSchedulerDb")
        {
            try
            {
                using var conn = CreateConnection(connectionName);
                await conn.OpenAsync();

                return await conn.ExecuteAsync(
                    sp,
                    param,
                    commandType: commandType,
                    commandTimeout: _defaultTimeout);
            }
            catch (Exception ex)
            {
                throw new Exception($"Database operation failed in {sp}. {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(
            string sp,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            string connectionName = "ReportSchedulerDb")
        {
            try
            {
                using var conn = CreateConnection(connectionName);
                await conn.OpenAsync();

                return await conn.QueryAsync<T>(
                    sp,
                    param,
                    commandType: commandType,
                    commandTimeout: _defaultTimeout);
            }
            catch (Exception ex)
            {
                throw new Exception($"Database query failed in {sp}. {ex.Message}", ex);
            }
        }

        public async Task<T?> QueryFirstOrDefaultAsync<T>(
            string sp,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            string connectionName = "ReportSchedulerDb")
        {
            try
            {
                using var conn = CreateConnection(connectionName);
                await conn.OpenAsync();

                return await conn.QueryFirstOrDefaultAsync<T>(
                    sp,
                    param,
                    commandType: commandType,
                    commandTimeout: _defaultTimeout);
            }
            catch (Exception ex)
            {
                throw new Exception($"Database query failed in {sp}. {ex.Message}", ex);
            }
        }

        public async Task<T> QuerySingleAsync<T>(
            string sp,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            string connectionName = "ReportSchedulerDb")
        {
            try
            {
                using var conn = CreateConnection(connectionName);
                await conn.OpenAsync();

                return await conn.QuerySingleAsync<T>(
                    sp,
                    param,
                    commandType: commandType,
                    commandTimeout: _defaultTimeout);
            }
            catch (Exception ex)
            {
                throw new Exception($"Database query failed in {sp}. {ex.Message}", ex);
            }
        }

        public async Task<T?> QuerySingleOrDefaultAsync<T>(
            string sp,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            string connectionName = "ReportSchedulerDb")
        {
            try
            {
                using var conn = CreateConnection(connectionName);
                await conn.OpenAsync();

                return await conn.QuerySingleOrDefaultAsync<T>(
                    sp,
                    param,
                    commandType: commandType,
                    commandTimeout: _defaultTimeout);
            }
            catch (Exception ex)
            {
                throw new Exception($"Database query failed in {sp}. {ex.Message}", ex);
            }
        }

        public async Task<(SqlMapper.GridReader Reader, SqlConnection Conn)> QueryMultipleAsync(
            string sp,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            string connectionName = "ReportSchedulerDb")
        {
            var conn = CreateConnection(connectionName);

            try
            {
                await conn.OpenAsync();

                var reader = await conn.QueryMultipleAsync(
                    sp,
                    param,
                    commandType: commandType,
                    commandTimeout: _defaultTimeout);

                return (reader, conn);
            }
            catch (Exception ex)
            {
                conn.Dispose();
                throw new Exception($"Database multi-query failed in {sp}. {ex.Message}", ex);
            }
        }

        public async Task<T?> ExecuteScalarAsync<T>(
            string sp,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            string connectionName = "ReportSchedulerDb")
        {
            try
            {
                using var conn = CreateConnection(connectionName);
                await conn.OpenAsync();

                return await conn.ExecuteScalarAsync<T>(
                    sp,
                    param,
                    commandType: commandType,
                    commandTimeout: _defaultTimeout);
            }
            catch (Exception ex)
            {
                throw new Exception($"Database scalar query failed in {sp}. {ex.Message}", ex);
            }
        }
    }

    //private void ThrowFormattedException(string sp, object? param, Exception ex)
    //{
    //    Log.Error(ex, "SQL Error in {StoredProcedure} with params {@Params}", sp, param);

    //    if (ex is SqlException sqlEx && _showSqlError)
    //    {
    //        var detailedMessage =
    //            $"SQL Error {sqlEx.Number} at Line {sqlEx.LineNumber} " +
    //            $"in {sqlEx.Procedure}: {sqlEx.Message}";

    //        throw new Exception(detailedMessage, sqlEx);
    //    }

    //    throw new Exception("Database operation failed. Please contact administrator.");
    //}
}

