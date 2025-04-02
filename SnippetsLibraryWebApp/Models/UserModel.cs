
using System;
using Microsoft.Data.SqlClient;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using Dapper;

namespace SnippetsLibraryWebApp.Models
{
    public class UserModel
    {
        public int Id { get; private set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; private set; }
    }
}