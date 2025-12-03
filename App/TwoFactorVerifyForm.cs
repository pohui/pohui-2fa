using System;
using System.Drawing;
using System.Windows.Forms;
using WinFormsApp1.Services;

namespace WinFormsApp1
{
    public class TwoFactorVerifyForm : Form
    {
        private readonly TextBox _txtCode;
        private readonly Button _btnVerify;
        private readonly Label _lblStatus;
        private readonly AuthService _authService;
        private readonly string _base32Secret;

        public TwoFactorVerifyForm(AuthService authService, string base32Secret)
        {
            _authService = authService;
            _base32Secret = base32Secret;

            Text = "Enter 2FA Code";
            ClientSize = new Size(320, 140);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;

            var lbl = new Label { Text = "6-digit code:", Location = new Point(10, 20), AutoSize = true };
            _txtCode = new TextBox { Location = new Point(100, 18), Width = 180 };
            _btnVerify = new Button { Text = "Verify", Location = new Point(100, 60), Width = 100 };
            _btnVerify.Click += BtnVerify_Click;
            _lblStatus = new Label { Text = "", ForeColor = Color.Red, Location = new Point(10, 90), AutoSize = true };

            Controls.AddRange(new Control[] { lbl, _txtCode, _btnVerify, _lblStatus });
        }

        private void BtnVerify_Click(object? sender, EventArgs e)
        {
            _lblStatus.Text = string.Empty;
            var code = _txtCode.Text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                _lblStatus.Text = "Enter the 6-digit code.";
                return;
            }

            if (_authService.VerifyTotp(_base32Secret, code))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _lblStatus.Text = "Invalid code.";
            }
        }
    }
}
