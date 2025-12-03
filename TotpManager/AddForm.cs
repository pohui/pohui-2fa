using System;
using System.Drawing;
using System.Windows.Forms;

namespace TotpManager
{
    public class AddForm : Form
    {
        private readonly TextBox _txtName;
        private readonly TextBox _txtSecret;
        private readonly Button _btnAdd;

        public string? EntryName { get; private set; }
        public string? EntrySecret { get; private set; }

        public AddForm()
        {
            Text = "Add TOTP Entry";
            ClientSize = new Size(360, 150);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;

            var lblName = new Label { Text = "Name:", Location = new Point(10, 20), AutoSize = true };
            _txtName = new TextBox { Location = new Point(100, 18), Width = 240 };

            var lblSecret = new Label { Text = "Secret (Base32):", Location = new Point(10, 60), AutoSize = true };
            _txtSecret = new TextBox { Location = new Point(100, 58), Width = 240 };

            _btnAdd = new Button { Text = "Add", Location = new Point(100, 95), Width = 100 };
            _btnAdd.Click += BtnAdd_Click;

            Controls.AddRange(new Control[] { lblName, _txtName, lblSecret, _txtSecret, _btnAdd });
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            var name = _txtName.Text.Trim();
            var secret = _txtSecret.Text.Trim();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(secret))
            {
                MessageBox.Show(this, "Please enter name and secret.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            EntryName = name;
            EntrySecret = secret;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

