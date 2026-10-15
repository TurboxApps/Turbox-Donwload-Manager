using System;
using System.Drawing;
using System.Windows.Forms;

namespace IDMClone
{
    public class SchedulerDialog : Form
    {
        public bool IsSchedulerEnabled { get { return chkEnableSchedule.Checked; } }
        public DateTime ScheduledStartTime { get { return dtpStartTime.Value; } }
        public bool AutoShutdown { get { return chkShutdown.Checked; } }
        public bool AutoExit { get { return chkExit.Checked; } }

        private CheckBox chkEnableSchedule;
        private DateTimePicker dtpStartTime;
        private CheckBox chkShutdown;
        private CheckBox chkExit;
        private NumericUpDown numMaxDownloads;
        private Button btnApply;
        private Button btnClose;

        public SchedulerDialog()
        {
            InitializeComponent();
            ThemeManager.ApplyToControl(this);
            ThemeManager.ThemeChanged += (s, e) => ThemeManager.ApplyToControl(this);
        }

        private void InitializeComponent()
        {
            this.Text = "Scheduler - IDM Turbo Downloader";
            this.Size = new Size(480, 400);
            this.MinimumSize = new Size(460, 360);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = SystemColors.Control;
            this.Font = new Font("Segoe UI", 9F);

            TabControl tabCtrl = new TabControl
            {
                Location = new Point(15, 15),
                Size = new Size(435, 275),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(tabCtrl);

            // Tab 1: Schedule
            TabPage tabSchedule = new TabPage("Schedule Settings");
            tabCtrl.TabPages.Add(tabSchedule);

            chkEnableSchedule = new CheckBox
            {
                Text = "Enable Scheduled Downloading",
                Location = new Point(15, 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            tabSchedule.Controls.Add(chkEnableSchedule);

            Label lblStart = new Label
            {
                Text = "Start downloading at:",
                Location = new Point(15, 55),
                AutoSize = true
            };
            tabSchedule.Controls.Add(lblStart);

            dtpStartTime = new DateTimePicker
            {
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Location = new Point(170, 52),
                Size = new Size(110, 24),
                Value = DateTime.Now.AddMinutes(5)
            };
            tabSchedule.Controls.Add(dtpStartTime);

            Label lblMax = new Label
            {
                Text = "Max simultaneous downloads:",
                Location = new Point(15, 95),
                AutoSize = true
            };
            tabSchedule.Controls.Add(lblMax);

            numMaxDownloads = new NumericUpDown
            {
                Location = new Point(200, 93),
                Size = new Size(60, 24),
                Minimum = 1,
                Maximum = 10,
                Value = 2
            };
            tabSchedule.Controls.Add(numMaxDownloads);

            // Tab 2: Automation Actions
            TabPage tabActions = new TabPage("Post-Download Actions");
            tabCtrl.TabPages.Add(tabActions);

            chkShutdown = new CheckBox
            {
                Text = "Turn off computer when download finishes",
                Location = new Point(15, 20),
                AutoSize = true
            };
            tabActions.Controls.Add(chkShutdown);

            chkExit = new CheckBox
            {
                Text = "Exit IDM Turbo Downloader when all files finish",
                Location = new Point(15, 55),
                AutoSize = true
            };
            tabActions.Controls.Add(chkExit);

            CheckBox chkSound = new CheckBox
            {
                Text = "Play notification sound when download finishes",
                Location = new Point(15, 90),
                AutoSize = true,
                Checked = true
            };
            tabActions.Controls.Add(chkSound);

            // Bottom Buttons
            btnApply = new Button
            {
                Text = "Apply Settings",
                Location = new Point(235, 310),
                Size = new Size(115, 32),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            this.Controls.Add(btnApply);

            btnClose = new Button
            {
                Text = "Close",
                Location = new Point(360, 310),
                Size = new Size(90, 32),
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            this.Controls.Add(btnClose);

            this.AcceptButton = btnApply;
            this.CancelButton = btnClose;
        }
    }
}
