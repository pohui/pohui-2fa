# pohui-2fa

A small Windows desktop sample that demonstrates managing Time-based One-Time Passwords (TOTP / 2FA) alongside a simple WinForms UI.

This repository contains two related projects in a single solution:

- App/ — the main WinForms sample application that provides user registration, login, and 2FA setup/verification flows.
- TotpManager/ — a helper project for generating and managing TOTP entries; can be used as a library or as a small WinForms utility.

Quick overview
--------------
This solution is intended as a compact example showing:
- How to generate and display a QR code for TOTP setup (QRCoder is included in the release build).
- How to generate and verify TOTP codes using Otp.NET.
- A simple sample user store (file-based or in-memory) and authentication flow wired to the WinForms UI.

Project structure
-----------------
- `App/` — main application
  - Forms: `Form1.cs`, `LoginForm.cs`, `RegisterForm.cs`, `TwoFactorSetupForm.cs`, `TwoFactorVerifyForm.cs`
  - `Services/` — `AuthService.cs`, `UserStore.cs` (sample user persistence and auth logic)
  - `Models/User.cs` — user model containing username/password and TOTP secret/flags
- `TotpManager/` — helper project for TOTP generation and management
  - `TotpManager.cs`, `TotpStore.cs` — TOTP logic and storage helpers
  - Optional UI: `AddForm.cs`, `MainForm.cs` if you open it as a WinForms app

Where user data is stored (sample)
----------------------------------
The sample app stores user registration and TOTP secret information via the sample `UserStore` implementation in `App/Services/UserStore.cs`.
By default this is a simple local store appropriate for samples and demos (in-memory or simple file-based persistence). Do not treat this as production-ready secret storage.

If you want production-safe storage, consider:
- Using DPAPI / Windows Data Protection APIs to encrypt secrets at rest
- Storing data in a secure database with encryption at rest and access controls

Build and run (PowerShell)
--------------------------
Open PowerShell in the repository root and run:

```powershell
# Restore packages
dotnet restore

# Build the whole solution in Release
dotnet build -c Release

# Run the App project (from repo root)
dotnet run --project App -c Release
```

Publish a single-file Windows EXE
--------------------------------
To publish a self-contained single-file EXE for Windows x64 (example):

```powershell
dotnet publish App -c Release -r win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=false
```

Notes:
- Keep `PublishTrimmed=false` initially; trimming can break reflection-based code (WinForms, some libraries).
- Use `-r win-x86` if you need 32-bit, or change `win-x64` to match your target RID.

Common issues & troubleshooting
-------------------------------
- NETSDK1136 (WinForms target error):
  If you see an error about the target platform for WinForms/WPF, ensure the WinForms project `.csproj` includes a Windows target framework and `UseWindowsForms`:

  ```xml
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
  </PropertyGroup>
  ```

- QR code cropping / scanning problems:
  - Generate QR images with a sufficient "pixels per module" and a quiet zone.
  - When showing in a `PictureBox`, prefer `SizeMode = PictureBoxSizeMode.Zoom`.
  - Verify the saved PNG looks correct in an external image viewer.

- OTPs don't match authenticator apps:
  - Ensure the secret is saved and encoded consistently (Base32 is common).
  - Check system time/clock skew between machines.

Development notes and suggestions
--------------------------------
- Totp generation: This project includes Otp.NET in the release output — use it to generate and verify OTP codes.
- Auto-refresh OTPs: Use a WinForms `Timer` to refresh displayed codes and the remaining seconds until the next code.
- Copying OTPs: Provide a "Copy" button that uses `Clipboard.SetText(code)` for user convenience.
- Secure storage: Replace the sample `UserStore` with an encrypted local store or a secure database for real deployments.

License
-------
Add a `LICENSE` file to choose and apply a license to this repository.

Next steps I can help with (pick any):
- Add a repository `.gitignore` tuned for .NET WinForms projects.
- Add a `LICENSE` file (MIT, Apache-2.0, etc.).
- Improve `TwoFactorVerifyForm.cs` flow to require an explicit choice when abandoning 2FA setup.
- Implement an encrypted local store for TOTP secrets (DPAPI or AES-encrypted file).
