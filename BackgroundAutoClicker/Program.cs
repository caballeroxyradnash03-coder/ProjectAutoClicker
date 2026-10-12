using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace BackgroundAutoClicker
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class RecordedFrame
    {
        public int ScreenX;
        public int ScreenY;
        public IntPtr TargetHwnd;
        public int ClientX;
        public int ClientY;
        public long TimestampMs;
        public bool IsLeftClick;
        public bool IsRightClick;
        public bool IsLeftRelease;
        public bool IsRightRelease;

        public override string ToString()
        {
            string act = "Move";
            if (IsLeftClick) act = "L-Click";
            else if (IsRightClick) act = "R-Click";
            else if (IsLeftRelease) act = "L-Up";
            else if (IsRightRelease) act = "R-Up";
            return string.Format("[{0,5}ms] Client:({1,4},{2,4}) Screen:({3,4},{4,4}) {5}",
                TimestampMs, ClientX, ClientY, ScreenX, ScreenY, act);
        }
    }

    public class MainForm : Form
    {
        #region Win32 API

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        private const uint INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_MOVE       = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN    = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP      = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN   = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP     = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN  = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP    = 0x0040;
        private const uint MOUSEEVENTF_ABSOLUTE    = 0x8000;

        private const uint WM_MOUSEMOVE   = 0x0200;
        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP   = 0x0202;
        private const uint WM_RBUTTONDOWN = 0x0204;
        private const uint WM_RBUTTONUP   = 0x0205;
        private const uint WM_MBUTTONDOWN = 0x0207;
        private const uint WM_MBUTTONUP   = 0x0208;
        private const uint WM_SETCURSOR   = 0x0020;

        private const int MK_LBUTTON = 0x0001;
        private const int MK_RBUTTON = 0x0002;
        private const int MK_MBUTTON = 0x0010;

        private const int VK_LBUTTON = 0x01;
        private const int VK_RBUTTON = 0x02;
        private const int VK_ESCAPE  = 0x1B;

        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_RECORD = 1;  // F8
        private const int HOTKEY_PLAY   = 2;  // F6
        private const int HOTKEY_CLEAR  = 3;  // F9

        private const uint GA_ROOT = 2;
        #endregion

        #region State
        private List<RecordedFrame> frames = new List<RecordedFrame>();
        private volatile bool isRecording = false;
        private volatile bool isPlaying = false;
        private Thread workerThread = null;
        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private long totalClicks = 0;
        private long loopsDone = 0;

        private IntPtr recordedMainHwnd = IntPtr.Zero;
        private string recordedWindowTitle = "None";

        private int screenW, screenH;
        private System.Windows.Forms.Timer uiTimer;
        #endregion

        #region Controls
        private Label lblStatus;
        private Label lblTargetInfo;
        private ListBox lstFrames;
        private Label lblStats;
        private Label lblPlayStats;
        private RadioButton rbBackgroundMode;
        private RadioButton rbPhysicalMode;
        private NumericUpDown numSpeed;
        private CheckBox chkAutoClick;
        private ComboBox cmbButton;
        private NumericUpDown numClickEvery;
        private CheckBox chkLoopForever;
        private NumericUpDown numLoops;
        private NumericUpDown numLoopDelay;
        private CheckBox chkSound;
        private CheckBox chkOnTop;
        private Button btnRecord;
        private Button btnPlay;
        private Button btnClear;
        #endregion

        public MainForm()
        {
            screenW = GetSystemMetrics(SM_CXSCREEN);
            screenH = GetSystemMetrics(SM_CYSCREEN);
            if (screenW <= 0) screenW = 1920;
            if (screenH <= 0) screenH = 1080;

            BuildUI();
            RegisterHotkeys();

            uiTimer = new System.Windows.Forms.Timer();
            uiTimer.Interval = 200;
            uiTimer.Tick += new EventHandler(UiTick);
            uiTimer.Start();
        }

        private void BuildUI()
        {
            this.Text = "Project ni Xyrad";
            this.Size = new Size(680, 810);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(24, 24, 37);
            this.ForeColor = Color.FromArgb(205, 214, 244);
            this.Font = new Font("Segoe UI", 9.5f);

            // Top Banner
            Panel top = new Panel();
            top.Dock = DockStyle.Top;
            top.Height = 68;
            top.BackColor = Color.FromArgb(30, 30, 46);
            this.Controls.Add(top);

            MakeLabel(top, "PROJECT NI XYRAD", 18, 8, new Font("Segoe UI", 14f, FontStyle.Bold), Color.FromArgb(137, 180, 250));
            MakeLabel(top, "Record mouse movement > Replay sa background > Pwede Alt+Tab & gamit mouse sa laing tab!",
                20, 38, new Font("Segoe UI", 8.5f), Color.FromArgb(166, 173, 200));

            // Status Bar
            Panel pnlS = new Panel();
            pnlS.Location = new Point(20, 78);
            pnlS.Size = new Size(625, 40);
            pnlS.BackColor = Color.FromArgb(30, 30, 46);
            this.Controls.Add(pnlS);

            lblStatus = new Label();
            lblStatus.Text = "[ READY ]  Pindota F8 para mag-record > Lihoka mouse > F8 stop > F6 play";
            lblStatus.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblStatus.ForeColor = Color.FromArgb(249, 226, 175);
            lblStatus.Location = new Point(10, 9);
            lblStatus.AutoSize = true;
            pnlS.Controls.Add(lblStatus);

            // Group 1: Playback Mode Selection
            GroupBox gMode = MakeGroup("1. Playback Mode (Pilia kung unsa imong gusto)", 20, 128, 625, 95);
            this.Controls.Add(gMode);

            rbBackgroundMode = new RadioButton();
            rbBackgroundMode.Text = "🌟 True Background Mode (PostMessage) - REKOMENDADO PARA SA ALT+TAB\r\n" +
                                   "    Dili gyud mo-lihok imong mouse cursor. Pwede kaayo mag-Alt+Tab ug mag-mouse sa lain!";
            rbBackgroundMode.Checked = true;
            rbBackgroundMode.Location = new Point(15, 20);
            rbBackgroundMode.Size = new Size(595, 38);
            rbBackgroundMode.ForeColor = Color.FromArgb(166, 227, 161);
            rbBackgroundMode.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            gMode.Controls.Add(rbBackgroundMode);

            rbPhysicalMode = new RadioButton();
            rbPhysicalMode.Text = "🎯 Hardware Cursor Mode (SendInput) - Mo-lihok ang physical cursor sa screen (Infinite Loop)";
            rbPhysicalMode.Location = new Point(15, 62);
            rbPhysicalMode.Size = new Size(595, 24);
            rbPhysicalMode.ForeColor = Color.FromArgb(249, 226, 175);
            rbPhysicalMode.Font = new Font("Segoe UI", 9f);
            gMode.Controls.Add(rbPhysicalMode);

            // Group 2: Target Window & Recording Preview
            GroupBox g2 = MakeGroup("2. Recorded Path Preview & Target Window", 20, 230, 625, 205);
            this.Controls.Add(g2);

            lblTargetInfo = new Label();
            lblTargetInfo.Text = "Target Window: (I-hover imong mouse sa target unya pindota F8)";
            lblTargetInfo.Font = new Font("Segoe UI", 8.5f, FontStyle.Italic);
            lblTargetInfo.ForeColor = Color.FromArgb(137, 180, 250);
            lblTargetInfo.Location = new Point(15, 22);
            lblTargetInfo.Size = new Size(590, 20);
            g2.Controls.Add(lblTargetInfo);

            lstFrames = new ListBox();
            lstFrames.Location = new Point(15, 46);
            lstFrames.Size = new Size(455, 115);
            lstFrames.BackColor = Color.FromArgb(17, 17, 27);
            lstFrames.ForeColor = Color.FromArgb(205, 214, 244);
            lstFrames.Font = new Font("Consolas", 8.5f);
            lstFrames.BorderStyle = BorderStyle.FixedSingle;
            g2.Controls.Add(lstFrames);

            btnRecord = MakeBtn("Start Record (F8)", 485, 46, 125, 36, Color.FromArgb(243, 139, 168), Color.FromArgb(24, 24, 37));
            btnRecord.Click += delegate { ToggleRecording(); };
            g2.Controls.Add(btnRecord);

            btnClear = MakeBtn("Clear (F9)", 485, 90, 125, 30, Color.FromArgb(69, 71, 90), Color.White);
            btnClear.Click += delegate { ClearAll(); };
            g2.Controls.Add(btnClear);

            lblStats = new Label();
            lblStats.Text = "Frames: 0 | Duration: 0.0s | Clicks: 0";
            lblStats.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblStats.ForeColor = Color.FromArgb(166, 227, 161);
            lblStats.Location = new Point(15, 170);
            lblStats.AutoSize = true;
            g2.Controls.Add(lblStats);

            // Group 3: Playback Settings
            GroupBox g3 = MakeGroup("3. Playback Settings", 20, 442, 625, 180);
            this.Controls.Add(g3);

            MakeLabel(g3, "Speed (%):", 15, 26, this.Font, this.ForeColor);
            numSpeed = MakeNumeric(g3, 95, 24, 65, 10, 1000, 100, 10);
            MakeLabel(g3, "(100 = real-time, 200 = 2x faster, 50 = half speed)", 170, 27,
                new Font("Segoe UI", 8f, FontStyle.Italic), Color.FromArgb(147, 153, 178));

            chkLoopForever = new CheckBox();
            chkLoopForever.Text = "Loop Forever (Walay undang hangtod pinduton F6)";
            chkLoopForever.Checked = true;
            chkLoopForever.Location = new Point(15, 60);
            chkLoopForever.AutoSize = true;
            chkLoopForever.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            chkLoopForever.ForeColor = Color.FromArgb(166, 227, 161);
            chkLoopForever.CheckedChanged += delegate { numLoops.Enabled = !chkLoopForever.Checked; };
            g3.Controls.Add(chkLoopForever);

            MakeLabel(g3, "Max Loops:", 390, 62, this.Font, this.ForeColor);
            numLoops = MakeNumeric(g3, 475, 60, 65, 1, 999999, 10, 1);
            numLoops.Enabled = false;

            MakeLabel(g3, "Delay between loops (ms):", 15, 96, this.Font, this.ForeColor);
            numLoopDelay = MakeNumeric(g3, 195, 94, 65, 0, 10000, 50, 50);

            chkAutoClick = new CheckBox();
            chkAutoClick.Text = "Auto-click every N frames:";
            chkAutoClick.Checked = false;
            chkAutoClick.Location = new Point(290, 96);
            chkAutoClick.AutoSize = true;
            g3.Controls.Add(chkAutoClick);

            numClickEvery = MakeNumeric(g3, 475, 94, 55, 1, 1000, 5, 1);

            cmbButton = new ComboBox();
            cmbButton.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbButton.Items.AddRange(new object[] { "Left", "Right" });
            cmbButton.SelectedIndex = 0;
            cmbButton.Location = new Point(540, 94);
            cmbButton.Size = new Size(70, 24);
            cmbButton.BackColor = Color.FromArgb(49, 50, 68);
            cmbButton.ForeColor = Color.White;
            g3.Controls.Add(cmbButton);

            chkSound = new CheckBox();
            chkSound.Text = "Audio Beeps";
            chkSound.Checked = true;
            chkSound.Location = new Point(15, 136);
            chkSound.AutoSize = true;
            g3.Controls.Add(chkSound);

            chkOnTop = new CheckBox();
            chkOnTop.Text = "Always On Top";
            chkOnTop.Location = new Point(140, 136);
            chkOnTop.AutoSize = true;
            chkOnTop.CheckedChanged += delegate { this.TopMost = chkOnTop.Checked; };
            g3.Controls.Add(chkOnTop);

            lblPlayStats = new Label();
            lblPlayStats.Text = "Loops Done: 0 | Total Clicks: 0";
            lblPlayStats.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblPlayStats.ForeColor = Color.FromArgb(137, 180, 250);
            lblPlayStats.Location = new Point(340, 136);
            lblPlayStats.AutoSize = true;
            g3.Controls.Add(lblPlayStats);

            // Big Play / Stop Button
            btnPlay = MakeBtn("PLAY RECORDED HOVER (F6)", 20, 632, 625, 54, Color.FromArgb(166, 227, 161), Color.FromArgb(24, 24, 37));
            btnPlay.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            btnPlay.Click += delegate { TogglePlayback(); };
            this.Controls.Add(btnPlay);

            Label lblHelp = new Label();
            lblHelp.Text = "HOTKEYS:  [F8] = Start / Stop Record   |   [F6] = Play / Stop Loop   |   [ESC] = Emergency Stop   |   [F9] = Clear\r\n" +
                           "TIPS: Kung True Background Mode ang napili, pwede ka mag-Alt+Tab ug mag-mouse sa lain samtang ga-dagan!";
            lblHelp.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblHelp.ForeColor = Color.FromArgb(249, 226, 175);
            lblHelp.Location = new Point(20, 695);
            lblHelp.Size = new Size(625, 40);
            this.Controls.Add(lblHelp);

            this.FormClosing += delegate { isRecording = false; isPlaying = false; UnregisterHotkeys(); };
        }

        #region UI Helpers
        private Label MakeLabel(Control parent, string text, int x, int y, Font f, Color c)
        {
            Label lb = new Label();
            lb.Text = text;
            lb.Font = f;
            lb.ForeColor = c;
            lb.Location = new Point(x, y);
            lb.AutoSize = true;
            parent.Controls.Add(lb);
            return lb;
        }

        private GroupBox MakeGroup(string t, int x, int y, int w, int h)
        {
            GroupBox g = new GroupBox();
            g.Text = t;
            g.Location = new Point(x, y);
            g.Size = new Size(w, h);
            g.ForeColor = Color.FromArgb(186, 194, 222);
            g.BackColor = Color.FromArgb(30, 30, 46);
            return g;
        }

        private Button MakeBtn(string t, int x, int y, int w, int h, Color bg, Color fg)
        {
            Button b = new Button();
            b.Text = t;
            b.Location = new Point(x, y);
            b.Size = new Size(w, h);
            b.BackColor = bg;
            b.ForeColor = fg;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Cursor = Cursors.Hand;
            return b;
        }

        private NumericUpDown MakeNumeric(Control parent, int x, int y, int w, int min, int max, int val, int inc)
        {
            NumericUpDown n = new NumericUpDown();
            n.Location = new Point(x, y);
            n.Size = new Size(w, 24);
            n.Minimum = min;
            n.Maximum = max;
            n.Value = val;
            n.Increment = inc;
            n.BackColor = Color.FromArgb(49, 50, 68);
            n.ForeColor = Color.White;
            parent.Controls.Add(n);
            return n;
        }
        #endregion

        #region Hotkeys
        private void RegisterHotkeys()
        {
            RegisterHotKey(this.Handle, HOTKEY_RECORD, 0, (uint)Keys.F8);
            RegisterHotKey(this.Handle, HOTKEY_PLAY, 0, (uint)Keys.F6);
            RegisterHotKey(this.Handle, HOTKEY_CLEAR, 0, (uint)Keys.F9);
        }

        private void UnregisterHotkeys()
        {
            UnregisterHotKey(this.Handle, HOTKEY_RECORD);
            UnregisterHotKey(this.Handle, HOTKEY_PLAY);
            UnregisterHotKey(this.Handle, HOTKEY_CLEAR);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_RECORD) ToggleRecording();
                else if (id == HOTKEY_PLAY) TogglePlayback();
                else if (id == HOTKEY_CLEAR) ClearAll();
            }
            base.WndProc(ref m);
        }
        #endregion

        #region Recording
        private void ToggleRecording()
        {
            if (isPlaying) { isPlaying = false; Thread.Sleep(100); }

            if (isRecording)
                StopRecording();
            else
                StartRecording();
        }

        private void StartRecording()
        {
            frames.Clear();
            lstFrames.Items.Clear();
            isRecording = true;

            btnRecord.Text = "STOP Record (F8)";
            btnRecord.BackColor = Color.FromArgb(166, 227, 161);
            lblStatus.Text = "[ RECORDING... ]  Lihoka imong mouse sa target window! Pindota F8 kung human na.";
            lblStatus.ForeColor = Color.FromArgb(243, 139, 168);

            Beep(1000, 100);

            sw.Reset();
            sw.Start();

            workerThread = new Thread(new ThreadStart(RecordLoop));
            workerThread.IsBackground = true;
            workerThread.Start();
        }

        private void RecordLoop()
        {
            bool prevL = false, prevR = false;

            while (isRecording)
            {
                POINT pt;
                if (GetCursorPos(out pt))
                {
                    IntPtr target = WindowFromPoint(pt);
                    POINT clientPt = pt;
                    if (target != IntPtr.Zero)
                    {
                        ScreenToClient(target, ref clientPt);
                    }

                    RecordedFrame f = new RecordedFrame();
                    f.ScreenX = pt.X;
                    f.ScreenY = pt.Y;
                    f.TargetHwnd = target;
                    f.ClientX = clientPt.X;
                    f.ClientY = clientPt.Y;
                    f.TimestampMs = sw.ElapsedMilliseconds;

                    bool curL = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
                    bool curR = (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;

                    if (curL && !prevL) f.IsLeftClick = true;
                    if (!curL && prevL) f.IsLeftRelease = true;
                    if (curR && !prevR) f.IsRightClick = true;
                    if (!curR && prevR) f.IsRightRelease = true;

                    prevL = curL;
                    prevR = curR;

                    frames.Add(f);

                    if (recordedMainHwnd == IntPtr.Zero && target != IntPtr.Zero)
                    {
                        IntPtr root = GetAncestor(target, GA_ROOT);
                        if (root == IntPtr.Zero) root = target;
                        recordedMainHwnd = root;

                        StringBuilder sbTitle = new StringBuilder(256);
                        GetWindowText(root, sbTitle, 256);
                        StringBuilder sbClass = new StringBuilder(256);
                        GetClassName(target, sbClass, 256);

                        recordedWindowTitle = string.Format("{0} [{1}] (HWND: 0x{2:X})",
                            sbTitle.ToString(), sbClass.ToString(), target.ToInt64());
                    }
                }

                Thread.Sleep(25); // ~40 fps
            }
        }

        private void StopRecording()
        {
            isRecording = false;
            sw.Stop();
            if (workerThread != null) workerThread.Join(500);

            btnRecord.Text = "Start Record (F8)";
            btnRecord.BackColor = Color.FromArgb(243, 139, 168);

            Beep(600, 100);

            // Populate listbox preview
            lstFrames.Items.Clear();
            int clicks = 0;
            int show = Math.Min(frames.Count, 200);
            for (int i = 0; i < show; i++)
            {
                lstFrames.Items.Add(frames[i].ToString());
                if (frames[i].IsLeftClick || frames[i].IsRightClick) clicks++;
            }
            if (frames.Count > 200)
                lstFrames.Items.Add(string.Format("... ug {0} pa ka frames ...", frames.Count - 200));

            for (int i = show; i < frames.Count; i++)
                if (frames[i].IsLeftClick || frames[i].IsRightClick) clicks++;

            long dur = frames.Count > 0 ? frames[frames.Count - 1].TimestampMs : 0;
            lblStats.Text = string.Format("Frames: {0} | Duration: {1:F1}s | Clicks: {2}", frames.Count, dur / 1000.0, clicks);
            lblTargetInfo.Text = "Target Window: " + recordedWindowTitle;

            if (frames.Count > 0)
            {
                lblStatus.Text = string.Format("[ READY ]  {0} frames recorded ({1:F1}s). Pindota F6 para mag-LOOP PLAY!", frames.Count, dur / 1000.0);
                lblStatus.ForeColor = Color.FromArgb(166, 227, 161);
            }
            else
            {
                lblStatus.Text = "[ IDLE ]  Walay na-record. Pindota F8 ug lihoka imong mouse.";
                lblStatus.ForeColor = Color.FromArgb(249, 226, 175);
            }
        }
        #endregion

        #region Playback
        private void TogglePlayback()
        {
            if (isRecording) StopRecording();

            if (isPlaying)
                StopPlayback();
            else
                StartPlayback();
        }

        private void StartPlayback()
        {
            if (frames.Count == 0)
            {
                MessageBox.Show(
                    "Wala pay na-record!\n\n" +
                    "1. Pindota F8 para mag-start og record.\n" +
                    "2. Lihoka imong mouse sa target window.\n" +
                    "3. Pindota F8 balik para mahuman.\n" +
                    "4. Pindota F6 para i-play!",
                    "No Recording", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            isPlaying = true;
            totalClicks = 0;
            loopsDone = 0;

            btnPlay.Text = "STOP PLAYBACK (F6)";
            btnPlay.BackColor = Color.FromArgb(243, 139, 168);

            bool isBg = rbBackgroundMode.Checked;
            if (isBg)
            {
                lblStatus.Text = "[ PLAYING BACKGROUND ]  Dili mo-lihok imong mouse! Pwede ka mag-Alt+Tab freely. F6 para hunong.";
                lblStatus.ForeColor = Color.FromArgb(166, 227, 161);
            }
            else
            {
                lblStatus.Text = "[ PLAYING CURSOR ]  Mouse is auto-moving in loop! F6 o ESC para hunong.";
                lblStatus.ForeColor = Color.FromArgb(249, 226, 175);
            }

            Beep(1200, 100);

            double speed = (double)numSpeed.Value;
            bool autoClick = chkAutoClick.Checked;
            int clickBtn = cmbButton.SelectedIndex;
            int clickEvery = (int)numClickEvery.Value;
            bool loopForever = chkLoopForever.Checked;
            long maxLoops = (long)numLoops.Value;
            int loopDelayMs = (int)numLoopDelay.Value;

            workerThread = new Thread(new ThreadStart(delegate
            {
                PlayLoop(isBg, speed, autoClick, clickBtn, clickEvery, loopForever, maxLoops, loopDelayMs);
            }));
            workerThread.IsBackground = true;
            workerThread.Start();
        }

        private void PlayLoop(bool isBackgroundMode, double speedPct, bool autoClick, int clickBtn,
                              int clickEvery, bool loopForever, long maxLoops, int loopDelayMs)
        {
            double speedFactor = speedPct / 100.0;
            if (speedFactor < 0.01) speedFactor = 0.01;

            while (isPlaying)
            {
                loopsDone++;
                int frameCounter = 0;

                for (int i = 0; i < frames.Count && isPlaying; i++)
                {
                    // Check emergency ESC key
                    if ((GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0)
                    {
                        isPlaying = false;
                        break;
                    }

                    RecordedFrame f = frames[i];
                    frameCounter++;

                    if (isBackgroundMode)
                    {
                        // True Background Mode (PostMessage): cursor NEVER moves!
                        IntPtr target = f.TargetHwnd;
                        if (!IsWindow(target))
                        {
                            POINT sp = new POINT { X = f.ScreenX, Y = f.ScreenY };
                            target = WindowFromPoint(sp);
                        }

                        if (target != IntPtr.Zero)
                        {
                            IntPtr lParam = MakeLParam(f.ClientX, f.ClientY);

                            // Send cursor hint & mouse move
                            PostMessage(target, WM_SETCURSOR, target, (IntPtr)((0x0200 << 16) | 1));
                            PostMessage(target, WM_MOUSEMOVE, IntPtr.Zero, lParam);

                            if (f.IsLeftClick)
                            {
                                PostMessage(target, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, lParam);
                                Interlocked.Increment(ref totalClicks);
                            }
                            if (f.IsLeftRelease)
                            {
                                PostMessage(target, WM_LBUTTONUP, IntPtr.Zero, lParam);
                            }
                            if (f.IsRightClick)
                            {
                                PostMessage(target, WM_RBUTTONDOWN, (IntPtr)MK_RBUTTON, lParam);
                                Interlocked.Increment(ref totalClicks);
                            }
                            if (f.IsRightRelease)
                            {
                                PostMessage(target, WM_RBUTTONUP, IntPtr.Zero, lParam);
                            }

                            // Optional auto-click
                            if (autoClick && (frameCounter % clickEvery == 0))
                            {
                                uint downMsg = clickBtn == 1 ? WM_RBUTTONDOWN : WM_LBUTTONDOWN;
                                uint upMsg   = clickBtn == 1 ? WM_RBUTTONUP   : WM_LBUTTONUP;
                                IntPtr flag  = (IntPtr)(clickBtn == 1 ? MK_RBUTTON : MK_LBUTTON);

                                PostMessage(target, downMsg, flag, lParam);
                                Thread.Sleep(5);
                                PostMessage(target, upMsg, IntPtr.Zero, lParam);
                                Interlocked.Increment(ref totalClicks);
                            }
                        }
                    }
                    else
                    {
                        // Hardware Cursor Mode (SendInput)
                        MovePhysicalMouse(f.ScreenX, f.ScreenY);

                        if (f.IsLeftClick) { SendPhysicalClick(0, true); Interlocked.Increment(ref totalClicks); }
                        if (f.IsLeftRelease) SendPhysicalClick(0, false);
                        if (f.IsRightClick) { SendPhysicalClick(1, true); Interlocked.Increment(ref totalClicks); }
                        if (f.IsRightRelease) SendPhysicalClick(1, false);

                        if (autoClick && (frameCounter % clickEvery == 0))
                        {
                            SendPhysicalClick(clickBtn, true);
                            Thread.Sleep(5);
                            SendPhysicalClick(clickBtn, false);
                            Interlocked.Increment(ref totalClicks);
                        }
                    }

                    // Precise delay to next frame
                    if (i + 1 < frames.Count && isPlaying)
                    {
                        long delta = frames[i + 1].TimestampMs - f.TimestampMs;
                        int sleepMs = (int)(delta / speedFactor);
                        if (sleepMs > 0 && sleepMs < 10000)
                            Thread.Sleep(sleepMs);
                    }
                }

                // Check loop termination
                if (!loopForever && loopsDone >= maxLoops)
                    break;

                // Loop delay between iterations
                if (isPlaying && loopDelayMs > 0)
                    Thread.Sleep(loopDelayMs);
            }

            if (this.IsHandleCreated)
                this.BeginInvoke(new Action(delegate { StopPlayback(); }));
        }

        private static IntPtr MakeLParam(int loWord, int hiWord)
        {
            return (IntPtr)((hiWord << 16) | (loWord & 0xFFFF));
        }

        private void MovePhysicalMouse(int screenX, int screenY)
        {
            int normX = (int)(((double)screenX / screenW) * 65535);
            int normY = (int)(((double)screenY / screenH) * 65535);

            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dx = normX;
            inputs[0].mi.dy = normY;
            inputs[0].mi.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE;
            inputs[0].mi.time = 0;
            inputs[0].mi.dwExtraInfo = IntPtr.Zero;
            inputs[0].mi.mouseData = 0;

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private void SendPhysicalClick(int btn, bool isDown)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;

            if (btn == 1) // Right
                inputs[0].mi.dwFlags = isDown ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP;
            else if (btn == 2) // Middle
                inputs[0].mi.dwFlags = isDown ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP;
            else // Left
                inputs[0].mi.dwFlags = isDown ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP;

            inputs[0].mi.dwExtraInfo = IntPtr.Zero;
            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private void StopPlayback()
        {
            isPlaying = false;
            btnPlay.Text = "PLAY RECORDED HOVER (F6)";
            btnPlay.BackColor = Color.FromArgb(166, 227, 161);
            lblStatus.Text = string.Format("[ STOPPED ]  Loops done: {0} | Clicks: {1}. Pindota F6 para mag-play balik.", loopsDone, totalClicks);
            lblStatus.ForeColor = Color.FromArgb(249, 226, 175);

            Beep(500, 100);
        }

        private void ClearAll()
        {
            if (isRecording) StopRecording();
            if (isPlaying) StopPlayback();

            frames.Clear();
            lstFrames.Items.Clear();
            recordedMainHwnd = IntPtr.Zero;
            recordedWindowTitle = "None";
            lblStats.Text = "Frames: 0 | Duration: 0.0s | Clicks: 0";
            lblTargetInfo.Text = "Target Window: (I-hover imong mouse sa target unya pindota F8)";
            lblStatus.Text = "[ CLEARED ]  Walay sulod. Pindota F8 para mag-record og bag-o.";
            lblStatus.ForeColor = Color.FromArgb(249, 226, 175);
        }

        private void UiTick(object sender, EventArgs e)
        {
            if (isPlaying)
            {
                lblPlayStats.Text = string.Format("Loops Done: {0} | Total Clicks: {1}", loopsDone, totalClicks);
            }
        }

        private void Beep(int freq, int dur)
        {
            if (chkSound != null && chkSound.Checked)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try { Console.Beep(freq, dur); } catch { }
                });
            }
        }
        #endregion
    }
}
