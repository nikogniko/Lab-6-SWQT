using SnippetsLibraryWebApp.Models;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using SnippetsLibraryWebApp.Utils;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using Duende.IdentityServer.Validation;
using NuGet.Protocol.Core.Types;
using IdentityModel.Client;

namespace SnippetsLibraryWebApp.Repository
{
    public class SnippetRepository
    {
        private readonly CategoryRepository _categoryRepository;
        private readonly TagRepository _tagRepository;

        public SnippetRepository(CategoryRepository categoryRepository, TagRepository tagRepository)
        {
            _categoryRepository = categoryRepository;
            _tagRepository = tagRepository;
        }

        public async Task<bool> IsUserAuthorOfSnippetAsync(int snippetId, int userId)
        {
            string connectionString = ConfigurationHelper.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT COUNT(1) 
                    FROM Snippet
                    WHERE ID = @SnippetID AND AuthorID = @UserID;
                ";

                var isAuthor = await connection.ExecuteScalarAsync<bool>(query, new { SnippetID = snippetId, UserID = userId });
                return isAuthor;
            }
        }

        private async Task<IEnumerable<SnippetModel>> EnrichSnippetsWithTagsAndCategories(IEnumerable<SnippetModel> snippets)
        {
            foreach(var snippet in snippets)
            {
                if (snippet != null)
                {
                    var tags = await _tagRepository.GetTagsBySnippetIdAsync(snippet.ID);
                    var categories = await _categoryRepository.GetCategoriesBySnippetIdAsync(snippet.ID);

                    snippet.Tags = tags;
                    snippet.Categories = categories;
                }
            }

            return snippets;
        }

        // Метод для отримання всіх сніпетів з фільтрами та сортуванням
        public async Task<IEnumerable<SnippetModel>> GetFilteredSnippetsAsync(
            int[] authorIds,
            int[] categoryIds,
            int[] tagIds,
            int[] languageIds,
            string sortOrder)
        {
            var sql = new StringBuilder();
            sql.Append(@"SELECT DISTINCT s.*,
                (SELECT COUNT(*) FROM SavedSnippets ss WHERE ss.SnippetID = s.ID) AS SavesCount
                FROM Snippet s
                LEFT JOIN SnippetCategory sc ON s.ID = sc.SnippetID
                LEFT JOIN Category c ON sc.CategoryID = c.ID
                LEFT JOIN SnippetTag st ON s.ID = st.SnippetID
                LEFT JOIN Tag t ON st.TagID = t.ID
                WHERE [Status] = 'public' AND 1=1");

            var parameters = new DynamicParameters();

            // Фільтрація за автором, категоріями, тегами та мовою
            AppendFilterIfNotEmpty(sql, parameters, "s.AuthorID", "AuthorIds", authorIds);
            AppendFilterIfNotEmpty(sql, parameters, "c.ID", "CategoryIds", categoryIds);
            AppendFilterIfNotEmpty(sql, parameters, "t.ID", "TagIds", tagIds);
            AppendFilterIfNotEmpty(sql, parameters, "s.ProgrammingLanguageID", "LanguageIds", languageIds);

            // Додавання сортування
            AppendSortOrder(sql, sortOrder);

            string connectionString = ConfigurationHelper.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                var snippets = await connection.QueryAsync<SnippetModel>(sql.ToString(), parameters);
                snippets = await EnrichSnippetsWithTagsAndCategories(snippets);
                return snippets;
            }
        }

