using Dapper;
using Microsoft.Data.SqlClient;
using SnippetsLibraryWebApp.Models;
using SnippetsLibraryWebApp.Utils;

namespace SnippetsLibraryWebApp.Repository
{
    public class ProgrammingLanguageRepository
    {
        public async Task<IEnumerable<ProgrammingLanguageModel>> GetAllProgrammingLanguagesAsync()
        {
            try
            {
                string connectionString = ConfigurationHelper.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT ID, Name FROM ProgrammingLanguage";
                    return await connection.QueryAsync<ProgrammingLanguageModel>(query);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while getting all programming languages: " + ex.Message);
                return null;
            }
        }

        public async Task<ProgrammingLanguageModel> GetLanguageByIdAsync(int languageId)
        {
            try
            {
                string connectionString = ConfigurationHelper.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT * FROM ProgrammingLanguage WHERE ID = @LanguageId";
                    return await connection.QueryFirstOrDefaultAsync<ProgrammingLanguageModel>(query, new { LanguageId = languageId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while getting programming language by id: " + ex.Message);
                return null;
            }
        }
    }
}
