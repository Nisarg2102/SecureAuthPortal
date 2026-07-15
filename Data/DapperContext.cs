using Npgsql;
using System.Data;

namespace SecureAuthPortal.Data
{
    public class DapperContext
    {
        private readonly string _connectionString;

        public DapperContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Method to get database connection
        public IDbConnection CreateConnection()
            => new NpgsqlConnection(_connectionString);
    }
}