        // Метод для отримання обраних сніпетів з фільтрами та сортуванням
        public async Task<IEnumerable<SnippetModel>> GetFavoriteSnippetsAsync(
            int userId,
            int[] authorIds,
            int[] categoryIds,
            int[] tagIds,
            int[] languageIds,
            string sortOrder)
        {
            var sql = new StringBuilder();
            sql.Append(@"SELECT DISTINCT s.*,
                (SELECT COUNT(*) FROM SavedSnippets ss WHERE ss.SnippetID = s.ID) AS SavesCount
                FROM Snippet s
                INNER JOIN SavedSnippets ss ON s.ID = ss.SnippetID
                WHERE ss.UserID = @UserId");

            var parameters = new DynamicParameters();
            parameters.Add("UserId", userId);

            AppendFilterIfNotEmpty(sql, parameters, "s.AuthorID", "AuthorIds", authorIds);

            // Для категорій та тегів використовуємо підзапити
            AppendSubqueryFilter(sql, parameters, categoryIds, "CategoryIds", "s.ID IN (SELECT SnippetID FROM SnippetCategories WHERE CategoryID IN @CategoryIds)");
            AppendSubqueryFilter(sql, parameters, tagIds, "TagIds", "s.ID IN (SELECT SnippetID FROM SnippetTags WHERE TagID IN @TagIds)");

            AppendFilterIfNotEmpty(sql, parameters, "s.ProgrammingLanguageID", "LanguageIds", languageIds);

            AppendSortOrder(sql, sortOrder);

            string connectionString = ConfigurationHelper.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                var snippets = await connection.QueryAsync<SnippetModel>(sql.ToString(), parameters);
                snippets = await EnrichSnippetsWithTagsAndCategories(snippets);
                return snippets;
            }
        }

        // Метод для отримання авторських сніпетів з фільтрами та сортуванням
        public async Task<IEnumerable<SnippetModel>> GetMySnippetsAsync(
            int userId,
            int[] authorIds,
            int[] categoryIds,
            int[] tagIds,
            int[] languageIds,
            string sortOrder)
        {
            var sql = new StringBuilder();
            sql.Append(@"SELECT DISTINCT s.*,
                (SELECT COUNT(*) FROM SavedSnippets ss WHERE ss.SnippetID = s.ID) AS SavesCount
                FROM Snippet s
                WHERE s.AuthorID = @UserId");

            var parameters = new DynamicParameters();
            parameters.Add("UserId", userId);

            AppendFilterIfNotEmpty(sql, parameters, "s.AuthorID", "AuthorIds", authorIds);
            AppendCategoryFilter(sql, parameters, categoryIds);
            AppendTagFilter(sql, parameters, tagIds);
            AppendFilterIfNotEmpty(sql, parameters, "s.ProgrammingLanguageID", "LanguageIds", languageIds);
            AppendSortOrder(sql, sortOrder);

            string connectionString = ConfigurationHelper.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                var snippets = await connection.QueryAsync<SnippetModel>(sql.ToString(), parameters);
                snippets = await EnrichSnippetsWithTagsAndCategories(snippets);
                return snippets;
            }
        }

        private void AppendSubqueryFilter(StringBuilder sql, DynamicParameters parameters, int[] values, string paramName, string subquery)
        {
            if (values != null && values.Length > 0)
            {
                sql.Append(" AND " + subquery);
                parameters.Add(paramName, values);
            }
        }

        private void AppendFilterIfNotEmpty(StringBuilder sql, DynamicParameters parameters, string columnName, string paramName, int[] values)
        {
            if (values != null && values.Length > 0)
            {
                sql.Append($" AND {columnName} IN @{paramName}");
                parameters.Add(paramName, values);
            }
        }

        private void AppendCategoryFilter(StringBuilder sql, DynamicParameters parameters, int[] categoryIds)
        {
            if (categoryIds != null && categoryIds.Length > 0)
            {
                sql.Append(" AND s.ID IN (");
                sql.Append("SELECT SnippetID FROM SnippetCategories WHERE CategoryID IN @CategoryIds ");
                sql.Append("GROUP BY SnippetID HAVING COUNT(DISTINCT CategoryID) = @CategoryCount)");
                parameters.Add("CategoryIds", categoryIds);
                parameters.Add("CategoryCount", categoryIds.Length);
            }
        }

        private void AppendTagFilter(StringBuilder sql, DynamicParameters parameters, int[] tagIds)
        {
            if (tagIds != null && tagIds.Length > 0)
            {
                sql.Append(" AND s.ID IN (");
                sql.Append("SELECT SnippetID FROM SnippetTags WHERE TagID IN @TagIds ");
                sql.Append("GROUP BY SnippetID HAVING COUNT(DISTINCT TagID) = @TagCount)");
                parameters.Add("TagIds", tagIds);
                parameters.Add("TagCount", tagIds.Length);
            }
        }

