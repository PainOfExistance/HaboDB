using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace HaboDB
{
    internal class UserList
    {
        private string filePath = "users.json";
        private Dictionary<string, User> users = new Dictionary<string, User>();

        public UserList()
        {
            LoadUsersFromJson();
        }

        public bool CheckCredentials(string userName, string password)
        {
            if (users.TryGetValue(userName, out User user))
            {
                return VerifyPassword(password, user.PasswordHash);
            }
            return false;
        }

        public bool CheckHash(string hash)
        {
            if (users.TryGetValue(hash, out User user))
            {
                return true;
            }
            return false;
        }

        public string AddUser(User user)
        {
            if (!users.ContainsKey(user.UserName))
            {
                users.Add(user.UserName, user);
                SaveUsersToJson();
                return user.UserName;
            }
            else
            {
                return "User already exists";
            }
        }

        public string RemoveUser(String UserName)
        {
            if (!users.ContainsKey(UserName))
            {
                users.Remove(UserName);
                SaveUsersToJson();
                return UserName;
            }
            else
            {
                return "User doesn't exists";
            }
        }

        private void SaveUsersToJson()
        {
            try
            {
                string json = JsonConvert.SerializeObject(users.Values, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void LoadUsersFromJson()
        {
            try
            {

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var userList = JsonConvert.DeserializeObject<List<User>>(json);
                    foreach (var user in userList)
                    {
                        users.Add(user.UserName, user);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private static bool VerifyPassword(string password, string passwordHash)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] hashBytes = Encoding.UTF8.GetBytes(passwordHash);
                byte[] inputBytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                for (int i = 0; i < hashBytes.Length; i++)
                {
                    if (hashBytes[i] != inputBytes[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public User GetUser(string username)
        {
            return users[username];
        }
    }

    internal class User
    {
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string url { get; set; }
        public string hash { get; set; }

        public User(string userName, string password, string name, string surname)
        {
            this.UserName = userName;
            this.PasswordHash = HashPassword(password);
            this.Name = name;
            this.Surname = surname;
        }

        public User()
        {
        }

        private static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
