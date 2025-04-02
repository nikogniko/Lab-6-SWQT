using Dapper;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using SnippetsLibraryWebApp.Models;
using SnippetsLibraryWebApp.Utils;
using System.Text;

namespace SnippetsLibraryWebApp.Repository
{
    public class UserRepository
    {
        public async Task<IEnumerable<AuthorModel>> GetAllAuthors(string filterTerm = "")
        {
            string connectionString = ConfigurationHelper.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
                SELECT 
                    [User].ID as Id, 
                    [User].Username, 
                    COUNT(Snippet.ID) AS SnippetCount
                FROM 
                    [User]
                JOIN 
                    Snippet ON [User].ID = Snippet.AuthorID
                GROUP BY 
                    [User].ID, 
                    [User].Username
                HAVING 
                    COUNT(Snippet.ID) > 0 AND [User].Username LIKE @FilterTerm
                "
                ;
                
                var authors = await connection.QueryAsync<AuthorModel>(query, new { FilterTerm = $"%{filterTerm}%" });

                return authors;
            }
        }

        public async Task<string> GetUsernameByIdAsync(int userId)
        {
            try
            {
                string connectionString = ConfigurationHelper.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT Username FROM [User] WHERE ID = @UserId";
                    var username = await connection.QueryFirstOrDefaultAsync<string>(query, new { UserId = userId });

                    return username;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching username: " + ex.Message);
                return null;
            }
        }

        // Метод для реєстрації нового користувача з використанням Dapper
        public async Task<int?> AddUserAsync(string username, string email, string password)
        {
            try
            {
                string connectionString = ConfigurationHelper.GetConnectionString(); // Метод для отримання рядка підключення
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "INSERT INTO [User] (Username, Email, Password) VALUES (@Username, @Email, @PasswordHash)";
                    var parameters = new { Username = username, Email = email, PasswordHash = HashHelper.HashPassword(password) };
                    int rowsAffected = await connection.ExecuteAsync(query, parameters);

                    if (rowsAffected > 0)
                    {
                        query = "SELECT ID FROM [User] WHERE Email = @Email";
                        int? userId = await connection.QueryFirstOrDefaultAsync<int?>(query, new { Email = email });

                        return userId;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error registering user: " + ex.Message);
                return null;
            }
            return null;
        }

        // метод для авторизації (з Dapper)
        public async Task<UserModel> GetUserAsync(string email, string password)
        {
            try
            {
                string connectionString = ConfigurationHelper.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT ID as Id, Username, Email, Password as PasswordHash FROM [User] WHERE Email = @Email";
                    var user = await connection.QueryFirstOrDefaultAsync<UserModel>(query, new { Email = email });

                    if (user != null && user.PasswordHash == HashHelper.HashPassword(password))
                    {
                        return user;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during login: " + ex.Message);
                return null;
            }
            // Якщо авторизація неуспішна
            return null;
        }

        // TODO: GetUserById(int userId) якщо буде вікно профіля користувача

        // TODO: EditUser(int userId) якщо буде профіль користувача який має редагуватися

    }
}
