using System;
using System.Drawing;
using System.Windows.Forms;
using QRCoder;
using WinFormsApp1.Services;

namespace WinFormsApp1
{
    public class TwoFactorSetupForm : Form
    {
        private readonly PictureBox _picQr;
        private readonly TextBox _txtManualCode;
        private readonly TextBox _txtVerifyCode;
        private readonly Button _btnVerify;
        private readonly Label _lblStatus;

        private readonly AuthService _authService;
        private readonly string _username;
        private readonly string _base32Secret;

        public TwoFactorSetupForm(AuthService authService, string username, string base32Secret)
        {
            _authService = authService;
            _username = username;
            _base32Secret = base32Secret;

            Text = "2FA Setup";
            ClientSize = new Size(420, 360);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;

            _picQr = new PictureBox { Location = new Point(10, 10), Size = new Size(200, 200), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom };
            _txtManualCode = new TextBox { Location = new Point(220, 10), Width = 180, ReadOnly = true, Text = _base32Secret };
            var lblScan = new Label { Text = "Scan this QR code with your authenticator app or enter the secret manually.", Location = new Point(10, 220), Size = new Size(390, 30) };

            var lblVerify = new Label { Text = "Enter code from app:", Location = new Point(10, 260), AutoSize = true };
            _txtVerifyCode = new TextBox { Location = new Point(140, 258), Width = 100 };
            _btnVerify = new Button { Text = "Verify & Enable", Location = new Point(260, 255), Width = 120 };
            _btnVerify.Click += BtnVerify_Click;

            _lblStatus = new Label { Text = "", ForeColor = Color.Red, Location = new Point(10, 300), AutoSize = true };

            Controls.AddRange(new Control[] { _picQr, _txtManualCode, lblScan, lblVerify, _txtVerifyCode, _btnVerify, _lblStatus });

            LoadQr();

            // Handle form closing to confirm user's intent
            this.FormClosing += TwoFactorSetupForm_FormClosing;
        }

        private void TwoFactorSetupForm_FormClosing(object? sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            // If the form is closing due to a user action (clicking the X) and setup wasn't completed (DialogResult != OK),
            // ask whether to finish registration without 2FA or stay on the page.
            if (e.CloseReason == System.Windows.Forms.CloseReason.UserClosing && this.DialogResult != System.Windows.Forms.DialogResult.OK)
            {
                var res = MessageBox.Show(this,
                    "Do you want to finish registration without enabling 2FA?\n\nYes - Create the user without 2FA and continue.\nNo - Stay on this page to finish 2FA setup.",
                    "Finish without 2FA?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (res == DialogResult.No)
                {
                    // Keep the form open so the user can continue setup
                    e.Cancel = true;
                }
                // If Yes, allow the close; RegisterForm will proceed treating setup as cancelled and user created without 2FA
            }
        }

        private void LoadQr()
        {
            try
            {
                var otpauth = $"otpauth://totp/WinFormsApp1:{Uri.EscapeDataString(_username)}?secret={_base32Secret}&issuer=WinFormsApp1";
                using (var qr = new QRCodeGenerator())
                {
                    var data = qr.CreateQrCode(otpauth, QRCodeGenerator.ECCLevel.Q);
                    using (var code = new QRCode(data))
                    {
                        // generate a larger, high-resolution QR and keep quiet zones so scanners can read it
                        var img = code.GetGraphic(20, Color.Black, Color.White, true);
                        var old = _picQr.Image;
                        _picQr.Image = img;
                        old?.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Failed to create QR: " + ex.Message;
            }
        }

        private void BtnVerify_Click(object? sender, EventArgs e)
        {
            _lblStatus.Text = string.Empty;
            var code = _txtVerifyCode.Text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                _lblStatus.Text = "Enter the 6-digit code from your app.";
                return;
            }

            if (_authService.VerifyTotp(_base32Secret, code))
            {
                if (_authService.EnableTotpForUser(_username, _base32Secret))
                {
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }
                else
                {
                    _lblStatus.Text = "Failed to enable 2FA for user.";
                    return;
                }
            }

            _lblStatus.Text = "Invalid code.";
        }
    }
}
