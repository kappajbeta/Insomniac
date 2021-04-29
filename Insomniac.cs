using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Insomniac.Properties;
using static Insomniac.UserInput;

namespace Insomniac
{
    public partial class Insomniac : Form
    {
        private long idleTime = 0;
        private int lastInput = -1;
        private Thread showIdleTimeThread;
        private Thread updateIdleTimeThread;
        private bool killThreads = false;
        private DateTime inputDate = DateTime.Now.AddDays(-1);

        public Insomniac(string[] args)
        {
            InitializeComponent();

            if ((args.Length > 0) &&
                (args[0] == "/i"))
            {
                notifyIcon.Visible = false;
            }

            Initialize();
        }

        public void Initialize()
        {
            InitializeForm();
            StartMonitoring("Starting...");
        }

        private void contextMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if ((e.ClickedItem.Text != "Quit") &&
                (e.ClickedItem.Text != "Show Error Log"))
            {
                for (int i = 0; i < contextMenuStrip.Items.Count; i++)
                {
                    if (contextMenuStrip.Items[i].Text != e.ClickedItem.Text)
                    {
                        ToolStripMenuItem toolStripMenuItem = (ToolStripMenuItem)contextMenuStrip.Items[i];

                        toolStripMenuItem.Checked = false;
                    }
                }
            }

            if (e.ClickedItem.Text == "Quit")
            {
                Settings.Default.Save();
                DisableMonitoring("Quitting...");
                Application.Exit();
            }
            else if ((e.ClickedItem.Text == "Disable") &&
                (e.ClickedItem.Selected == true))
            {
                Settings.Default.Save();
                DisableMonitoring("Disabling...");
            }
            else if ((e.ClickedItem.Text == "Enable") &&
                (e.ClickedItem.Selected == true))
            {
                StartMonitoring("Enabling...");
            }
            else if (e.ClickedItem.Text == "Show Error Log")
            {
                MessageBox.Show(Settings.Default.ErrorLog, "Insomniac - Error Log");
            }
        }

        public void InitializeForm()
        {
            Rectangle workingArea = Screen.GetWorkingArea(this);

            this.Tag = false;
            this.Text = "Insomniac - Idle Time";
            this.TopLevel = true;
            this.Location = new Point(workingArea.Right - Size.Width, workingArea.Bottom - Size.Height);

            lblInsomniac.Text = "00:00:00";
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            Settings.Default.ErrorLog = string.Empty;

            Settings.Default.Save();
            WriteStatusLog(string.Empty, false);
        }

