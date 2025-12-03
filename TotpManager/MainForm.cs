using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using OtpNet;
using System.Drawing.Drawing2D;

namespace TotpManager
{
    public class MainForm : Form
    {
        private readonly TotpStore _store;
        private readonly ListBox _lst;
        private readonly Button _btnAdd;
        private readonly Button _btnRemove;
        private readonly Button _btnCode;

        // New UI for code display
        private readonly Label _lblCode;
        private readonly Button _btnCopy;
        private readonly Label _lblRemaining;
        private readonly ProgressBar _progress;
        private readonly System.Windows.Forms.Timer _timer;
        private readonly PictureBox _picSpinner;
        private float _spinnerAngle = 0f; // degrees

        private int _lastCycleNumber = -1;

        public MainForm()
        {
            Text = "TotpManager";
            ClientSize = new Size(620, 420);
            StartPosition = FormStartPosition.CenterScreen;

            _store = new TotpStore();

            _lst = new ListBox { Location = new Point(10, 10), Size = new Size(360, 340) };
            _btnAdd = new Button { Text = "Add", Location = new Point(380, 10), Width = 100 };
            _btnRemove = new Button { Text = "Remove", Location = new Point(380, 50), Width = 100 };
            _btnCode = new Button { Text = "Generate Code", Location = new Point(380, 90), Width = 100 };

            // code display controls
            _lblCode = new Label { Text = "------", Font = new Font(FontFamily.GenericMonospace, 24, FontStyle.Bold), Location = new Point(380, 150), AutoSize = false, Size = new Size(220, 50), TextAlign = ContentAlignment.MiddleCenter, BorderStyle = BorderStyle.FixedSingle };
            _btnCopy = new Button { Text = "Copy", Location = new Point(510, 210), Width = 90 };
            _lblRemaining = new Label { Text = "Remaining: --s", Location = new Point(380, 210), AutoSize = true };
            _progress = new ProgressBar { Location = new Point(380, 240), Size = new Size(220, 18) };
            _picSpinner = new PictureBox { Location = new Point(380, 268), Size = new Size(24, 24), SizeMode = PictureBoxSizeMode.CenterImage, BackColor = Color.Transparent };

            _btnAdd.Click += BtnAdd_Click;
            _btnRemove.Click += BtnRemove_Click;
            _btnCode.Click += BtnCode_Click;
            _lst.SelectedIndexChanged += Lst_SelectedIndexChanged;
            _btnCopy.Click += BtnCopy_Click;

            Controls.AddRange(new Control[] { _lst, _btnAdd, _btnRemove, _btnCode, _lblCode, _btnCopy, _lblRemaining, _progress, _picSpinner });

            // timer for updating remaining seconds and code cycle
            _timer = new System.Windows.Forms.Timer { Interval = 500 }; // update twice a second for snappy UI
            _timer.Tick += Timer_Tick;
            _timer.Start();

            LoadEntries();
        }

        private void LoadEntries()
        {
            _lst.Items.Clear();
            foreach (var e in _store.Entries)
            {
                _lst.Items.Add(e.Name);
            }

            // auto-select first if available
            if (_lst.Items.Count > 0) _lst.SelectedIndex = 0;
        }

        private void Lst_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateCodeDisplay();
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            using (var f = new AddForm())
            {
                if (f.ShowDialog() == DialogResult.OK && f.EntryName != null && f.EntrySecret != null)
                {
                    _store.Add(new TotpEntry { Name = f.EntryName, Secret = f.EntrySecret });
                    LoadEntries();
                }
            }
        }

        private void BtnRemove_Click(object? sender, EventArgs e)
        {
            if (_lst.SelectedItem == null) return;
            var name = _lst.SelectedItem.ToString();
            var entry = _store.Entries.FirstOrDefault(x => x.Name == name);
            if (entry == null) return;
            _store.Remove(entry);
            LoadEntries();
        }

