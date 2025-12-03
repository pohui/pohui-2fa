using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using WinFormsApp1.Models;

namespace WinFormsApp1.Services
{
    public class UserStore
    {
        private readonly string _filePath;
        private readonly List<User> _users = new List<User>();
        private readonly object _lock = new object();

        public UserStore(string? filePath = null)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dataDir = Path.Combine(baseDir, "data");
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
            _filePath = filePath ?? Path.Combine(dataDir, "users.json");

            Load();
        }

        private void Load()
        {
            lock (_lock)
            {
                if (!File.Exists(_filePath))
                {
                    // create default demo user for convenience
                    var demo = new User
                    {
                        Username = "demo",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                        Is2FaEnabled = false,
                        TotpSecret = null,
                        CreatedAt = System.DateTime.UtcNow
                    };
                    _users.Clear();
                    _users.Add(demo);
                    Save();
                    return;
                }

                var json = File.ReadAllText(_filePath);
                try
                {
                    var users = JsonSerializer.Deserialize<List<User>>(json);
                    if (users != null)
                    {
                        _users.Clear();
                        _users.AddRange(users);
                    }
                }
                catch
                {
                    // ignore parse errors for demo
                }

                // if file exists but contains no users, create demo
                if (_users.Count == 0)
                {
                    var demo = new User
                    {
                        Username = "demo",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                        Is2FaEnabled = false,
                        TotpSecret = null,
                        CreatedAt = System.DateTime.UtcNow
                    };
                    _users.Add(demo);
                    Save();
                }
            }
        }

        private void Save()
        {
            lock (_lock)
            {
                var json = JsonSerializer.Serialize(_users, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
        }

        public User? GetByUsername(string username)
        {
            lock (_lock)
            {
                return _users.FirstOrDefault(u => u.Username.Equals(username, System.StringComparison.OrdinalIgnoreCase));
            }
        }

        public bool Add(User user)
        {
            lock (_lock)
            {
                if (_users.Any(u => u.Username.Equals(user.Username, System.StringComparison.OrdinalIgnoreCase))) return false;
                _users.Add(user);
                Save();
                return true;
            }
        }

        public bool Update(User user)
        {
            lock (_lock)
            {
                var existing = _users.FirstOrDefault(u => u.Username.Equals(user.Username, System.StringComparison.OrdinalIgnoreCase));
                if (existing == null) return false;
                existing.PasswordHash = user.PasswordHash;
                existing.TotpSecret = user.TotpSecret;
                existing.Is2FaEnabled = user.Is2FaEnabled;
                Save();
                return true;
            }
        }
    }
}