        private void AppendSortOrder(StringBuilder sql, string sortOrder)
        {
            string orderBy = " ORDER BY s.UpdatedAt DESC"; // Значення за замовчуванням
            if (!string.IsNullOrEmpty(sortOrder))
            {
                switch (sortOrder)
                {
                    case "date_desc":
                        orderBy = " ORDER BY s.UpdatedAt DESC";
                        break;
                    case "date_asc":
                        orderBy = " ORDER BY s.UpdatedAt ASC";
                        break;
                    case "name_asc":
                        orderBy = " ORDER BY s.Title ASC";
                        break;
                    case "name_desc":
                        orderBy = " ORDER BY s.Title DESC";
                        break;
                    case "favorite_asc":
                        orderBy = " ORDER BY SavesCount ASC";
                        break;
                    case "favorite_desc":
                        orderBy = " ORDER BY SavesCount DESC";
                        break;
                }
            }
            sql.Append(orderBy);
        }

        public async Task<bool> IsSnippedSavedByUser(int userId, int snippetId)
        {
            string connectionString = ConfigurationHelper.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"SELECT UserId, SnippetId FROM SavedSnippets WHERE UserID = @userId AND SnippetID = @snippetId";

                var result = (await connection.QueryAsync(query, new { userId, snippetId })).FirstOrDefault();

                if(result != null &&
                    result.UserId != null &&
                    result.SnippetId != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task<IEnumerable<SnippetModel>> GetAllSnippetsAsync()
        {
            string connectionString = ConfigurationHelper.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT Snippet.ID, Title, Description, ProgrammingLanguageID, AuthorID, CreatedAt, UpdatedAt, Code, Status, 
                    COUNT(SavedSnippets.UserID) AS SavesCount 
                    FROM Snippet
                    LEFT JOIN SavedSnippets ON SavedSnippets.SnippetID = Snippet.ID
                    GROUP BY Snippet.ID, Title, Description, ProgrammingLanguageID, AuthorID, CreatedAt, UpdatedAt, Code, Status;
                ";

                var snippets = await connection.QueryAsync<SnippetModel>(query);

                // Завантаження категорій і тегів для кожного сніпета
                foreach (var snippet in snippets)
                {
                    /*snippet.Categories = await GetCategoriesBySnippetIdAsync(snippet.ID);
                    snippet.Tags = await GetTagsBySnippetIdAsync(snippet.ID);*/
                    snippet.Categories = await _categoryRepository.GetCategoriesBySnippetIdAsync(snippet.ID);
                    snippet.Tags = await _tagRepository.GetTagsBySnippetIdAsync(snippet.ID);
                    snippet.Code = Base64Helper.Base64Decode(snippet.Code);
                }
                return snippets;
            }
        }

        public async Task<SnippetModel> GetSnippetByIdAsync(int snippetId)
        {
            string connectionString = ConfigurationHelper.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT Snippet.ID, Title, Description, ProgrammingLanguageID, Snippet.AuthorID, CreatedAt, UpdatedAt, Code, Status, 
                    COUNT(SavedSnippets.UserID) AS SavesCount
                    FROM Snippet
                    LEFT JOIN SavedSnippets ON SavedSnippets.SnippetID = Snippet.ID
                    WHERE Snippet.ID = @CurrentSnippetID
                    GROUP BY Snippet.ID, Title, Description, ProgrammingLanguageID, Snippet.AuthorID, CreatedAt, UpdatedAt, Code, Status;
                ";

                var snippet = await connection.QuerySingleOrDefaultAsync<SnippetModel>(query, new { CurrentSnippetID = snippetId });

                if (snippet != null)
                {
                    // Завантаження категорій і тегів для сніпета
                    snippet.Categories = await _categoryRepository.GetCategoriesBySnippetIdAsync(snippet.ID);
                    snippet.Tags = await _tagRepository.GetTagsBySnippetIdAsync(snippet.ID);
                    snippet.Code = Base64Helper.Base64Decode(snippet.Code);
                }

                return snippet;
            }
        }

