using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System;
using OtpNet;

namespace TotpManager
{
    public class TotpEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
    }

    public class TotpStore
    {
        private readonly string _filePath;
        private readonly List<TotpEntry> _entries = new List<TotpEntry>();

        public TotpStore(string? filePath = null)
        {
            var baseDir = AppContext.BaseDirectory;
            var dataDir = Path.Combine(baseDir, "data");
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
            _filePath = filePath ?? Path.Combine(dataDir, "secrets.json");

            Load();
        }

        private void Load()
        {
            if (!File.Exists(_filePath)) return;
            try
            {
                var json = File.ReadAllText(_filePath);
                var arr = JsonSerializer.Deserialize<List<TotpEntry>>(json);
                if (arr != null) _entries.AddRange(arr);
            }
            catch
            {
                // ignore
            }
        }

        private void Save()
        {
            var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public IReadOnlyList<TotpEntry> Entries => _entries.AsReadOnly();

        public void Add(TotpEntry e)
        {
            _entries.Add(e);
            Save();
        }

        public void Remove(TotpEntry e)
        {
            _entries.Remove(e);
            Save();
        }

        // Compute the current TOTP code for an entry. Returns numeric code as string.
        public string ComputeTotp(TotpEntry entry, bool forceRecompute = false)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrWhiteSpace(entry.Secret)) throw new ArgumentException("Missing secret.", nameof(entry));

            try
            {
                var secretBytes = Base32Encoding.ToBytes(entry.Secret);
                var totp = new Totp(secretBytes);
                return totp.ComputeTotp();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to compute TOTP.", ex);
            }
        }
    }
}