        private void BtnCode_Click(object? sender, EventArgs e)
        {
            UpdateCodeDisplay(forceRecompute: true);
        }

        private void BtnCopy_Click(object? sender, EventArgs e)
        {
            try
            {
                var txt = _lblCode.Text.Trim();
                if (!string.IsNullOrEmpty(txt) && txt != "------")
                {
                    Clipboard.SetText(txt);
                }
            }
            catch
            {
                // ignore clipboard errors
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateRemainingAndMaybeRecompute();
        }

        private void UpdateRemainingAndMaybeRecompute()
        {
            if (_lst.SelectedItem == null)
            {
                _lblRemaining.Text = "Remaining: --s";
                _progress.Value = 0;
                return;
            }

            var name = _lst.SelectedItem.ToString();
            var entry = _store.Entries.FirstOrDefault(x => x.Name == name);
            if (entry == null)
            {
                _lblRemaining.Text = "Remaining: --s";
                _progress.Value = 0;
                return;
            }

            try
            {
                // assume 30s step as default
                const int period = 30;
                var unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var secondsRemaining = (int)(period - (unix % period));
                _lblRemaining.Text = $"Remaining: {secondsRemaining}s";
                var percent = (int)((period - secondsRemaining) * 100 / (double)period);
                if (percent < 0) percent = 0;
                if (percent > 100) percent = 100;
                _progress.Value = percent;

                var currentCycle = (int)(unix / period);
                if (currentCycle != _lastCycleNumber)
                {
                    // new cycle -> recompute code
                    UpdateCodeDisplay();
                    _lastCycleNumber = currentCycle;
                }
                // advance spinner angle and redraw
                _spinnerAngle = (_spinnerAngle + 30f) % 360f; // rotate 30 degrees per tick (~60deg/s)
                DrawSpinner();
            }
            catch
            {
                _lblRemaining.Text = "Remaining: --s";
            }
        }

        private void DrawSpinner()
        {
            int size = Math.Max(16, Math.Min(32, _picSpinner.Width));
            var bmp = new Bitmap(size, size);
            try
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    int thickness = Math.Max(2, size / 10);
                    var rect = new Rectangle(thickness, thickness, size - thickness * 2, size - thickness * 2);
                    using (var pen = new Pen(Color.DodgerBlue, thickness))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        // draw a thick arc (270 degrees) rotated by spinner angle
                        float startAngle = -_spinnerAngle; // negative to rotate clockwise visually
                        float sweep = 270f;
                        g.DrawArc(pen, rect, startAngle, sweep);

                        // Draw a small highlight dot at the arc head
                        var rad = (rect.Width) / 2f;
                        var center = new PointF(rect.Left + rect.Width / 2f, rect.Top + rect.Height / 2f);
                        var angleRad = (startAngle + sweep) * (float)(Math.PI / 180.0);
                        var headX = center.X + (rad - thickness) * (float)Math.Cos(angleRad);
                        var headY = center.Y + (rad - thickness) * (float)Math.Sin(angleRad);
                        var dotSize = Math.Max(2, thickness);
                        using (var br = new SolidBrush(Color.White))
                        {
                            g.FillEllipse(br, headX - dotSize / 2f, headY - dotSize / 2f, dotSize, dotSize);
                        }
                    }
                }

                var old = _picSpinner.Image;
                _picSpinner.Image = bmp;
                // do not dispose bmp here because it's assigned to PictureBox; dispose old image
                old?.Dispose();
            }
            catch
            {
                bmp.Dispose();
            }
        }

        private void UpdateCodeDisplay(bool forceRecompute = false)
        {
            if (_lst.SelectedItem == null) return;

            var name = _lst.SelectedItem.ToString();
            var entry = _store.Entries.FirstOrDefault(x => x.Name == name);
            if (entry == null) return;

            try
            {
                var code = _store.ComputeTotp(entry, forceRecompute);
                _lblCode.Text = code;
            }
            catch
            {
                _lblCode.Text = "------";
            }
        }
    }
}