        public async Task<bool> DeleteSnippetAsync(int snippetId, int userId)
        {
            // Перевіряємо, чи є користувач автором сніпета
            var isAuthor = await IsUserAuthorOfSnippetAsync(snippetId, userId);
            if (!isAuthor)
            {
                return false; // Якщо користувач не є автором, видалення неможливе
            }

            string connectionString = ConfigurationHelper.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
                    DELETE FROM Snippet
                    WHERE ID = @SnippetID;
                ";

                var rowsAffected = await connection.ExecuteAsync(query, new { SnippetID = snippetId });
                return rowsAffected > 0;
            }
        }

        public async Task<bool> UpdateSnippetAsync(SnippetModel snippet, int userId, bool areCategoriesChanged, bool areTagsChanged)
        {
            var base64Code = Base64Helper.Base64Encode(snippet.Code);

            // Перевіряємо, чи є користувач автором сніпета
            var isAuthor = await IsUserAuthorOfSnippetAsync(snippet.ID, userId);
            if (!isAuthor)
            {
                return false; // Якщо користувач не є автором, редагування неможливе
            }

            string connectionString = ConfigurationHelper.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Оновлення основної інформації про сніпет
                        string updateSnippetSql = @"
                            UPDATE Snippet
                            SET Title = @Title,
                                Description = @Description,
                                ProgrammingLanguageID = @ProgrammingLanguageID,
                                Code = @Code,
                                Status = @Status
                            WHERE ID = @SnippetID;
                        ";

                        var rowsAffected = await connection.ExecuteAsync(updateSnippetSql, new
                        {
                            snippet.Title,
                            snippet.Description,
                            snippet.ProgrammingLanguageID,
                            Code = base64Code,
                            snippet.Status,
                            SnippetID = snippet.ID
                        }, transaction);


                        // Перевірка та оновлення категорій, якщо необхідно
                        bool areCategoriesUpdated = await _categoryRepository.UpdateSnippetCategoriesAsync(snippet.ID, snippet.Categories, connection, transaction, areCategoriesChanged);

                        // Перевірка та оновлення тегів, якщо необхідно
                        bool areTagsUpdated = await _tagRepository.UpdateSnippetTagsAsync(snippet.ID, snippet.Tags, connection, transaction, areTagsChanged);

                        if (rowsAffected == 0 && !areCategoriesUpdated && !areTagsUpdated)
                        {
                            transaction.Rollback();
                            return false; // Якщо жодного рядка не було оновлено
                        }

                        // Фіксація транзакції
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // Відкат транзакції у разі помилки
                        transaction.Rollback();
                        Console.WriteLine("Error while updating snippet: " + ex.Message);
                        return false;
                    }
                }
            }
        }

        public async Task<int?> AddSnippetAsync(SnippetModel snippet)
        {
            snippet.Code = Base64Helper.Base64Encode(snippet.Code);

            string connectionString = ConfigurationHelper.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Додавання основної інформації про сніпет
                        string insertSnippetSql = @"
                            INSERT INTO Snippet (Title, Description, ProgrammingLanguageID, Code, Status, AuthorID)
                            VALUES (@Title, @Description, @ProgrammingLanguageID, @Code, @Status, @AuthorID);
                            SELECT CAST(SCOPE_IDENTITY() as int);
                        ";

                        int snippetId = await connection.QuerySingleAsync<int>(insertSnippetSql, new
                        {
                            snippet.Title,
                            snippet.Description,
                            snippet.ProgrammingLanguageID,
                            snippet.Code,
                            snippet.Status,
                            snippet.AuthorID
                        }, transaction);

                        // Додавання зв'язків для категорій
                        if (snippet.Categories != null && snippet.Categories.Any())
                        {
                            await _categoryRepository.AddSnippetCategoriesAsync(snippetId, snippet.Categories, connection, transaction);
                        }

                        // Додавання зв'язків для тегів
                        if (snippet.Tags != null && snippet.Tags.Any())
                        {
                            await _tagRepository.AddSnippetTagsAsync(snippetId, snippet.Tags, connection, transaction);
                        }

                        // Фіксація транзакції
                        transaction.Commit();
                        return snippetId;
                    }
                    catch (Exception ex)
                    {
                        // Відкат транзакції у разі помилки
                        transaction.Rollback();
                        Console.WriteLine("Error while adding snippet: " + ex.Message);
                        return null;
                    }
                }
            }
        }

        public async Task<IEnumerable<SnippetModel>> GetAllPublicSnippetsAsync()
        {
            string connectionString = ConfigurationHelper.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
                      SELECT Snippet.ID, Title, Description, ProgrammingLanguageID, Snippet.AuthorID, CreatedAt, UpdatedAt, 
                      Code, Status, COUNT(SavedSnippets.UserID) AS SavesCount
                      FROM Snippet
                      LEFT JOIN SavedSnippets ON SavedSnippets.SnippetID = Snippet.ID
                      WHERE Status = 'public'
                      GROUP BY Snippet.ID, Title, Description, ProgrammingLanguageID, Snippet.AuthorID, CreatedAt, UpdatedAt, 
                      Code, Status;
                ";

                var publicSnippets = await connection.QueryAsync<SnippetModel>(query);

                foreach (var snippet in publicSnippets)
                {
                    snippet.Code = Base64Helper.Base64Decode(snippet.Code);
                }

                return publicSnippets;
            }
        }

        public async Task<IEnumerable<SnippetModel>> GetUserSnippetsAsync(int userId)
        {
            string connectionString = ConfigurationHelper.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT Snippet.ID, Title, Description, ProgrammingLanguageID, Snippet.AuthorID, CreatedAt, UpdatedAt, Code, Status,
                    COUNT(SavedSnippets.UserID) AS SavesCount
                    FROM Snippet
                    LEFT JOIN SavedSnippets ON SavedSnippets.SnippetID = Snippet.ID
                    WHERE Snippet.AuthorID = @CurrentUserID
                    GROUP BY Snippet.ID, Title, Description, ProgrammingLanguageID, Snippet.AuthorID, CreatedAt, UpdatedAt, Code, Status;
                ";

                var userSnippets = await connection.QueryAsync<SnippetModel>(query, new { CurrentUserID = userId });

                foreach (var snippet in userSnippets)
                {
                    snippet.Code = Base64Helper.Base64Decode(snippet.Code);
                }

                return userSnippets;
            }
        }

        public async Task<IEnumerable<SnippetModel>> GetSavedSnippetsByUserAsync(int userId)
        {
            string connectionString = ConfigurationHelper.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT Snippet.ID AS ID, Title, Description, ProgrammingLanguageID, Snippet.AuthorID, CreatedAt, UpdatedAt, 
                           Code, Status, COUNT(SavedSnippets.UserID) AS SavesCount
                    FROM SavedSnippets
                    LEFT JOIN Snippet ON Snippet.ID = SavedSnippets.SnippetID
                    WHERE SavedSnippets.UserID = @CurrentUserID
                    GROUP BY Snippet.ID, Title, Description, ProgrammingLanguageID, Snippet.AuthorID, CreatedAt, UpdatedAt, 
                             Code, Status;
                ";

                var savedSnippets = await connection.QueryAsync<SnippetModel>(query, new { CurrentUserID = userId });

                foreach (var snippet in savedSnippets)
                {
                    snippet.Code = Base64Helper.Base64Decode(snippet.Code);
                }

                return savedSnippets;
            }
        }

        public async Task<bool> AddSnippetToSavedAsync(int userId, int snippetId)
        {
            string connectionString = ConfigurationHelper.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
                    INSERT INTO SavedSnippets (UserID, SnippetID)
                    VALUES (@CurrentUserID, @CurrentSnippetID);
                ";

                var rowsAffected = await connection.ExecuteAsync(query, new
                {
                    CurrentUserID = userId,
                    CurrentSnippetID = snippetId
                });

                return rowsAffected > 0;
            }
        }

        public async Task<bool> RemoveSnippetFromSavedAsync(int userId, int snippetId)
        {
            string connectionString = ConfigurationHelper.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
                    DELETE FROM SavedSnippets
                    WHERE UserID = @CurrentUserID AND SnippetID = @CurrentSnippetID;
                ";

                var rowsAffected = await connection.ExecuteAsync(query, new
                {
                    CurrentUserID = userId,
                    CurrentSnippetID = snippetId
                });

                return rowsAffected > 0;
            }
        }
    }
}
