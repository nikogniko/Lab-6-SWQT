using Dapper;
using Microsoft.Data.SqlClient;
using SnippetsLibraryWebApp.Models;
using SnippetsLibraryWebApp.Utils;
using System.Collections.Generic;

namespace SnippetsLibraryWebApp.Repository
{
    public class CategoryRepository
    {
        public async Task<IEnumerable<CategoryModel>> GetAllCategoriesAsync()
        {
            try
            {
                string connectionString = ConfigurationHelper.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT ID, Name FROM Category";
                    return await connection.QueryAsync<CategoryModel>(query);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while getting all categories: " + ex.Message);
                return null;
            }
        }

        public async Task<CategoryModel> GetCategoryByIdAsync(int categoryId)
        {
            try
            {
                string connectionString = ConfigurationHelper.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT ID, Name FROM Category WHERE ID = @CategoryID";
                    return await connection.QueryFirstOrDefaultAsync<CategoryModel>(query, new { CategoryID = categoryId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while getting category by id: " + ex.Message);
                return null;
            }
        }

        public async Task<IEnumerable<CategoryModel>> GetCategoriesBySnippetIdAsync(int snippetId)
        {
            try
            {
                string connectionString = ConfigurationHelper.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT Category.ID, Category.Name 
                        FROM SnippetCategory
                        INNER JOIN Category ON SnippetCategory.CategoryID = Category.ID
                        WHERE SnippetID = @CurrentSnippetID;
                    ";
                    var categories = await connection.QueryAsync<CategoryModel>(query, new { CurrentSnippetID = snippetId });
                    return categories;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while getting categories by snippet ID: " + ex.Message);
                return null;
            }
        }

        private async Task<bool> AreCategoriesChangedAsync(int snippetId, IEnumerable<CategoryModel> newCategories)
        {
            var existingCategories = await GetCategoriesBySnippetIdAsync(snippetId);
            var existingCategoryIds = new HashSet<int>(existingCategories.Select(c => c.ID));
            var newCategoryIds = new HashSet<int>(newCategories.Select(c => c.ID));

            return !existingCategoryIds.SetEquals(newCategoryIds);
        }

        public async Task<bool> UpdateSnippetCategoriesAsync(int snippetId, IEnumerable<CategoryModel> categories,
            SqlConnection connection, SqlTransaction transaction, bool areCategoriesChanged)
        {
            // Перевірка та оновлення категорій, якщо необхідно
            if (await AreCategoriesChangedAsync(snippetId, categories) || areCategoriesChanged)
            {
                // Видалення старих зв'язків
                string deleteCategoriesSql = "DELETE FROM SnippetCategory WHERE SnippetID = @SnippetID";
                await connection.ExecuteAsync(deleteCategoriesSql, new { SnippetID = snippetId }, transaction);

                // Додавання нових зв'язків
                await AddSnippetCategoriesAsync(snippetId, categories, connection, transaction);
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task AddSnippetCategoriesAsync(int snippetId, IEnumerable<CategoryModel> categories, 
            SqlConnection connection, SqlTransaction transaction)
        {
            foreach (var category in categories)
            {
                string insertCategorySql = "INSERT INTO SnippetCategory (SnippetID, CategoryID) VALUES (@SnippetID, @CategoryID)";
                await connection.ExecuteAsync(insertCategorySql, new { SnippetID = snippetId, CategoryID = category.ID }, transaction);
            }
        }

    }
}
