using System;
using System.Drawing;
using System.Windows.Forms;
using WinFormsApp1.Services;

namespace WinFormsApp1
{
    public class RegisterForm : Form
    {
        private readonly TextBox _txtUsername;
        private readonly TextBox _txtPassword;
        private readonly TextBox _txtConfirm;
        private readonly CheckBox _chkEnable2Fa;
        private readonly Button _btnRegister;
        private readonly Label _lblStatus;

        private readonly UserStore _userStore;
        private readonly AuthService _authService;

        public RegisterForm(UserStore store)
        {
            _userStore = store;
            _authService = new AuthService(_userStore);

            Text = "Register";
            ClientSize = new Size(380, 240);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            var lblUser = new Label { Text = "Username:", Location = new Point(10, 20), AutoSize = true };
            _txtUsername = new TextBox { Location = new Point(120, 18), Width = 240 };

            var lblPass = new Label { Text = "Password:", Location = new Point(10, 60), AutoSize = true };
            _txtPassword = new TextBox { Location = new Point(120, 58), Width = 240, UseSystemPasswordChar = true };

            var lblConfirm = new Label { Text = "Confirm:", Location = new Point(10, 100), AutoSize = true };
            _txtConfirm = new TextBox { Location = new Point(120, 98), Width = 240, UseSystemPasswordChar = true };

            _chkEnable2Fa = new CheckBox { Text = "Enable 2FA (TOTP)", Location = new Point(120, 140), AutoSize = true };

            _btnRegister = new Button { Text = "Register", Location = new Point(120, 170), Width = 100 };
            _btnRegister.Click += BtnRegister_Click;

            _lblStatus = new Label { Text = "", Location = new Point(10, 205), ForeColor = Color.Red, AutoSize = true };

            Controls.AddRange(new Control[] { lblUser, _txtUsername, lblPass, _txtPassword, lblConfirm, _txtConfirm, _chkEnable2Fa, _btnRegister, _lblStatus });
        }

        private void BtnRegister_Click(object? sender, EventArgs e)
        {
            _lblStatus.Text = string.Empty;
            var username = _txtUsername.Text.Trim();
            var password = _txtPassword.Text;
            var confirm = _txtConfirm.Text;
            var enable2Fa = _chkEnable2Fa.Checked;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _lblStatus.Text = "Please enter username and password.";
                return;
            }

            if (password != confirm)
            {
                _lblStatus.Text = "Passwords do not match.";
                return;
            }

            if (!_authService.Register(username, password, enable2Fa, out var totpSecret, out var error))
            {
                _lblStatus.Text = error ?? "Could not register.";
                return;
            }

            if (enable2Fa && !string.IsNullOrEmpty(totpSecret))
            {
                using (var setup = new TwoFactorSetupForm(_authService, username, totpSecret))
                {
                    if (setup.ShowDialog() == DialogResult.OK)
                    {
                        MessageBox.Show(this, "Registration complete and 2FA enabled.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show(this, "2FA setup was cancelled. User created without 2FA.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                }
            }
            else
            {
                MessageBox.Show(this, "Registration complete.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
