using System;
using OtpNet;
using BCrypt.Net;
using WinFormsApp1.Models;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using QRCoder;

namespace WinFormsApp1.Services
{
    public class AuthService
    {
        private readonly UserStore _store;

        public AuthService(UserStore store)
        {
            _store = store;
        }

        public bool Register(string username, string password, bool enable2Fa, out string? totpSecret, out string? error)
        {
            totpSecret = null;
            error = null;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                error = "Username and password are required.";
                return false;
            }

            if (_store.GetByUsername(username) != null)
            {
                error = "Username already exists.";
                return false;
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User
            {
                Username = username,
                PasswordHash = hash,
                Is2FaEnabled = false
            };

            if (!_store.Add(user))
            {
                error = "Could not create user.";
                return false;
            }

            if (enable2Fa)
            {
                // generate TOTP secret but do not enable until verified
                var key = KeyGeneration.GenerateRandomKey(20);
                totpSecret = Base32Encoding.ToString(key);
            }

            return true;
        }

        public bool VerifyPassword(string username, string password)
        {
            var user = _store.GetByUsername(username);
            if (user == null) return false;
            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        public string GenerateNewTotpSecret()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(key);
        }

        // Create an otpauth provisioning URI suitable for authenticator apps.
        // Format: otpauth://totp/{issuer}:{username}?secret=...&issuer=...
        public string CreateTotpProvisioningUri(string username, string issuer, string base32Secret)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("username required", nameof(username));
            if (string.IsNullOrWhiteSpace(issuer)) throw new ArgumentException("issuer required", nameof(issuer));
            if (string.IsNullOrWhiteSpace(base32Secret)) throw new ArgumentException("secret required", nameof(base32Secret));

            var label = Uri.EscapeDataString($"{issuer}:{username}");
            var issuerParam = Uri.EscapeDataString(issuer);
            return $"otpauth://totp/{label}?secret={base32Secret}&issuer={issuerParam}&algorithm=SHA1&digits=6&period=30";
        }

        // Generate PNG bytes for the QR code and ensure quiet zones (margins) are drawn to prevent cropping.
        // Requires QRCoder + System.Drawing.Common NuGet packages.
        public byte[] GenerateTotpQrCodePng(string provisioningUri, int pixelsPerModule = 20, bool drawQuietZones = true)
        {
            if (string.IsNullOrWhiteSpace(provisioningUri)) throw new ArgumentException("provisioningUri required", nameof(provisioningUri));

            using (var generator = new QRCodeGenerator())
            using (var qrData = generator.CreateQrCode(provisioningUri, QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCode(qrData))
            using (var bitmap = qrCode.GetGraphic(pixelsPerModule, Color.Black, Color.White, drawQuietZones))
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        public bool EnableTotpForUser(string username, string base32Secret)
        {
            var user = _store.GetByUsername(username);
            if (user == null) return false;
            user.TotpSecret = base32Secret;
            user.Is2FaEnabled = true;
            return _store.Update(user);
        }

        public bool VerifyTotp(string base32Secret, string code, int allowedDriftSteps = 1)
        {
            if (string.IsNullOrWhiteSpace(base32Secret) || string.IsNullOrWhiteSpace(code)) return false;
            try
            {
                var secretBytes = Base32Encoding.ToBytes(base32Secret);
                var totp = new Totp(secretBytes);
                return totp.VerifyTotp(code, out long _, new VerificationWindow(allowedDriftSteps, allowedDriftSteps));
            }
            catch
            {
                return false;
            }
        }
    }
}
