using System;
using System.Drawing;
using System.Windows.Forms;
using WinFormsApp1.Services;

namespace WinFormsApp1
{
    public class LoginForm : Form
    {
        private readonly TextBox _txtUsername;
        private readonly TextBox _txtPassword;
        private readonly Button _btnLogin;
        private readonly LinkLabel _lnkRegister;
        private readonly Label _lblStatus;

        private readonly UserStore _userStore;
        private readonly AuthService _authService;

        public string? AuthenticatedUsername { get; private set; }

        public LoginForm()
        {
            Text = "Login";
            ClientSize = new Size(320, 200);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            _userStore = new UserStore();
            _authService = new AuthService(_userStore);

            var lblUser = new Label { Text = "Username:", Location = new Point(10, 20), AutoSize = true };
            _txtUsername = new TextBox { Location = new Point(100, 18), Width = 200 };

            var lblPass = new Label { Text = "Password:", Location = new Point(10, 60), AutoSize = true };
            _txtPassword = new TextBox { Location = new Point(100, 58), Width = 200, UseSystemPasswordChar = true };

            _btnLogin = new Button { Text = "Login", Location = new Point(100, 100), Width = 90 };
            _btnLogin.Click += BtnLogin_Click;

            _lnkRegister = new LinkLabel { Text = "Register", Location = new Point(210, 105), AutoSize = true };
            _lnkRegister.Click += LnkRegister_Click;

            _lblStatus = new Label { Text = "", Location = new Point(10, 140), ForeColor = Color.Red, AutoSize = true };

            Controls.AddRange(new Control[] { lblUser, _txtUsername, lblPass, _txtPassword, _btnLogin, _lnkRegister, _lblStatus });
        }

        private void LnkRegister_Click(object? sender, EventArgs e)
        {
            using (var reg = new RegisterForm(_userStore))
            {
                reg.ShowDialog();
            }
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            _lblStatus.Text = string.Empty;
            var username = _txtUsername.Text.Trim();
            var password = _txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _lblStatus.Text = "Please enter username and password.";
                return;
            }

            if (!_authService.VerifyPassword(username, password))
            {
                _lblStatus.Text = "Invalid username or password.";
                return;
            }

            var user = _userStore.GetByUsername(username);
            if (user != null && user.Is2FaEnabled && !string.IsNullOrEmpty(user.TotpSecret))
            {
                using (var v = new TwoFactorVerifyForm(_authService, user.TotpSecret))
                {
                    if (v.ShowDialog() == DialogResult.OK)
                    {
                        AuthenticatedUsername = username;
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        _lblStatus.Text = "Invalid 2FA code.";
                    }
                }
            }
            else
            {
                AuthenticatedUsername = username;
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