        public void StartMonitoring(string messageText)
        {
            killThreads = false;
            lblInsomniac.Text = messageText;
            this.Text = "Insomniac";

            ShowWindow(this);
            Application.DoEvents();
            StartShowIdleTimeThread();
            StartUpdateIdleTimeThread();
            Thread.Sleep(3000);

            lblInsomniac.Text = "00:00:00";
            this.Text = "Insomniac - Idle Time";
            Process.GetCurrentProcess().PriorityBoostEnabled = false;
            Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)1;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;
            Settings.Default.ErrorLog += DateTime.Now.ToString() + " " + messageText + "\r\n";
        }

        public void DisableMonitoring(string messageText)
        {
            killThreads = true;
            lblInsomniac.Text = messageText;
            this.Text = "Insomniac";

            ShowWindow(this);
            Application.DoEvents();

            while ((showIdleTimeThread.IsAlive == true) ||
                (updateIdleTimeThread.IsAlive == true))
            {
                Thread.Sleep(1000);
            }

            HideWindow(this);

            lblInsomniac.Text = "00:00:00";
            this.Text = "Insomniac - Idle Time";
            Settings.Default.ErrorLog += DateTime.Now.ToString() + " " + messageText + "\r\n";
        }

        public void StartShowIdleTimeThread()
        {
            ParameterizedThreadStart parametrizedThreadStart = new ParameterizedThreadStart(ShowIdleTime);

            showIdleTimeThread = new Thread(parametrizedThreadStart);
            showIdleTimeThread.IsBackground = true;
            showIdleTimeThread.Priority = ThreadPriority.Lowest;

            showIdleTimeThread.Start(this);
        }

        public void StartUpdateIdleTimeThread()
        {
            ParameterizedThreadStart parametrizedThreadStart = new ParameterizedThreadStart(UpdateIdleTime);

            updateIdleTimeThread = new Thread(parametrizedThreadStart);
            updateIdleTimeThread.IsBackground = true;
            updateIdleTimeThread.Priority = ThreadPriority.Lowest;

            updateIdleTimeThread.Start(this);
        }

        public void ShowWindow(Insomniac InsomniacForm)
        {
            if (notifyIcon.Visible == true)
            {
                if (InsomniacForm.InvokeRequired == true)
                {
                    InsomniacForm.Invoke(new Action<Form>((formInstance) =>
                    {
                        formInstance.ShowInTaskbar = true;

                        formInstance.Show();
                        formInstance.BringToFront();
                        formInstance.Focus();
                        formInstance.Activate();

                        formInstance.Tag = true;
                        formInstance.ShowInTaskbar = false;
                    }), InsomniacForm);
                }
                else
                {
                    InsomniacForm.ShowInTaskbar = true;

                    InsomniacForm.Show();
                    InsomniacForm.BringToFront();
                    InsomniacForm.Focus();
                    InsomniacForm.Activate();

                    InsomniacForm.Tag = true;
                    InsomniacForm.ShowInTaskbar = false;
                }
            }
        }

        public void HideWindow(Insomniac InsomniacForm)
        {
            if (InsomniacForm.InvokeRequired == true)
            {
                InsomniacForm.Invoke(new Action<Form>((formInstance) =>
                {
                    formInstance.Hide();

                    formInstance.Tag = false;
                }), InsomniacForm);
            }
            else
            {
                InsomniacForm.Hide();

                InsomniacForm.Tag = false;
            }
        }

        public void FadeIn(Control formControl)
        {
            Insomniac InsomniacForm = (Insomniac)formControl;

            while (InsomniacForm.Opacity < 1)
            {
                InsomniacForm.Opacity = InsomniacForm.Opacity + 0.01;

                Thread.Sleep(5);
            }
        }

        public void FadeOut(Control formControl)
        {
            Insomniac InsomniacForm = (Insomniac)formControl;

            while (InsomniacForm.Opacity > 0)
            {
                InsomniacForm.Opacity = InsomniacForm.Opacity - 0.01;

                Thread.Sleep(5);
            }
        }

        public void UpdateLabel(Label InsomniacLabel, string labelText)
        {
            if (InsomniacLabel.InvokeRequired == true)
            {
                InsomniacLabel.Invoke(new Action<Label, string>((labelInstance, textInstance) =>
                {
                    labelInstance.Text = textInstance;
                }), InsomniacLabel, labelText);
            }
            else
            {
                InsomniacLabel.Text = labelText;
            }
        }

        public void WriteStatusLog(string logText, bool appendText = true)
        {
            if (File.Exists(Settings.Default.StatusLogPath))
            {
                if (appendText == true)
                {
                    File.AppendAllText(Settings.Default.StatusLogPath, logText);
                }
                else
                {
                    File.WriteAllText(Settings.Default.StatusLogPath, logText);
                }
            }
        }

        public void SendInput()
        {
            INPUT[] Inputs = new INPUT[4];
            INPUT Input = new INPUT();

            if ((lastInput == -1) ||
                (lastInput == 3))
            {
                Input.type = 1; // 1 = Keyboard Input
                Input.U.ki.wScan = ScanCodeShort.F1;
                Input.U.ki.dwFlags = KEYEVENTF.SCANCODE;
                Inputs[0] = Input;

                lastInput = 0;
            }

            if (lastInput == 0)
            {
                Input.type = 1; // 1 = Keyboard Input
                Input.U.ki.wScan = ScanCodeShort.F2;
                Input.U.ki.dwFlags = KEYEVENTF.SCANCODE;
                Inputs[1] = Input;

                lastInput = 1;
            }

            if (lastInput == 1)
            {
                Input.type = 1; // 1 = Keyboard Input
                Input.U.ki.wScan = ScanCodeShort.F3;
                Input.U.ki.dwFlags = KEYEVENTF.SCANCODE;
                Inputs[2] = Input;

                lastInput = 2;
            }

            if (lastInput == 2)
            {
                Input.type = 1; // 1 = Keyboard Input
                Input.U.ki.wScan = ScanCodeShort.F4;
                Input.U.ki.dwFlags = KEYEVENTF.SCANCODE;
                Inputs[3] = Input;

                lastInput = 3;
            }

            UserInput.SendInput(1, Inputs, INPUT.Size);

            inputDate = DateTime.Now;
        }

        public void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                DisableMonitoring("Disabling...");
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                StartMonitoring("Starting...");
            }
        }

        public void ShowIdleTime(object formObject)
        {
            try
            {
                Insomniac InsomniacForm = (Insomniac)formObject;

                while (killThreads == false)
                {
                    if ((idleTime >= Settings.Default.IdleTimeThreshold) &&
                        ((bool)InsomniacForm.Tag == false))
                    {
                        ShowWindow(InsomniacForm);
                        SendInput();
                        Thread.Sleep(Settings.Default.InputFrequency);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteStatusLog(DateTime.Now.ToString() + " Error occured in ShowIdleTime\r\n");

                if ((ex.InnerException is ThreadInterruptedException) == false)
                {
                    Settings.Default.ErrorLog += ex.ToString();
                }
            }
        }

        public void UpdateIdleTime(object formObject)
        {
            try
            {
                int focusInterval = 0;
                bool inIdleState = false;
                DateTime idleStartTimestamp = DateTime.Now;
                Insomniac InsomniacForm = (Insomniac)formObject;
                Label InsomniacLabel = (Label)InsomniacForm.Controls["lblInsomniac"];

                while (killThreads == false)
                {
                    if ((idleTime < Settings.Default.IdleTimeThreshold) &&
                        (DateTime.Now.AddSeconds(-idleTime - Settings.Default.IdleTimeError) > inputDate) &&
                        ((bool)InsomniacForm.Tag == true))
                    {
                        HideWindow(InsomniacForm);
                    }
                    else
                    {
                        if ((bool)InsomniacForm.Tag == true)
                        {
                            if (inIdleState == false)
                            {
                                inIdleState = true;
                                idleStartTimestamp = DateTime.Now;
                                Settings.Default.ErrorLog += DateTime.Now.ToString() + " Idle time started...\r\n";

                                WriteStatusLog(DateTime.Now.ToString() + " Idle time started...\r\n");
                            }

                            focusInterval++;

                            if (focusInterval == Settings.Default.FocusInterval)
                            {
                                focusInterval = 0;

                                ShowWindow((Insomniac)InsomniacLabel.FindForm());
                            }

                            UpdateLabel(InsomniacLabel, (DateTime.Now.Subtract(idleStartTimestamp)).ToString(@"hh\:mm\:ss"));
                        }
                        else if (inIdleState == true)
                        {
                            focusInterval = 0;
                            inIdleState = false;
                            Settings.Default.ErrorLog += DateTime.Now.ToString() + " Idle time ended...\r\n";

                            UpdateLabel(InsomniacLabel, "00:00:00");
                            WriteStatusLog(DateTime.Now.ToString() + " Idle time ended...\r\n");
                        }
                    }

                    idleTime = IdleTimeFinder.GetIdleTime();

                    Thread.Sleep(Settings.Default.HideWindowIdleTimeFrequency);
                }
            }
            catch (Exception ex)
            {
                WriteStatusLog(DateTime.Now.ToString() + " Error occured in UpdateIdleTime\r\n");

                if ((ex.InnerException is ThreadInterruptedException) == false)
                {
                    Settings.Default.ErrorLog += ex.ToString();
                }
            }
        }
    }
}