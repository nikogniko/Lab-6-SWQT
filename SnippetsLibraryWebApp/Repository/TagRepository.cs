using Azure;
using Dapper;
using Microsoft.Data.SqlClient;
using SnippetsLibraryWebApp.Models;
using SnippetsLibraryWebApp.Utils;

namespace SnippetsLibraryWebApp.Repository
{
    public class TagRepository
    {
        public async Task<IEnumerable<TagModel>> SearchTagsAsync(string query = "", int amount = -1)
        {
            var take = amount <= 0 ? "" : "TOP " + amount.ToString();

            var sql = @$"SELECT {take} ID, Name 
                FROM Tag 
                WHERE Name LIKE @Query 
                ORDER BY Name";
            using (var connection = new SqlConnection(ConfigurationHelper.GetConnectionString()))
            {
                return await connection.QueryAsync<TagModel>(sql, new { Query = $"%{query}%" });
            }
        }

        public async Task<int?> AddNewTagAsync(string tagName)
        {
            try
            {
                string connectionString = ConfigurationHelper.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    // Перевірка, чи існує тег
                    string checkTagSql = "SELECT ID FROM Tag WHERE Name = @Name";
                    int? tagId = await connection.QueryFirstOrDefaultAsync<int?>(checkTagSql, new { Name = tagName });

                    // Якщо тег не існує, додаємо його
                    if (tagId == null)
                    {
                        string insertTagSql = "INSERT INTO Tag (Name) VALUES (@Name); SELECT CAST(SCOPE_IDENTITY() as int)";
                        tagId = await connection.QuerySingleAsync<int>(insertTagSql, new { Name = tagName });
                    }

                    return tagId;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while getting all tags: " + ex.Message);
                return null;
            }
        }

        public async Task<TagModel> GetTagsByIdAsync(int tagId)
        {
            try
            {
                string connectionString = ConfigurationHelper.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT ID, Name FROM Tag WHERE ID = @TagID";
                    return await connection.QueryFirstOrDefaultAsync<TagModel>(query, new { TagID = tagId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while getting tag by id: " + ex.Message);
                return null;
            }
        }

        public async Task<IEnumerable<TagModel>> GetTagsBySnippetIdAsync(int snippetId)
        {
            try
            {
                string connectionString = ConfigurationHelper.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT Tag.ID, Tag.Name 
                        FROM SnippetTag
                        INNER JOIN Tag ON SnippetTag.TagID = Tag.ID
                        WHERE SnippetID = @CurrentSnippetID;
                    ";
                    var tags = await connection.QueryAsync<TagModel>(query, new { CurrentSnippetID = snippetId });
                    return tags;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while getting tags by snippet id: " + ex.Message);
                return null;
            }
        }

        public async Task AddSnippetTagsAsync(int snippetId, IEnumerable<TagModel> tags, SqlConnection connection, SqlTransaction transaction)
        {
            foreach (var tag in tags)
            {
                // Перевірка, чи існує тег
                string checkTagSql = "SELECT ID FROM Tag WHERE Name = @Name";
                int? tagId = await connection.QueryFirstOrDefaultAsync<int?>(checkTagSql, new { Name = tag.Name }, transaction);

                // Якщо тег не існує, додаємо його
                if (tagId == null)
                {
                    string insertTagSql = "INSERT INTO Tag (Name) VALUES (@Name); SELECT CAST(SCOPE_IDENTITY() as int)";
                    tagId = await connection.QuerySingleAsync<int>(insertTagSql, new { Name = tag.Name }, transaction);
                }

                // Додавання зв’язку між сніпетом і тегом
                string insertSnippetTagSql = "INSERT INTO SnippetTag (SnippetID, TagID) VALUES (@SnippetID, @TagID)";
                await connection.ExecuteAsync(insertSnippetTagSql, new { SnippetID = snippetId, TagID = tagId }, transaction);
            }
        }


        private async Task<bool> AreTagsChangedAsync(int snippetId, IEnumerable<TagModel> newTags)
        {
            var existingTags = await GetTagsBySnippetIdAsync(snippetId);
            var existingTagIds = new HashSet<int>(existingTags.Select(t => t.ID));
            var newTagIds = new HashSet<int>(newTags.Select(t => t.ID));

            return !existingTagIds.SetEquals(newTagIds);
        }

        public async Task<bool> UpdateSnippetTagsAsync(int snippetId, IEnumerable<TagModel> tags, SqlConnection connection, SqlTransaction transaction, bool areCategoriesChanged)
        {
            // Перевірка та оновлення тегів, якщо необхідно
            if (await AreTagsChangedAsync(snippetId, tags) || areCategoriesChanged)
            {
                // Видалення старих зв'язків
                string deleteTagsSql = "DELETE FROM SnippetTag WHERE SnippetID = @SnippetID";
                await connection.ExecuteAsync(deleteTagsSql, new { SnippetID = snippetId }, transaction);

                // Додавання нових зв'язків
                await AddSnippetTagsAsync(snippetId, tags, connection, transaction);
                return true;
            }
            else
            {
                return false;
            }
        }


    }
}
