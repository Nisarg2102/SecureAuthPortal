using Dapper;
using SecureAuthPortal.Models;
using System.Data;

namespace SecureAuthPortal.Data
{
    public class UserDAL
    {
        private readonly DapperContext _dapperContext;

        public UserDAL(DapperContext dapperContext)
        {
            _dapperContext = dapperContext;
        }

        // Method to get user by username using stored procedure
        public UserMaster GetUserByUsername(string username)
        {
            using (IDbConnection conn = _dapperContext.CreateConnection())
            {
                var user = conn.QueryFirstOrDefault<UserMaster>(
                    "GetUserByUsername",
                    new { p_username = username },
                    commandType: CommandType.StoredProcedure
                );
                return user;
            }
        }

        // Method to get all users
        public List<UserMaster> GetAllUsers()
        {
            using (IDbConnection conn = _dapperContext.CreateConnection())
            {
                var users = conn.Query<UserMaster>(
                    "SELECT * FROM \"UserMaster\" ORDER BY \"CreatedDate\" DESC"
                ).ToList();
                return users;
            }
        }

        // Method to add user
        public int AddUser(UserMaster user)
        {
            using (IDbConnection conn = _dapperContext.CreateConnection())
            {
                string query = @"INSERT INTO ""UserMaster"" 
                    (""FullName"", ""Username"", ""Password"", ""EmailId"", ""MobileNo"", ""DOB"", ""Gender"", ""RoleId"", ""CreatedDate"")
                    VALUES (@FullName, @Username, @Password, @EmailId, @MobileNo, @DOB, @Gender, @RoleId, @CreatedDate)";
                
                return conn.Execute(query, user);
            }
        }

        // Method to delete user
        public int DeleteUser(long userId)
        {
            using (IDbConnection conn = _dapperContext.CreateConnection())
            {
                string query = "DELETE FROM \"UserMaster\" WHERE \"UserId\" = @UserId";
                return conn.Execute(query, new { UserId = userId });
            }
        }
    }
}