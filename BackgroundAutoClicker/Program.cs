using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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

    public class AttributeSlot
    {
        public string Name;
        public Keys Hotkey;
        public int ScreenX;
        public int ScreenY;
        public IntPtr TargetHwnd;
        public int ClientX;
        public int ClientY;
        public bool IsSet;

        public AttributeSlot(string name, Keys defaultKey)
        {
            Name = name;
            Hotkey = defaultKey;
            ScreenX = 0;
            ScreenY = 0;
            TargetHwnd = IntPtr.Zero;
            ClientX = 0;
            ClientY = 0;
            IsSet = false;
        }
    }

    public class CuboidActionSlot
    {
        public string Name;
        public Keys Hotkey;
        public int OffsetY; // Exact pixel offset Y below "..." button (auto-calculated)

        public CuboidActionSlot(string name, Keys defaultKey, int offsetY)
        {
            Name = name;
            Hotkey = defaultKey;
            OffsetY = offsetY;
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
        public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

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
        private const int HOTKEY_RECORD         = 1;  // F8
        private const int HOTKEY_PLAY           = 2;  // F6
        private const int HOTKEY_CLEAR          = 3;  // F9
        private const int HOTKEY_TOGGLE_ATTR    = 4;  // F7 (Master switch for Tab 2 Attributes)
        private const int HOTKEY_TOGGLE_CUBOID  = 10; // F4 (Master switch for Tab 3 Cuboid actions)
        private const int HOTKEY_CUBOID_SETUP   = 11; // X (Auto-setup cuboid)
        private const int HOTKEY_ATTR_BASE      = 100; // 100..104 for Slots 0..4 (Tab 2)
        private const int HOTKEY_CUBOID_BASE    = 200; // 200..203 for Cuboid actions 0..3 (Tab 3)

        private const uint GA_ROOT = 2;
        #endregion

        #region Hover Recorder State
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

        #region Tab 2: Attributes State
        private bool attributesEnabled = true;
        private List<AttributeSlot> attrSlots = new List<AttributeSlot>();
        private int listeningSlotIndex = -1;

        private bool autoNextFrame = true;
        private Keys nextFrameKey = Keys.Oemplus; // '+' key
        private int nextFrameDelay = 100; // ms
        private bool isListeningNextKey = false;
        #endregion

        #region Tab 3: Cuboid Automations State
        private bool cuboidActionsEnabled = true;

        // "..." 3-dots button — the ONLY position to set once
        private Point dotsPos = new Point(0, 0);

        // Auto-calculated pixel offset Y below "..." button for "Make dynamic"
        private int makeDynamicOffsetY = 215; // 6th item in Remotasks menu

        private Keys cuboidSetupKey = Keys.X;
        private bool isListeningCuboidSetupKey = false;

        // 4 Actions: Floor (+102px), CopyAttr (+134px), CopyMotion (+166px), Delete (+264px)
        private List<CuboidActionSlot> cuboidActions = new List<CuboidActionSlot>();
        private int listeningCuboidActionIndex = -1;
        #endregion

        #region Shared Countdown Timer State
        private System.Windows.Forms.Timer countdownTimer;
        private int countdownSeconds = 0;
        private int countdownTargetType = 0; // 0: None, 1: AttrSlot, 2: DotsPos
        private int countdownTargetIndex = -1;

        private string configPath;
        #endregion

        #region UI Controls
        private Label lblStatus;

        // Tab Navigation
        private Button btnTabHover;
        private Button btnTabAttr;
        private Button btnTabCuboid;
        private Panel pnlHoverTab;
        private Panel pnlAttrTab;
        private Panel pnlCuboidTab;

        // Hover Tab Controls
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

        // Attributes Tab Controls
        private Button btnAttrMasterToggle;
        private RadioButton rbAttrPhysical;
        private RadioButton rbAttrPostMessage;
        private CheckBox chkRestoreCursor;

        private CheckBox chkAutoNextFrame;
        private Button btnNextKey;
        private NumericUpDown numNextDelay;
        private Button btnTestNext;

        private Label[] lblSlotName = new Label[5];
        private Button[] btnSlotKey = new Button[5];
        private Label[] lblSlotPos = new Label[5];
        private Button[] btnSlotSet = new Button[5];
        private Button[] btnSlotTest = new Button[5];
        private Button[] btnSlotClear = new Button[5];
        private Button[] btnQuickTest = new Button[5];

        // Cuboid Tab Controls
        private Button btnCuboidMasterToggle;

        private Label lblDotsPos;
        private Button btnSetDots;

        private Button btnCuboidSetupKey;
        private Button btnRunCuboidSetup;
        private NumericUpDown numMakeDynamicIdx;

        private Label[] lblCuboidActName = new Label[4];
        private Button[] btnCuboidActKey = new Button[4];
        private NumericUpDown[] numCuboidActIndex = new NumericUpDown[4];
        private Button[] btnCuboidActTest = new Button[4];
        #endregion

        public MainForm()
        {
            this.KeyPreview = true;
            configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "attributes_config.txt");

            screenW = GetSystemMetrics(SM_CXSCREEN);
            screenH = GetSystemMetrics(SM_CYSCREEN);
            if (screenW <= 0) screenW = 1920;
            if (screenH <= 0) screenH = 1080;

            InitAttributeSlots();
            InitCuboidActionSlots();
            LoadAttributesConfig();

            BuildUI();
            RegisterHotkeys();

            uiTimer = new System.Windows.Forms.Timer();
            uiTimer.Interval = 200;
            uiTimer.Tick += new EventHandler(UiTick);
            uiTimer.Start();

            countdownTimer = new System.Windows.Forms.Timer();
            countdownTimer.Interval = 1000;
            countdownTimer.Tick += new EventHandler(CountdownTick);
        }

        private void InitAttributeSlots()
        {
            attrSlots.Clear();
            attrSlots.Add(new AttributeSlot("0%", Keys.D1));
            attrSlots.Add(new AttributeSlot("0% - 25%", Keys.D2));
            attrSlots.Add(new AttributeSlot("25% - 50%", Keys.D3));
            attrSlots.Add(new AttributeSlot("50% - 75%", Keys.D4));
            attrSlots.Add(new AttributeSlot("75% - 100%", Keys.D5));
        }

        private void InitCuboidActionSlots()
        {
            cuboidActions.Clear();
            // Dropdown menu items are automatically clicked relative to "..." button:
            // X offset = -90 (centered in menu), Y offset below "..." button:
            // 3=Set floor (+102px), 4=Copy attr (+134px), 5=Copy motion (+166px), 7=Delete (+264px)
            cuboidActions.Add(new CuboidActionSlot("Set Floor to Bottom", Keys.D4, 102));
            cuboidActions.Add(new CuboidActionSlot("Copy Attributes",     Keys.D2, 134));
            cuboidActions.Add(new CuboidActionSlot("Copy Motion",         Keys.D1, 166));
            cuboidActions.Add(new CuboidActionSlot("Delete Cuboid",       Keys.D3, 264));
        }

        private void BuildUI()
        {
            this.Text = "Project ni Xyrad";
            this.Size = new Size(730, 895);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(24, 24, 37);
            this.ForeColor = Color.FromArgb(205, 214, 244);
            this.Font = new Font("Segoe UI", 9.5f);

            // Top Banner
            Panel top = new Panel();
            top.Dock = DockStyle.Top;
            top.Height = 65;
            top.BackColor = Color.FromArgb(30, 30, 46);
            this.Controls.Add(top);

            MakeLabel(top, "PROJECT NI XYRAD", 18, 8, new Font("Segoe UI", 14f, FontStyle.Bold), Color.FromArgb(137, 180, 250));
            MakeLabel(top, "Hover Recorder  |  Visibility Auto-Next  |  Cuboid Dynamic & Actions",
                20, 38, new Font("Segoe UI", 8.5f), Color.FromArgb(166, 173, 200));

            // Status Bar
            Panel pnlS = new Panel();
            pnlS.Location = new Point(20, 75);
            pnlS.Size = new Size(675, 38);
            pnlS.BackColor = Color.FromArgb(30, 30, 46);
            this.Controls.Add(pnlS);

            lblStatus = new Label();
            lblStatus.Text = "[ READY ]  F8: Record  |  F7: Toggle Visibility  |  F4: Toggle Cuboid Actions";
            lblStatus.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblStatus.ForeColor = Color.FromArgb(249, 226, 175);
            lblStatus.Location = new Point(10, 8);
            lblStatus.AutoSize = true;
            pnlS.Controls.Add(lblStatus);

            // 3 Tab Navigation Buttons
            int tabW = 221;
            btnTabHover = MakeBtn("🖱️  1. Hover Recorder", 20, 120, tabW, 40, Color.FromArgb(137, 180, 250), Color.FromArgb(24, 24, 37));
            btnTabHover.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnTabHover.Click += delegate { SwitchTab(0); };
            this.Controls.Add(btnTabHover);

            btnTabAttr = MakeBtn("🏷️  2. Visibility (1-5)", 246, 120, tabW, 40, Color.FromArgb(49, 50, 68), Color.FromArgb(205, 214, 244));
            btnTabAttr.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            btnTabAttr.Click += delegate { SwitchTab(1); };
            this.Controls.Add(btnTabAttr);

            btnTabCuboid = MakeBtn("📦  3. Cuboid Actions", 472, 120, tabW, 40, Color.FromArgb(49, 50, 68), Color.FromArgb(205, 214, 244));
            btnTabCuboid.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            btnTabCuboid.Click += delegate { SwitchTab(2); };
            this.Controls.Add(btnTabCuboid);

            Image bgImg = LoadBackgroundImage();
            if (bgImg != null)
            {
                this.BackgroundImage = bgImg;
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }

            Icon appIcon = LoadAppIcon();
            if (appIcon != null)
            {
                this.Icon = appIcon;
            }

            // Panels for Tabs
            pnlHoverTab = new Panel();
            pnlHoverTab.Location = new Point(20, 168);
            pnlHoverTab.Size = new Size(675, 675);
            pnlHoverTab.BackColor = Color.Transparent;
            this.Controls.Add(pnlHoverTab);

            pnlAttrTab = new Panel();
            pnlAttrTab.Location = new Point(20, 168);
            pnlAttrTab.Size = new Size(675, 675);
            pnlAttrTab.BackColor = Color.Transparent;
            pnlAttrTab.Visible = false;
            this.Controls.Add(pnlAttrTab);

            pnlCuboidTab = new Panel();
            pnlCuboidTab.Location = new Point(20, 168);
            pnlCuboidTab.Size = new Size(675, 675);
            pnlCuboidTab.BackColor = Color.Transparent;
            pnlCuboidTab.Visible = false;
            this.Controls.Add(pnlCuboidTab);

            BuildHoverTab();
            BuildAttrTab();
            BuildCuboidTab();

            this.FormClosing += delegate
            {
                isRecording = false;
                isPlaying = false;
                UnregisterHotkeys();
                SaveAttributesConfig();
            };
        }

        private void SwitchTab(int tabIndex)
        {
            pnlHoverTab.Visible = (tabIndex == 0);
            pnlAttrTab.Visible = (tabIndex == 1);
            pnlCuboidTab.Visible = (tabIndex == 2);

            Button[] btns = new Button[] { btnTabHover, btnTabAttr, btnTabCuboid };
            for (int i = 0; i < 3; i++)
            {
                if (i == tabIndex)
                {
                    btns[i].BackColor = Color.FromArgb(137, 180, 250);
                    btns[i].ForeColor = Color.FromArgb(24, 24, 37);
                    btns[i].Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else
                {
                    btns[i].BackColor = Color.FromArgb(49, 50, 68);
                    btns[i].ForeColor = Color.FromArgb(205, 214, 244);
                    btns[i].Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                }
            }
        }

        #region Hover Tab UI
        private void BuildHoverTab()
        {
            GroupBox gMode = MakeGroup("1. Playback Mode (Pilia kung unsa imong gusto)", 0, 0, 675, 88);
            pnlHoverTab.Controls.Add(gMode);

            rbBackgroundMode = new RadioButton();
            rbBackgroundMode.Text = "🌟 True Background Mode (PostMessage) - Dili mo-lihok ang mouse. Pwede Alt+Tab & work sa lain!";
            rbBackgroundMode.Checked = true;
            rbBackgroundMode.Location = new Point(15, 20);
            rbBackgroundMode.Size = new Size(645, 32);
            rbBackgroundMode.ForeColor = Color.FromArgb(166, 227, 161);
            rbBackgroundMode.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            gMode.Controls.Add(rbBackgroundMode);

            rbPhysicalMode = new RadioButton();
            rbPhysicalMode.Text = "🎯 Hardware Cursor Mode (SendInput) - Mo-lihok ang physical cursor sa screen (Infinite Loop)";
            rbPhysicalMode.Location = new Point(15, 54);
            rbPhysicalMode.Size = new Size(645, 24);
            rbPhysicalMode.ForeColor = Color.FromArgb(249, 226, 175);
            rbPhysicalMode.Font = new Font("Segoe UI", 9f);
            gMode.Controls.Add(rbPhysicalMode);

            GroupBox g2 = MakeGroup("2. Recorded Path Preview & Target Window", 0, 94, 675, 195);
            pnlHoverTab.Controls.Add(g2);

            lblTargetInfo = new Label();
            lblTargetInfo.Text = "Target Window: (I-hover imong mouse sa target unya pindota F8)";
            lblTargetInfo.Font = new Font("Segoe UI", 8.5f, FontStyle.Italic);
            lblTargetInfo.ForeColor = Color.FromArgb(137, 180, 250);
            lblTargetInfo.Location = new Point(15, 22);
            lblTargetInfo.Size = new Size(645, 20);
            g2.Controls.Add(lblTargetInfo);

            lstFrames = new ListBox();
            lstFrames.Location = new Point(15, 46);
            lstFrames.Size = new Size(505, 108);
            lstFrames.BackColor = Color.FromArgb(17, 17, 27);
            lstFrames.ForeColor = Color.FromArgb(205, 214, 244);
            lstFrames.Font = new Font("Consolas", 8.5f);
            lstFrames.BorderStyle = BorderStyle.FixedSingle;
            g2.Controls.Add(lstFrames);

            btnRecord = MakeBtn("Start Record (F8)", 530, 46, 130, 36, Color.FromArgb(243, 139, 168), Color.FromArgb(24, 24, 37));
            btnRecord.Click += delegate { ToggleRecording(); };
            g2.Controls.Add(btnRecord);

            btnClear = MakeBtn("Clear (F9)", 530, 90, 130, 30, Color.FromArgb(69, 71, 90), Color.White);
            btnClear.Click += delegate { ClearAll(); };
            g2.Controls.Add(btnClear);

            lblStats = new Label();
            lblStats.Text = "Frames: 0 | Duration: 0.0s | Clicks: 0";
            lblStats.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblStats.ForeColor = Color.FromArgb(166, 227, 161);
            lblStats.Location = new Point(15, 162);
            lblStats.AutoSize = true;
            g2.Controls.Add(lblStats);

            GroupBox g3 = MakeGroup("3. Playback Settings", 0, 296, 675, 165);
            pnlHoverTab.Controls.Add(g3);

            MakeLabel(g3, "Speed (%):", 15, 26, this.Font, this.ForeColor);
            numSpeed = MakeNumeric(g3, 95, 24, 65, 10, 1000, 100, 10);
            MakeLabel(g3, "(100 = normal, 200 = 2x faster, 50 = half speed)", 170, 27,
                new Font("Segoe UI", 8f, FontStyle.Italic), Color.FromArgb(147, 153, 178));

            chkLoopForever = new CheckBox();
            chkLoopForever.Text = "Loop Forever (Walay undang hangtod pinduton F6)";
            chkLoopForever.Checked = true;
            chkLoopForever.Location = new Point(15, 58);
            chkLoopForever.AutoSize = true;
            chkLoopForever.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            chkLoopForever.ForeColor = Color.FromArgb(166, 227, 161);
            chkLoopForever.CheckedChanged += delegate { numLoops.Enabled = !chkLoopForever.Checked; };
            g3.Controls.Add(chkLoopForever);

            MakeLabel(g3, "Max Loops:", 410, 60, this.Font, this.ForeColor);
            numLoops = MakeNumeric(g3, 495, 58, 65, 1, 999999, 10, 1);
            numLoops.Enabled = false;

            MakeLabel(g3, "Delay between loops (ms):", 15, 92, this.Font, this.ForeColor);
            numLoopDelay = MakeNumeric(g3, 195, 90, 65, 0, 10000, 50, 50);

            chkAutoClick = new CheckBox();
            chkAutoClick.Text = "Auto-click every N frames:";
            chkAutoClick.Checked = false;
            chkAutoClick.Location = new Point(290, 92);
            chkAutoClick.AutoSize = true;
            g3.Controls.Add(chkAutoClick);

            numClickEvery = MakeNumeric(g3, 475, 90, 55, 1, 1000, 5, 1);

            cmbButton = new ComboBox();
            cmbButton.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbButton.Items.AddRange(new object[] { "Left", "Right" });
            cmbButton.SelectedIndex = 0;
            cmbButton.Location = new Point(540, 90);
            cmbButton.Size = new Size(70, 24);
            cmbButton.BackColor = Color.FromArgb(49, 50, 68);
            cmbButton.ForeColor = Color.White;
            g3.Controls.Add(cmbButton);

            chkSound = new CheckBox();
            chkSound.Text = "Audio Beeps";
            chkSound.Checked = true;
            chkSound.Location = new Point(15, 128);
            chkSound.AutoSize = true;
            g3.Controls.Add(chkSound);

            chkOnTop = new CheckBox();
            chkOnTop.Text = "Always On Top";
            chkOnTop.Location = new Point(140, 128);
            chkOnTop.AutoSize = true;
            chkOnTop.CheckedChanged += delegate { this.TopMost = chkOnTop.Checked; };
            g3.Controls.Add(chkOnTop);

            lblPlayStats = new Label();
            lblPlayStats.Text = "Loops Done: 0 | Total Clicks: 0";
            lblPlayStats.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblPlayStats.ForeColor = Color.FromArgb(137, 180, 250);
            lblPlayStats.Location = new Point(340, 128);
            lblPlayStats.AutoSize = true;
            g3.Controls.Add(lblPlayStats);

            btnPlay = MakeBtn("PLAY RECORDED HOVER (F6)", 0, 470, 675, 52, Color.FromArgb(166, 227, 161), Color.FromArgb(24, 24, 37));
            btnPlay.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            btnPlay.Click += delegate { TogglePlayback(); };
            pnlHoverTab.Controls.Add(btnPlay);

            Label lblHelp = new Label();
            lblHelp.Text = "HOTKEYS:  [F8] = Start / Stop Record   |   [F6] = Play / Stop Loop   |   [ESC] = Emergency Stop   |   [F9] = Clear\r\n" +
                           "TIPS: Kung True Background Mode ang napili, pwede ka mag-Alt+Tab ug mag-mouse sa lain samtang ga-dagan!";
            lblHelp.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblHelp.ForeColor = Color.FromArgb(249, 226, 175);
            lblHelp.Location = new Point(0, 532);
            lblHelp.Size = new Size(675, 40);
            pnlHoverTab.Controls.Add(lblHelp);
        }
        #endregion

        #region Tab 2: Attributes UI
        private void BuildAttrTab()
        {
            GroupBox gMaster = MakeGroup("1. Attributes Master Switch, Click Mode, & Auto-Next Frame", 0, 0, 675, 142);
            pnlAttrTab.Controls.Add(gMaster);

            btnAttrMasterToggle = MakeBtn("", 15, 20, 645, 38, Color.FromArgb(166, 227, 161), Color.FromArgb(24, 24, 37));
            btnAttrMasterToggle.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnAttrMasterToggle.Click += delegate { ToggleAttributesMaster(); };
            gMaster.Controls.Add(btnAttrMasterToggle);
            UpdateMasterToggleUI();

            rbAttrPhysical = new RadioButton();
            rbAttrPhysical.Text = "⚡ Instant Jump & Click";
            rbAttrPhysical.Checked = true;
            rbAttrPhysical.Location = new Point(15, 66);
            rbAttrPhysical.Size = new Size(180, 24);
            rbAttrPhysical.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            rbAttrPhysical.ForeColor = Color.FromArgb(166, 227, 161);
            rbAttrPhysical.CheckedChanged += delegate { SaveAttributesConfig(); };
            gMaster.Controls.Add(rbAttrPhysical);

            chkRestoreCursor = new CheckBox();
            chkRestoreCursor.Text = "Auto-return cursor to canvas";
            chkRestoreCursor.Checked = true;
            chkRestoreCursor.Location = new Point(200, 67);
            chkRestoreCursor.Size = new Size(200, 22);
            chkRestoreCursor.ForeColor = Color.FromArgb(249, 226, 175);
            gMaster.Controls.Add(chkRestoreCursor);

            rbAttrPostMessage = new RadioButton();
            rbAttrPostMessage.Text = "🌟 Background (PostMessage)";
            rbAttrPostMessage.Location = new Point(415, 66);
            rbAttrPostMessage.Size = new Size(245, 24);
            rbAttrPostMessage.Font = new Font("Segoe UI", 9f);
            rbAttrPostMessage.ForeColor = Color.FromArgb(137, 180, 250);
            rbAttrPostMessage.CheckedChanged += delegate { SaveAttributesConfig(); };
            gMaster.Controls.Add(rbAttrPostMessage);

            Panel pnlNext = new Panel();
            pnlNext.Location = new Point(15, 96);
            pnlNext.Size = new Size(645, 36);
            pnlNext.BackColor = Color.FromArgb(24, 24, 37);
            gMaster.Controls.Add(pnlNext);

            chkAutoNextFrame = new CheckBox();
            chkAutoNextFrame.Text = "⏩ Auto-Next Frame human og click:";
            chkAutoNextFrame.Checked = autoNextFrame;
            chkAutoNextFrame.Location = new Point(8, 7);
            chkAutoNextFrame.Size = new Size(250, 22);
            chkAutoNextFrame.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            chkAutoNextFrame.ForeColor = Color.FromArgb(166, 227, 161);
            chkAutoNextFrame.CheckedChanged += delegate
            {
                autoNextFrame = chkAutoNextFrame.Checked;
                SaveAttributesConfig();
            };
            pnlNext.Controls.Add(chkAutoNextFrame);

            btnNextKey = MakeBtn(string.Format("Next Key: {0}", FormatKeyName(nextFrameKey)),
                262, 3, 105, 30, Color.FromArgb(49, 50, 68), Color.White);
            btnNextKey.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnNextKey.Click += delegate { StartListeningNextKey(); };
            pnlNext.Controls.Add(btnNextKey);

            MakeLabel(pnlNext, "Delay:", 375, 9, this.Font, Color.FromArgb(186, 194, 222));
            numNextDelay = MakeNumeric(pnlNext, 420, 7, 55, 10, 1000, nextFrameDelay, 10);
            numNextDelay.ValueChanged += delegate
            {
                nextFrameDelay = (int)numNextDelay.Value;
                SaveAttributesConfig();
            };

            MakeLabel(pnlNext, "ms", 480, 9, this.Font, Color.FromArgb(186, 194, 222));

            btnTestNext = MakeBtn("Test [ + ]", 515, 3, 120, 30, Color.FromArgb(137, 180, 250), Color.FromArgb(24, 24, 37));
            btnTestNext.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnTestNext.Click += delegate
            {
                IntPtr tgt = recordedMainHwnd;
                if (tgt == IntPtr.Zero && attrSlots.Count > 0 && attrSlots[0].IsSet) tgt = attrSlots[0].TargetHwnd;
                if (tgt != IntPtr.Zero && IsWindow(tgt))
                {
                    SetForegroundWindow(tgt);
                    Thread.Sleep(80);
                }
                SendNextFrameKey();
                lblStatus.Text = string.Format("[ TEST NEXT ] Gi-padala ang key [{0}] para sa next frame!", FormatKeyName(nextFrameKey));
                lblStatus.ForeColor = Color.FromArgb(166, 227, 161);
                Beep(1100, 60);
            };
            pnlNext.Controls.Add(btnTestNext);

            GroupBox gSlots = MakeGroup("2. Visibility Attribute Slots (Editable Hotkeys & Position Captures)", 0, 150, 675, 385);
            pnlAttrTab.Controls.Add(gSlots);

            int startY = 25;
            int stepY = 52;

            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                int curY = startY + (i * stepY);

                lblSlotName[i] = new Label();
                lblSlotName[i].Text = string.Format("{0}. {1}", i + 1, attrSlots[i].Name);
                lblSlotName[i].Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                lblSlotName[i].ForeColor = Color.FromArgb(137, 180, 250);
                lblSlotName[i].Location = new Point(15, curY + 6);
                lblSlotName[i].Size = new Size(125, 24);
                gSlots.Controls.Add(lblSlotName[i]);

                btnSlotKey[i] = MakeBtn(string.Format("Key: {0}", FormatKeyName(attrSlots[i].Hotkey)),
                    145, curY, 95, 30, Color.FromArgb(49, 50, 68), Color.White);
                btnSlotKey[i].Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                btnSlotKey[i].Click += delegate { StartListeningKey(idx); };
                gSlots.Controls.Add(btnSlotKey[i]);

                lblSlotPos[i] = new Label();
                lblSlotPos[i].Text = "[ Wala pa na-set ]";
                lblSlotPos[i].Font = new Font("Segoe UI", 9f);
                lblSlotPos[i].ForeColor = Color.FromArgb(108, 112, 134);
                lblSlotPos[i].Location = new Point(248, curY + 7);
                lblSlotPos[i].Size = new Size(160, 22);
                gSlots.Controls.Add(lblSlotPos[i]);

                btnSlotSet[i] = MakeBtn("Set Pos (3s)", 415, curY, 105, 30, Color.FromArgb(243, 139, 168), Color.FromArgb(24, 24, 37));
                btnSlotSet[i].Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                btnSlotSet[i].Click += delegate { StartCountdown(1, idx); };
                gSlots.Controls.Add(btnSlotSet[i]);

                btnSlotTest[i] = MakeBtn("Test", 528, curY, 62, 30, Color.FromArgb(166, 227, 161), Color.FromArgb(24, 24, 37));
                btnSlotTest[i].Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                btnSlotTest[i].Click += delegate { TriggerSlotClick(idx); };
                gSlots.Controls.Add(btnSlotTest[i]);

                btnSlotClear[i] = MakeBtn("Clear", 596, curY, 62, 30, Color.FromArgb(69, 71, 90), Color.White);
                btnSlotClear[i].Font = new Font("Segoe UI", 8.5f);
                btnSlotClear[i].Click += delegate { ClearSlot(idx); };
                gSlots.Controls.Add(btnSlotClear[i]);

                RefreshSlotUI(i);
            }

            Label lblQuick = new Label();
            lblQuick.Text = "Quick Click Test Buttons (Walay hotkey kinahanglan, direct click):";
            lblQuick.Font = new Font("Segoe UI", 8.5f, FontStyle.Italic);
            lblQuick.ForeColor = Color.FromArgb(186, 194, 222);
            lblQuick.Location = new Point(15, 290);
            lblQuick.Size = new Size(645, 20);
            gSlots.Controls.Add(lblQuick);

            string[] quickLabels = new string[] { "0%", "0% - 25%", "25% - 50%", "50% - 75%", "75% - 100%" };
            int qW = 124;
            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                btnQuickTest[i] = MakeBtn(quickLabels[i], 15 + (i * (qW + 6)), 315, qW, 36, Color.FromArgb(49, 50, 68), Color.White);
                btnQuickTest[i].Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                btnQuickTest[i].Click += delegate { TriggerSlotClick(idx); };
                gSlots.Controls.Add(btnQuickTest[i]);
            }

            Panel pnlHelp = new Panel();
            pnlHelp.Location = new Point(0, 545);
            pnlHelp.Size = new Size(675, 110);
            pnlHelp.BackColor = Color.FromArgb(30, 30, 46);
            pnlAttrTab.Controls.Add(pnlHelp);

            MakeLabel(pnlHelp, "💡 UNSAON PAG-GAMIT SA AUTO-NEXT ATTRIBUTES:", 15, 8, new Font("Segoe UI", 9.5f, FontStyle.Bold), Color.FromArgb(249, 226, 175));
            MakeLabel(pnlHelp, "1. I-set una ang 5 ka posisyon gamit ang \"Set Pos (3s)\" button.\r\n" +
                               "2. Pindota ang 1, 2, 3, 4, o 5: mo-click sa radio button unya AUTO-NEXT dayon sa next frame gamit ang \"+\"!\r\n" +
                               "3. Pindota F7 bisan kanus-a para i-OFF o i-ON ang Attributes (kung naka-OFF, magamit nimo ang 1-5 sa lain!).",
                               15, 32, new Font("Segoe UI", 8.5f), Color.FromArgb(205, 214, 244));
        }
        #endregion

        #region Tab 3: Cuboid Actions UI
        private void BuildCuboidTab()
        {
            // ── Group 1: ONE-TIME SETUP (just the "..." dots button) ──────────────────
            GroupBox gSetup = MakeGroup("1. One-Time Setup — \"...\" (3 Dots) Button Position", 0, 0, 675, 120);
            pnlCuboidTab.Controls.Add(gSetup);

            MakeLabel(gSetup, "\"...\" (3 Dots) Button:", 15, 26,
                new Font("Segoe UI", 9.5f, FontStyle.Bold), Color.FromArgb(137, 180, 250));

            lblDotsPos = MakeLabel(gSetup, FormatPoint(dotsPos), 185, 26, this.Font, Color.FromArgb(166, 227, 161));

            btnSetDots = MakeBtn("📍 Set Pos (3s hover)", 380, 20, 160, 30,
                Color.FromArgb(243, 139, 168), Color.FromArgb(24, 24, 37));
            btnSetDots.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnSetDots.Click += delegate { StartCountdown(2, 0); };
            gSetup.Controls.Add(btnSetDots);

            Label lblDotsNote = new Label();
            lblDotsNote.Text = "⚠️  I-set ni ISIG USAB LANG kung mag-lihok ang browser window. Isave na dayon sa config!";
            lblDotsNote.Font = new Font("Segoe UI", 8.5f, FontStyle.Italic);
            lblDotsNote.ForeColor = Color.FromArgb(249, 226, 175);
            lblDotsNote.Location = new Point(15, 60);
            lblDotsNote.Size = new Size(645, 40);
            gSetup.Controls.Add(lblDotsNote);

            // ── Group 2: MAKE DYNAMIC + 100% DENSITY (Auto-Click!) ──
            GroupBox gDyn = MakeGroup("2. Make Dynamic + 100% Density — 100% Automatic (Auto-Click from '...')", 0, 128, 675, 145);
            pnlCuboidTab.Controls.Add(gDyn);

            // Info row
            Label lblDynInfo = new Label();
            lblDynInfo.Text = "🤖  Automatic! I-click ang \"...\", unya automatic mo-click sa \"Make dynamic\" (+215px), unya Alt+J para sa Density.";
            lblDynInfo.Font = new Font("Segoe UI", 8.5f);
            lblDynInfo.ForeColor = Color.FromArgb(166, 227, 161);
            lblDynInfo.Location = new Point(15, 20);
            lblDynInfo.Size = new Size(645, 38);
            gDyn.Controls.Add(lblDynInfo);

            // Hotkey + Make Dynamic offset Y + Run button
            MakeLabel(gDyn, "Hotkey:", 15, 68, new Font("Segoe UI", 9f, FontStyle.Bold), Color.FromArgb(249, 226, 175));

            btnCuboidSetupKey = MakeBtn(string.Format("Key: {0}", FormatKeyName(cuboidSetupKey)),
                75, 63, 100, 30, Color.FromArgb(49, 50, 68), Color.White);
            btnCuboidSetupKey.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnCuboidSetupKey.Click += delegate { StartListeningCuboidSetupKey(); };
            gDyn.Controls.Add(btnCuboidSetupKey);

            MakeLabel(gDyn, "Auto Y: +", 185, 68, new Font("Segoe UI", 9f, FontStyle.Bold), Color.FromArgb(249, 226, 175));
            numMakeDynamicIdx = MakeNumeric(gDyn, 255, 65, 55, 40, 500, makeDynamicOffsetY, 5);
            numMakeDynamicIdx.ValueChanged += delegate
            {
                makeDynamicOffsetY = (int)numMakeDynamicIdx.Value;
                SaveAttributesConfig();
            };
            MakeLabel(gDyn, "px", 315, 68, new Font("Segoe UI", 8.5f), Color.FromArgb(147, 153, 178));

            btnRunCuboidSetup = MakeBtn("⚡ TEST RUN (Dynamic + Density)", 435, 63, 220, 30,
                Color.FromArgb(166, 227, 161), Color.FromArgb(24, 24, 37));
            btnRunCuboidSetup.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnRunCuboidSetup.Click += delegate { ExecuteMakeDynamicAndDensity(); };
            gDyn.Controls.Add(btnRunCuboidSetup);

            Label lblDensityNote = new Label();
            lblDensityNote.Text = "💡  Dili na kinahanglan i-set mano-mano! Ang distansya gikan sa \"...\" kay automatic na.";
            lblDensityNote.Font = new Font("Segoe UI", 8.5f, FontStyle.Italic);
            lblDensityNote.ForeColor = Color.FromArgb(147, 153, 178);
            lblDensityNote.Location = new Point(15, 102);
            lblDensityNote.Size = new Size(645, 22);
            gDyn.Controls.Add(lblDensityNote);

            // ── Group 3: CUBOID ACTION HOTKEYS (Master ON/OFF) ───────────────────────
            GroupBox gActs = MakeGroup("3. Cuboid Action Hotkeys — 100% Automatic Direct Click (F4 = Master ON/OFF)", 0, 280, 675, 310);
            pnlCuboidTab.Controls.Add(gActs);

            btnCuboidMasterToggle = MakeBtn("", 15, 20, 645, 38, Color.FromArgb(166, 227, 161), Color.FromArgb(24, 24, 37));
            btnCuboidMasterToggle.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnCuboidMasterToggle.Click += delegate { ToggleCuboidMaster(); };
            gActs.Controls.Add(btnCuboidMasterToggle);
            UpdateCuboidMasterToggleUI();

            MakeLabel(gActs, "Action",   15,  65, new Font("Segoe UI", 8.5f, FontStyle.Bold), Color.FromArgb(147, 153, 178));
            MakeLabel(gActs, "Hotkey",  178,  65, new Font("Segoe UI", 8.5f, FontStyle.Bold), Color.FromArgb(147, 153, 178));
            MakeLabel(gActs, "Auto Click Offset Y", 285, 65, new Font("Segoe UI", 8.5f, FontStyle.Bold), Color.FromArgb(249, 226, 175));

            int actStartY = 84;
            int actStepY  = 50;

            for (int i = 0; i < 4; i++)
            {
                int idx  = i;
                int curY = actStartY + (i * actStepY);

                lblCuboidActName[i] = new Label();
                lblCuboidActName[i].Text      = cuboidActions[i].Name;
                lblCuboidActName[i].Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                lblCuboidActName[i].ForeColor = Color.FromArgb(137, 180, 250);
                lblCuboidActName[i].Location  = new Point(15, curY + 5);
                lblCuboidActName[i].Size      = new Size(160, 26);
                gActs.Controls.Add(lblCuboidActName[i]);

                btnCuboidActKey[i] = MakeBtn(string.Format("Key: {0}", FormatKeyName(cuboidActions[i].Hotkey)),
                    178, curY, 100, 30, Color.FromArgb(49, 50, 68), Color.White);
                btnCuboidActKey[i].Font  = new Font("Segoe UI", 9f, FontStyle.Bold);
                btnCuboidActKey[i].Click += delegate { StartListeningCuboidActionKey(idx); };
                gActs.Controls.Add(btnCuboidActKey[i]);

                MakeLabel(gActs, "+", 285, curY + 7, new Font("Segoe UI", 10f, FontStyle.Bold), Color.FromArgb(249, 226, 175));
                numCuboidActIndex[i] = MakeNumeric(gActs, 302, curY + 3, 56, 40, 500, cuboidActions[i].OffsetY, 2);
                numCuboidActIndex[i].ValueChanged += delegate
                {
                    cuboidActions[idx].OffsetY = (int)numCuboidActIndex[idx].Value;
                    SaveAttributesConfig();
                };
                MakeLabel(gActs, "px (Auto-Click)", 364, curY + 7, new Font("Segoe UI", 8.5f), Color.FromArgb(166, 227, 161));

                btnCuboidActTest[i] = MakeBtn("▶ Test", 565, curY, 90, 30, Color.FromArgb(166, 227, 161), Color.FromArgb(24, 24, 37));
                btnCuboidActTest[i].Font  = new Font("Segoe UI", 9f, FontStyle.Bold);
                btnCuboidActTest[i].Click += delegate { ExecuteCuboidAction(idx); };
                gActs.Controls.Add(btnCuboidActTest[i]);
            }
        }
        #endregion

        #region Cuboid Automations Logic
        private void ToggleCuboidMaster()
        {
            cuboidActionsEnabled = !cuboidActionsEnabled;
            UpdateCuboidMasterToggleUI();

            if (cuboidActionsEnabled)
            {
                RegisterCuboidHotkeys();
                lblStatus.Text = "[ CUBOID ACTIONS: ACTIVE ] Naka-ON na ang hotkeys! Pindota F4 para i-OFF.";
                lblStatus.ForeColor = Color.FromArgb(166, 227, 161);
                Beep(1400, 100);
            }
            else
            {
                UnregisterCuboidHotkeys();
                lblStatus.Text = "[ CUBOID ACTIONS: OFF ] Naka-OFF na! Hotkeys released. Pindota F4 para i-ON.";
                lblStatus.ForeColor = Color.FromArgb(243, 139, 168);
                Beep(700, 100);
            }

            SaveAttributesConfig();
        }

        private void UpdateCuboidMasterToggleUI()
        {
            if (cuboidActionsEnabled)
            {
                btnCuboidMasterToggle.Text = "🟢  CUBOID ACTIONS: ON  (Pindota F4 o i-click dinhi para i-OFF)";
                btnCuboidMasterToggle.BackColor = Color.FromArgb(166, 227, 161);
                btnCuboidMasterToggle.ForeColor = Color.FromArgb(24, 24, 37);
            }
            else
            {
                btnCuboidMasterToggle.Text = "🔴  CUBOID ACTIONS: OFF  (Hotkeys released | Pindota F4 o i-click para i-ON)";
                btnCuboidMasterToggle.BackColor = Color.FromArgb(69, 71, 90);
                btnCuboidMasterToggle.ForeColor = Color.FromArgb(205, 214, 244);
            }
        }

        private void SimulatePhysicalClick(int x, int y)
        {
            SetCursorPos(x, y);
            MovePhysicalMouse(x, y);
            Thread.Sleep(30);

            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            SendPhysicalClick(0, true);
            Thread.Sleep(35);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            SendPhysicalClick(0, false);
            Thread.Sleep(50);
        }

        private void ExecuteMakeDynamicAndDensity()
        {
            if (dotsPos.X == 0 && dotsPos.Y == 0)
            {
                lblStatus.Text = "[ WALA PA NA-SET ] I-set una ang posisyon sa \"...\" (3 Dots) button!";
                lblStatus.ForeColor = Color.FromArgb(243, 139, 168);
                Beep(400, 100);
                return;
            }

            POINT origPt;
            GetCursorPos(out origPt);

            int dynOffY = (numMakeDynamicIdx != null) ? (int)numMakeDynamicIdx.Value : makeDynamicOffsetY;
            int dynX = dotsPos.X - 130;
            int dynY = dotsPos.Y + dynOffY;

            // ── Step 1: Click "..." button ────────────────────────────────────────────
            SimulatePhysicalClick(dotsPos.X, dotsPos.Y);
            Thread.Sleep(120); // wait for dropdown

            // ── Step 2: Direct Mouse Click on "Make dynamic" ──────────────────────────
            SimulatePhysicalClick(dynX, dynY);
            Thread.Sleep(60);

            // ── Step 3: Restore cursor back to canvas ─────────────────────────────────
            SetCursorPos(origPt.X, origPt.Y);

            lblStatus.Text = string.Format("[ DYNAMIC ] Cuboid nahimong DYNAMIC na! (Key: {0})", FormatKeyName(cuboidSetupKey));
            lblStatus.ForeColor = Color.FromArgb(166, 227, 161);
            Beep(1600, 80);
        }

        private void ExecuteCuboidAction(int actionIndex)
        {
            if (actionIndex < 0 || actionIndex >= cuboidActions.Count) return;
            CuboidActionSlot act = cuboidActions[actionIndex];

            POINT origPt;
            GetCursorPos(out origPt);

            // For DELETE CUBOID: also send keyboard Delete key directly to active window
            if (actionIndex == 3)
            {
                byte vkDel = (byte)Keys.Delete;
                byte scanDel = (byte)MapVirtualKey(vkDel, 0);
                keybd_event(vkDel, scanDel, 0, UIntPtr.Zero);
                Thread.Sleep(15);
                keybd_event(vkDel, scanDel, 2, UIntPtr.Zero);
                Thread.Sleep(30);
            }

            if (dotsPos.X == 0 && dotsPos.Y == 0)
            {
                if (actionIndex == 3)
                {
                    lblStatus.Text = string.Format("[ ACTION: {0} ] Keyboard Delete sent! (Key: {1})",
                        act.Name, FormatKeyName(act.Hotkey));
                    lblStatus.ForeColor = Color.FromArgb(166, 227, 161);
                    Beep(1400, 60);
                    return;
                }
                lblStatus.Text = "[ WALA PA NA-SET ] Palihog i-set una ang posisyon sa \"...\" (3 Dots) button!";
                lblStatus.ForeColor = Color.FromArgb(243, 139, 168);
                Beep(400, 100);
                return;
            }

            int offY = act.OffsetY;
            if (numCuboidActIndex != null && numCuboidActIndex[actionIndex] != null)
                offY = (int)numCuboidActIndex[actionIndex].Value;

            // Delete Cuboid (index 3) is shorter text and needs to click further left (-125px)
            int offX = (actionIndex == 3) ? -125 : -95;
            int targetX = dotsPos.X + offX;
            int targetY = dotsPos.Y + offY;

            // 1. Click "..." button to open the dropdown menu
            SimulatePhysicalClick(dotsPos.X, dotsPos.Y);
            Thread.Sleep(120);

            // 2. Direct Mouse Click on the menu item!
            SimulatePhysicalClick(targetX, targetY);
            Thread.Sleep(60);

            // For Delete Cuboid: also send Enter key in case a confirm modal ("Are you sure?") appears
            if (actionIndex == 3)
            {
                byte vkEnter = (byte)Keys.Return;
                byte scanEnter = (byte)MapVirtualKey(vkEnter, 0);
                keybd_event(vkEnter, scanEnter, 0, UIntPtr.Zero);
                Thread.Sleep(15);
                keybd_event(vkEnter, scanEnter, 2, UIntPtr.Zero);
            }

            // 3. Restore cursor back to original canvas position
            Thread.Sleep(60);
            SetCursorPos(origPt.X, origPt.Y);

            lblStatus.Text = string.Format("[ ACTION: {0} ] Na-click sa ({1}, {2})! (Key: {3})",
                act.Name, targetX, targetY, FormatKeyName(act.Hotkey));
            lblStatus.ForeColor = Color.FromArgb(166, 227, 161);
            Beep(1400, 60);
        }

        private void ClickSinglePoint(Point pt)
        {
            if (pt.X == 0 && pt.Y == 0)
            {
                lblStatus.Text = "[ WALA PA NA-SET ] Posisyon wala pa ma-set!";
                lblStatus.ForeColor = Color.FromArgb(243, 139, 168);
                Beep(400, 100);
                return;
            }

            POINT origPt;
            GetCursorPos(out origPt);

            SetCursorPos(pt.X, pt.Y);
            Thread.Sleep(15);
            SendPhysicalClick(0, true);
            Thread.Sleep(20);
            SendPhysicalClick(0, false);

            Thread.Sleep(20);
            SetCursorPos(origPt.X, origPt.Y);

            lblStatus.Text = string.Format("[ CLICKED ] Gi-click ang X:{0}, Y:{1}", pt.X, pt.Y);
            lblStatus.ForeColor = Color.FromArgb(166, 227, 161);
            Beep(1200, 50);
        }

        private void RefreshCuboidActUI(int i)
        {
            if (i < 0 || i >= cuboidActions.Count) return;
            CuboidActionSlot act = cuboidActions[i];

            if (btnCuboidActKey[i] != null)
            {
                if (listeningCuboidActionIndex == i)
                {
                    btnCuboidActKey[i].Text = "[ Pindota Key... ]";
                    btnCuboidActKey[i].BackColor = Color.FromArgb(249, 226, 175);
                    btnCuboidActKey[i].ForeColor = Color.FromArgb(24, 24, 37);
                }
                else
                {
                    btnCuboidActKey[i].Text = string.Format("Key: {0}", FormatKeyName(act.Hotkey));
                    btnCuboidActKey[i].BackColor = Color.FromArgb(49, 50, 68);
                    btnCuboidActKey[i].ForeColor = Color.White;
                }
            }
        }

        private void StartListeningCuboidSetupKey()
        {
            isListeningCuboidSetupKey = true;
            btnCuboidSetupKey.Text = "[ Pindota Key... ]";
            btnCuboidSetupKey.BackColor = Color.FromArgb(249, 226, 175);
            btnCuboidSetupKey.ForeColor = Color.FromArgb(24, 24, 37);

            lblStatus.Text = "[ CHANGE SETUP KEY ] Pindota ang key para sa Cuboid Setup (ESC para cancel)";
            lblStatus.ForeColor = Color.FromArgb(249, 226, 175);

            this.Focus();
        }

        private void StartListeningCuboidActionKey(int index)
        {
            listeningCuboidActionIndex = index;
            btnCuboidActKey[index].Text = "[ Pindota Key... ]";
            btnCuboidActKey[index].BackColor = Color.FromArgb(249, 226, 175);
            btnCuboidActKey[index].ForeColor = Color.FromArgb(24, 24, 37);

            lblStatus.Text = string.Format("[ CHANGE KEY ] Pindota ang key para sa \"{0}\" (ESC para cancel)", cuboidActions[index].Name);
            lblStatus.ForeColor = Color.FromArgb(249, 226, 175);

            this.Focus();
        }
        #endregion

        #region Countdown Logic
        private void StartCountdown(int targetType, int index)
        {
            if (countdownTimer.Enabled) countdownTimer.Stop();

            countdownTargetType = targetType;
            countdownTargetIndex = index;
            countdownSeconds = 3;

            string targetName = GetTargetName(targetType, index);
            lblStatus.Text = string.Format("[ SETTING POS ] I-hover imong mouse sa \"{0}\" sulod sa 3 segundos! (3s)", targetName);
            lblStatus.ForeColor = Color.FromArgb(249, 226, 175);
            Beep(800, 80);

            UpdateCountdownButtonText(string.Format("({0}s)...", countdownSeconds), true);
            countdownTimer.Start();
        }

        private string GetTargetName(int targetType, int index)
        {
            if (targetType == 1 && index >= 0 && index < attrSlots.Count) return attrSlots[index].Name;
            if (targetType == 2) return "\"...\" 3 Dots Menu Button";
            return "Target";
        }

        private void UpdateCountdownButtonText(string txt, bool active)
        {
            Color bg = active ? Color.FromArgb(249, 226, 175) : Color.FromArgb(243, 139, 168);
            Color fg = Color.FromArgb(24, 24, 37);

            if (countdownTargetType == 1 && countdownTargetIndex >= 0 && countdownTargetIndex < 5)
            {
                btnSlotSet[countdownTargetIndex].Text = txt;
                btnSlotSet[countdownTargetIndex].BackColor = bg;
                btnSlotSet[countdownTargetIndex].ForeColor = fg;
            }
            else if (countdownTargetType == 2 && btnSetDots != null)
            {
                btnSetDots.Text = txt;
                btnSetDots.BackColor = bg;
                btnSetDots.ForeColor = fg;
            }
        }

        private void CountdownTick(object sender, EventArgs e)
        {
            countdownSeconds--;
            if (countdownSeconds > 0)
            {
                string targetName = GetTargetName(countdownTargetType, countdownTargetIndex);
                lblStatus.Text = string.Format("[ SETTING POS ] I-hover imong mouse sa \"{0}\"! ({1}s)", targetName, countdownSeconds);
                UpdateCountdownButtonText(string.Format("({0}s)...", countdownSeconds), true);
                Beep(800, 80);
            }
            else
            {
                countdownTimer.Stop();
                POINT pt;
                if (GetCursorPos(out pt))
                {
                    IntPtr target = WindowFromPoint(pt);
                    POINT clientPt = pt;
                    if (target != IntPtr.Zero) ScreenToClient(target, ref clientPt);

                    if (countdownTargetType == 1 && countdownTargetIndex >= 0 && countdownTargetIndex < attrSlots.Count)
                    {
                        AttributeSlot s = attrSlots[countdownTargetIndex];
                        s.ScreenX = pt.X;
                        s.ScreenY = pt.Y;
                        s.TargetHwnd = target;
                        s.ClientX = clientPt.X;
                        s.ClientY = clientPt.Y;
                        s.IsSet = true;
                        RefreshSlotUI(countdownTargetIndex);
                    }
                    else if (countdownTargetType == 2)
                    {
                        dotsPos = new Point(pt.X, pt.Y);
                        if (lblDotsPos != null) lblDotsPos.Text = FormatPoint(dotsPos);
                    }

                    Beep(1600, 200);
                    string targetName = GetTargetName(countdownTargetType, countdownTargetIndex);
                    lblStatus.Text = string.Format("[ NA-CAPTURE NA! ] Posisyon sa \"{0}\" na-set sa X:{1}, Y:{2}", targetName, pt.X, pt.Y);
                    lblStatus.ForeColor = Color.FromArgb(166, 227, 161);

                    UpdateCountdownButtonText("Set Pos (3s)", false);
                    SaveAttributesConfig();
                }

                countdownTargetType = 0;
                countdownTargetIndex = -1;
            }
        }
        #endregion

        #region Key Process & Hotkeys
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys k = keyData & Keys.KeyCode;

            if (isListeningNextKey)
            {
                isListeningNextKey = false;
                if (k != Keys.Escape)
                {
                    nextFrameKey = k;
                    btnNextKey.Text = string.Format("Next Key: {0}", FormatKeyName(nextFrameKey));
                    lblStatus.Text = string.Format("[ NEXT KEY ] Next Frame key na-set sa [{0}]!", FormatKeyName(nextFrameKey));
                    lblStatus.ForeColor = Color.FromArgb(166, 227, 161);
                    Beep(1200, 100);
                    SaveAttributesConfig();
                }
                else
                {
                    btnNextKey.Text = string.Format("Next Key: {0}", FormatKeyName(nextFrameKey));
                    lblStatus.Text = "[ CANCELLED ] Wala gi-ilisan ang Next Key.";
                    lblStatus.ForeColor = Color.FromArgb(249, 226, 175);
                }
                btnNextKey.BackColor = Color.FromArgb(49, 50, 68);
                btnNextKey.ForeColor = Color.White;
                return true;
            }

            if (listeningSlotIndex != -1)
            {
                int idx = listeningSlotIndex;
                listeningSlotIndex = -1;
                if (k != Keys.Escape)
                {
                    attrSlots[idx].Hotkey = k;
                    lblStatus.Text = string.Format("[ KEY NA-ILISAN ] Ang \"{0}\" gigapos na sa key [{1}]!",
                        attrSlots[idx].Name, FormatKeyName(k));
                    lblStatus.ForeColor = Color.FromArgb(166, 227, 161);
                    Beep(1200, 100);
                    if (attributesEnabled) RegisterAttributeHotkeys();
                    SaveAttributesConfig();
                }
                else
                {
                    lblStatus.Text = "[ CANCELLED ] Wala gi-ilisan ang key.";
                    lblStatus.ForeColor = Color.FromArgb(249, 226, 175);
                }
                RefreshSlotUI(idx);
                return true;
            }

            if (isListeningCuboidSetupKey)
            {
                isListeningCuboidSetupKey = false;
                if (k != Keys.Escape)
                {
                    cuboidSetupKey = k;
                    btnCuboidSetupKey.Text = string.Format("Key: {0}", FormatKeyName(cuboidSetupKey));
                    lblStatus.Text = string.Format("[ SETUP KEY ] Setup key na-set sa [{0}]!", FormatKeyName(cuboidSetupKey));
                    lblStatus.ForeColor = Color.FromArgb(166, 227, 161);
                    Beep(1200, 100);
                    if (cuboidActionsEnabled) RegisterCuboidHotkeys();
                    SaveAttributesConfig();
                }
                else
                {
                    btnCuboidSetupKey.Text = string.Format("Key: {0}", FormatKeyName(cuboidSetupKey));
                    lblStatus.Text = "[ CANCELLED ] Wala gi-ilisan ang Setup Key.";
                    lblStatus.ForeColor = Color.FromArgb(249, 226, 175);
                }
                btnCuboidSetupKey.BackColor = Color.FromArgb(49, 50, 68);
                btnCuboidSetupKey.ForeColor = Color.White;
                return true;
            }

            if (listeningCuboidActionIndex != -1)
            {
                int idx = listeningCuboidActionIndex;
                listeningCuboidActionIndex = -1;
                if (k != Keys.Escape)
                {
                    cuboidActions[idx].Hotkey = k;
                    lblStatus.Text = string.Format("[ ACTION KEY ] Ang \"{0}\" gigapos na sa key [{1}]!",
                        cuboidActions[idx].Name, FormatKeyName(k));
                    lblStatus.ForeColor = Color.FromArgb(166, 227, 161);
                    Beep(1200, 100);
                    if (cuboidActionsEnabled) RegisterCuboidHotkeys();
                    SaveAttributesConfig();
                }
                else
                {
                    lblStatus.Text = "[ CANCELLED ] Wala gi-ilisan ang action key.";
                    lblStatus.ForeColor = Color.FromArgb(249, 226, 175);
                }
                RefreshCuboidActUI(idx);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void RegisterHotkeys()
        {
            RegisterHotKey(this.Handle, HOTKEY_RECORD, 0, (uint)Keys.F8);
            RegisterHotKey(this.Handle, HOTKEY_PLAY, 0, (uint)Keys.F6);
            RegisterHotKey(this.Handle, HOTKEY_CLEAR, 0, (uint)Keys.F9);
            RegisterHotKey(this.Handle, HOTKEY_TOGGLE_ATTR, 0, (uint)Keys.F7);
            RegisterHotKey(this.Handle, HOTKEY_TOGGLE_CUBOID, 0, (uint)Keys.F4);

            if (attributesEnabled) RegisterAttributeHotkeys();
            if (cuboidActionsEnabled) RegisterCuboidHotkeys();
        }

        private void UnregisterHotkeys()
        {
            UnregisterHotKey(this.Handle, HOTKEY_RECORD);
            UnregisterHotKey(this.Handle, HOTKEY_PLAY);
            UnregisterHotKey(this.Handle, HOTKEY_CLEAR);
            UnregisterHotKey(this.Handle, HOTKEY_TOGGLE_ATTR);
            UnregisterHotKey(this.Handle, HOTKEY_TOGGLE_CUBOID);

            UnregisterAttributeHotkeys();
            UnregisterCuboidHotkeys();
        }

        private void RegisterAttributeHotkeys()
        {
            UnregisterAttributeHotkeys();
            if (!attributesEnabled) return;

            for (int i = 0; i < attrSlots.Count; i++)
            {
                if (attrSlots[i].Hotkey != Keys.None)
                {
                    RegisterHotKey(this.Handle, HOTKEY_ATTR_BASE + i, 0, (uint)attrSlots[i].Hotkey);
                }
            }
        }

        private void UnregisterAttributeHotkeys()
        {
            for (int i = 0; i < attrSlots.Count; i++)
            {
                UnregisterHotKey(this.Handle, HOTKEY_ATTR_BASE + i);
            }
        }

        private void RegisterCuboidHotkeys()
        {
            UnregisterCuboidHotkeys();
            if (!cuboidActionsEnabled) return;

            if (cuboidSetupKey != Keys.None)
            {
                RegisterHotKey(this.Handle, HOTKEY_CUBOID_SETUP, 0, (uint)cuboidSetupKey);
            }

            for (int i = 0; i < cuboidActions.Count; i++)
            {
                if (cuboidActions[i].Hotkey != Keys.None)
                {
                    RegisterHotKey(this.Handle, HOTKEY_CUBOID_BASE + i, 0, (uint)cuboidActions[i].Hotkey);
                }
            }
        }

        private void UnregisterCuboidHotkeys()
        {
            UnregisterHotKey(this.Handle, HOTKEY_CUBOID_SETUP);
            for (int i = 0; i < cuboidActions.Count; i++)
            {
                UnregisterHotKey(this.Handle, HOTKEY_CUBOID_BASE + i);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_RECORD) ToggleRecording();
                else if (id == HOTKEY_PLAY) TogglePlayback();
                else if (id == HOTKEY_CLEAR) ClearAll();
                else if (id == HOTKEY_TOGGLE_ATTR) ToggleAttributesMaster();
                else if (id == HOTKEY_TOGGLE_CUBOID) ToggleCuboidMaster();
                else if (id == HOTKEY_CUBOID_SETUP) ExecuteMakeDynamicAndDensity();
                else if (id >= HOTKEY_ATTR_BASE && id < HOTKEY_ATTR_BASE + attrSlots.Count)
                {
                    TriggerSlotClick(id - HOTKEY_ATTR_BASE);
                }
                else if (id >= HOTKEY_CUBOID_BASE && id < HOTKEY_CUBOID_BASE + cuboidActions.Count)
                {
                    ExecuteCuboidAction(id - HOTKEY_CUBOID_BASE);
                }
            }
            base.WndProc(ref m);
        }
        #endregion

        #region Tab 2: Attributes Logic
        private void ToggleAttributesMaster()
        {
            attributesEnabled = !attributesEnabled;
            UpdateMasterToggleUI();

            if (attributesEnabled)
            {
                RegisterAttributeHotkeys();
                lblStatus.Text = "[ ATTRIBUTES: ACTIVE ] Naka-ON na ang hotkeys! Pindota F7 para i-OFF.";
                lblStatus.ForeColor = Color.FromArgb(166, 227, 161);
                Beep(1400, 100);
            }
            else
            {
                UnregisterAttributeHotkeys();
                lblStatus.Text = "[ ATTRIBUTES: OFF ] Naka-OFF na! Gawasnon nimo magamit ang 1,2,3,4,5 sa ubang app. Pindota F7 para i-ON.";
                lblStatus.ForeColor = Color.FromArgb(243, 139, 168);
                Beep(700, 100);
            }

            SaveAttributesConfig();
        }

        private void UpdateMasterToggleUI()
        {
            if (attributesEnabled)
            {
                btnAttrMasterToggle.Text = "🟢  ATTRIBUTES: ON  (Pindota F7 o i-click dinhi para i-OFF)";
                btnAttrMasterToggle.BackColor = Color.FromArgb(166, 227, 161);
                btnAttrMasterToggle.ForeColor = Color.FromArgb(24, 24, 37);
            }
            else
            {
                btnAttrMasterToggle.Text = "🔴  ATTRIBUTES: OFF  (Hotkeys 1-5 released | Pindota F7 o i-click para i-ON)";
                btnAttrMasterToggle.BackColor = Color.FromArgb(69, 71, 90);
                btnAttrMasterToggle.ForeColor = Color.FromArgb(205, 214, 244);
            }
        }

        private void StartListeningKey(int slotIndex)
        {
            listeningSlotIndex = slotIndex;
            btnSlotKey[slotIndex].Text = "[ Pindota Key... ]";
            btnSlotKey[slotIndex].BackColor = Color.FromArgb(249, 226, 175);
            btnSlotKey[slotIndex].ForeColor = Color.FromArgb(24, 24, 37);

            lblStatus.Text = string.Format("[ CHANGE KEY ] Pindota bisan unsang key para sa \"{0}\" (ESC para cancel)", attrSlots[slotIndex].Name);
            lblStatus.ForeColor = Color.FromArgb(249, 226, 175);

            this.Focus();
        }

        private void StartListeningNextKey()
        {
            isListeningNextKey = true;
            btnNextKey.Text = "[ Pindota Key... ]";
            btnNextKey.BackColor = Color.FromArgb(249, 226, 175);
            btnNextKey.ForeColor = Color.FromArgb(24, 24, 37);

            lblStatus.Text = "[ CHANGE NEXT KEY ] Pindota ang key para sa Next Frame (e.g. +, D, Right Arrow) - ESC para cancel";
            lblStatus.ForeColor = Color.FromArgb(249, 226, 175);

            this.Focus();
        }

        private void TriggerSlotClick(int index)
        {
            if (index < 0 || index >= attrSlots.Count) return;
            AttributeSlot slot = attrSlots[index];

            if (!slot.IsSet)
            {
                lblStatus.Text = string.Format("[ WALA PA NA-SET ] Palihog i-set una ang posisyon sa \"{0}\"", slot.Name);
                lblStatus.ForeColor = Color.FromArgb(243, 139, 168);
                Beep(400, 100);
                return;
            }

            if (rbAttrPostMessage != null && rbAttrPostMessage.Checked)
            {
                IntPtr target = slot.TargetHwnd;
                if (!IsWindow(target))
                {
                    POINT sp = new POINT();
                    sp.X = slot.ScreenX;
                    sp.Y = slot.ScreenY;
                    target = WindowFromPoint(sp);
                }

                if (target != IntPtr.Zero)
                {
                    IntPtr lParam = MakeLParam(slot.ClientX, slot.ClientY);
                    PostMessage(target, WM_SETCURSOR, target, (IntPtr)((0x0200 << 16) | 1));
                    PostMessage(target, WM_MOUSEMOVE, IntPtr.Zero, lParam);
                    PostMessage(target, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, lParam);
                    Thread.Sleep(10);
                    PostMessage(target, WM_LBUTTONUP, IntPtr.Zero, lParam);
                }
            }
            else
            {
                POINT origPt;
                GetCursorPos(out origPt);

                SetCursorPos(slot.ScreenX, slot.ScreenY);
                Thread.Sleep(15);
                SendPhysicalClick(0, true);
                Thread.Sleep(20);
                SendPhysicalClick(0, false);

                if (chkRestoreCursor != null && chkRestoreCursor.Checked)
                {
                    Thread.Sleep(15);
                    SetCursorPos(origPt.X, origPt.Y);
                }
            }

            Beep(1300, 50);

            if (chkAutoNextFrame != null && chkAutoNextFrame.Checked)
            {
                int delay = numNextDelay != null ? (int)numNextDelay.Value : 100;
                if (delay > 0) Thread.Sleep(delay);

                SendNextFrameKey();

                lblStatus.Text = string.Format("[ CLICKED + NEXT ] \"{0}\" na-click ➔ Auto-abante sa Next Frame [{1}]!",
                    slot.Name, FormatKeyName(nextFrameKey));
                lblStatus.ForeColor = Color.FromArgb(166, 227, 161);
            }
            else
            {
                lblStatus.Text = string.Format("[ CLICKED ] Attribute \"{0}\" na-click! (Key: {1})",
                    slot.Name, FormatKeyName(slot.Hotkey));
                lblStatus.ForeColor = Color.FromArgb(166, 227, 161);
            }
        }

        private void SendNextFrameKey()
        {
            byte vk = (byte)nextFrameKey;
            byte scan = (byte)MapVirtualKey((uint)vk, 0);

            keybd_event(vk, scan, 0, UIntPtr.Zero);
            Thread.Sleep(30);
            keybd_event(vk, scan, 2, UIntPtr.Zero);
        }

        private void ClearSlot(int index)
        {
            if (index < 0 || index >= attrSlots.Count) return;
            AttributeSlot s = attrSlots[index];
            s.IsSet = false;
            s.ScreenX = 0;
            s.ScreenY = 0;
            s.ClientX = 0;
            s.ClientY = 0;
            s.TargetHwnd = IntPtr.Zero;

            RefreshSlotUI(index);
            SaveAttributesConfig();

            lblStatus.Text = string.Format("[ CLEARED ] Gi-tangtang ang posisyon sa \"{0}\".", s.Name);
            lblStatus.ForeColor = Color.FromArgb(249, 226, 175);
        }

        private void RefreshSlotUI(int i)
        {
            if (i < 0 || i >= attrSlots.Count) return;
            AttributeSlot s = attrSlots[i];

            if (btnSlotKey[i] != null)
            {
                if (listeningSlotIndex == i)
                {
                    btnSlotKey[i].Text = "[ Pindota Key... ]";
                    btnSlotKey[i].BackColor = Color.FromArgb(249, 226, 175);
                    btnSlotKey[i].ForeColor = Color.FromArgb(24, 24, 37);
                }
                else
                {
                    btnSlotKey[i].Text = string.Format("Key: {0}", FormatKeyName(s.Hotkey));
                    btnSlotKey[i].BackColor = Color.FromArgb(49, 50, 68);
                    btnSlotKey[i].ForeColor = Color.White;
                }
            }

            if (lblSlotPos[i] != null)
            {
                lblSlotPos[i].Text = s.IsSet ? string.Format("X: {0}, Y: {1}", s.ScreenX, s.ScreenY) : "[ Wala pa na-set ]";
                lblSlotPos[i].ForeColor = s.IsSet ? Color.FromArgb(166, 227, 161) : Color.FromArgb(108, 112, 134);
            }

            if (btnSlotTest[i] != null) btnSlotTest[i].Enabled = s.IsSet;
            if (btnSlotClear[i] != null) btnSlotClear[i].Enabled = s.IsSet;
        }
        #endregion

        #region Helpers & Config
        private string FormatPoint(Point pt)
        {
            return (pt.X == 0 && pt.Y == 0) ? "[ Wala pa na-set ]" : string.Format("X: {0}, Y: {1}", pt.X, pt.Y);
        }

        private string FormatKeyName(Keys k)
        {
            switch (k)
            {
                case Keys.D0: return "0";
                case Keys.D1: return "1";
                case Keys.D2: return "2";
                case Keys.D3: return "3";
                case Keys.D4: return "4";
                case Keys.D5: return "5";
                case Keys.D6: return "6";
                case Keys.D7: return "7";
                case Keys.D8: return "8";
                case Keys.D9: return "9";
                case Keys.NumPad0: return "Num 0";
                case Keys.NumPad1: return "Num 1";
                case Keys.NumPad2: return "Num 2";
                case Keys.NumPad3: return "Num 3";
                case Keys.NumPad4: return "Num 4";
                case Keys.NumPad5: return "Num 5";
                case Keys.NumPad6: return "Num 6";
                case Keys.NumPad7: return "Num 7";
                case Keys.NumPad8: return "Num 8";
                case Keys.NumPad9: return "Num 9";
                case Keys.Oemplus: return "+";
                case Keys.Add: return "Num +";
                case Keys.OemMinus: return "-";
                case Keys.Subtract: return "Num -";
                default: return k.ToString();
            }
        }

        private void SaveAttributesConfig()
        {
            try
            {
                List<string> lines = new List<string>();
                lines.Add("Enabled=" + attributesEnabled.ToString());
                lines.Add("Mode=" + ((rbAttrPhysical != null && rbAttrPhysical.Checked) ? "Physical" : "PostMessage"));
                lines.Add("Restore=" + ((chkRestoreCursor != null && chkRestoreCursor.Checked) ? "True" : "False"));
                lines.Add("AutoNext=" + ((chkAutoNextFrame != null && chkAutoNextFrame.Checked) ? "True" : "False"));
                lines.Add("NextKey=" + ((int)nextFrameKey).ToString());
                lines.Add("NextDelay=" + (numNextDelay != null ? ((int)numNextDelay.Value).ToString() : nextFrameDelay.ToString()));

                for (int i = 0; i < attrSlots.Count; i++)
                {
                    AttributeSlot s = attrSlots[i];
                    lines.Add(string.Format("Slot{0}={1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}",
                        i, s.Name, (int)s.Hotkey, s.ScreenX, s.ScreenY, s.TargetHwnd.ToInt64(), s.ClientX, s.ClientY, s.IsSet));
                }

                lines.Add("CuboidEnabled=" + cuboidActionsEnabled.ToString());
                lines.Add(string.Format("DotsPos={0}|{1}", dotsPos.X, dotsPos.Y));
                lines.Add("CuboidSetupKey=" + ((int)cuboidSetupKey).ToString());
                int dynMenuIdx = (numMakeDynamicIdx != null) ? (int)numMakeDynamicIdx.Value : makeDynamicOffsetY;
                lines.Add("MakeDynamicOffsetY=" + dynMenuIdx.ToString());

                for (int i = 0; i < cuboidActions.Count; i++)
                {
                    CuboidActionSlot act = cuboidActions[i];
                    int savedIdx = (numCuboidActIndex[i] != null) ? (int)numCuboidActIndex[i].Value : act.OffsetY;
                    lines.Add(string.Format("CuboidAct{0}={1}|{2}|{3}",
                        i, act.Name, (int)act.Hotkey, savedIdx));
                }

                File.WriteAllLines(configPath, lines.ToArray());
            }
            catch { }
        }

        private void LoadAttributesConfig()
        {
            try
            {
                if (!File.Exists(configPath)) return;
                string[] lines = File.ReadAllLines(configPath);
                foreach (string rawLine in lines)
                {
                    if (string.IsNullOrEmpty(rawLine)) continue;
                    int eq = rawLine.IndexOf('=');
                    if (eq < 0) continue;
                    string key = rawLine.Substring(0, eq).Trim();
                    string val = rawLine.Substring(eq + 1).Trim();

                    if (key == "Enabled")
                    {
                        bool en;
                        if (bool.TryParse(val, out en)) attributesEnabled = en;
                    }
                    else if (key == "Mode")
                    {
                        if (val == "PostMessage" && rbAttrPostMessage != null) rbAttrPostMessage.Checked = true;
                        else if (rbAttrPhysical != null) rbAttrPhysical.Checked = true;
                    }
                    else if (key == "Restore" && chkRestoreCursor != null)
                    {
                        bool rst;
                        if (bool.TryParse(val, out rst)) chkRestoreCursor.Checked = rst;
                    }
                    else if (key == "AutoNext")
                    {
                        bool an;
                        if (bool.TryParse(val, out an))
                        {
                            autoNextFrame = an;
                            if (chkAutoNextFrame != null) chkAutoNextFrame.Checked = an;
                        }
                    }
                    else if (key == "NextKey")
                    {
                        int nk;
                        if (int.TryParse(val, out nk)) nextFrameKey = (Keys)nk;
                    }
                    else if (key == "NextDelay")
                    {
                        int nd;
                        if (int.TryParse(val, out nd))
                        {
                            nextFrameDelay = nd;
                            if (numNextDelay != null) numNextDelay.Value = Math.Max(numNextDelay.Minimum, Math.Min(numNextDelay.Maximum, nd));
                        }
                    }
                    else if (key.StartsWith("Slot"))
                    {
                        int sIdx;
                        if (int.TryParse(key.Substring(4), out sIdx) && sIdx >= 0 && sIdx < attrSlots.Count)
                        {
                            string[] parts = val.Split('|');
                            if (parts.Length >= 9)
                            {
                                attrSlots[sIdx].Name = parts[0];
                                int kInt;
                                if (int.TryParse(parts[1], out kInt)) attrSlots[sIdx].Hotkey = (Keys)kInt;
                                int.TryParse(parts[2], out attrSlots[sIdx].ScreenX);
                                int.TryParse(parts[3], out attrSlots[sIdx].ScreenY);
                                long h;
                                if (long.TryParse(parts[4], out h)) attrSlots[sIdx].TargetHwnd = (IntPtr)h;
                                int.TryParse(parts[5], out attrSlots[sIdx].ClientX);
                                int.TryParse(parts[6], out attrSlots[sIdx].ClientY);
                                bool isS;
                                if (bool.TryParse(parts[7], out isS)) attrSlots[sIdx].IsSet = isS;
                            }
                        }
                    }
                    else if (key == "CuboidEnabled")
                    {
                        bool ce;
                        if (bool.TryParse(val, out ce)) cuboidActionsEnabled = ce;
                    }
                    else if (key == "DotsPos")
                    {
                        string[] p = val.Split('|');
                        if (p.Length >= 2)
                        {
                            int x, y;
                            if (int.TryParse(p[0], out x) && int.TryParse(p[1], out y)) dotsPos = new Point(x, y);
                        }
                    }
                    else if (key == "CuboidSetupKey")
                    {
                        int sk;
                        if (int.TryParse(val, out sk)) cuboidSetupKey = (Keys)sk;
                    }
                    else if (key == "MakeDynamicIdx" || key == "MakeDynamicOffsetY")
                    {
                        int mi;
                        if (int.TryParse(val, out mi) && mi >= 40 && mi <= 500) makeDynamicOffsetY = mi;
                    }
                    else if (key.StartsWith("CuboidAct"))
                    {
                        int actIdx;
                        if (int.TryParse(key.Substring(9), out actIdx) && actIdx >= 0 && actIdx < cuboidActions.Count)
                        {
                            string[] p = val.Split('|');
                            if (p.Length >= 3)
                            {
                                cuboidActions[actIdx].Name = p[0];
                                int kInt;
                                if (int.TryParse(p[1], out kInt)) cuboidActions[actIdx].Hotkey = (Keys)kInt;
                                int mIdx;
                                if (int.TryParse(p[2], out mIdx) && mIdx >= 40 && mIdx <= 500) cuboidActions[actIdx].OffsetY = mIdx;
                            }
                        }
                    }
                }
            }
            catch { }
        }

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

        private Image LoadBackgroundImage()
        {
            try
            {
                string bgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bg.jpg");
                if (File.Exists(bgPath))
                {
                    using (FileStream fs = new FileStream(bgPath, FileMode.Open, FileAccess.Read))
                    {
                        return Image.FromStream(fs);
                    }
                }

                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream("bg.jpg"))
                {
                    if (stream != null) return Image.FromStream(stream);
                }
            }
            catch { }
            return null;
        }

        private Icon LoadAppIcon()
        {
            try
            {
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(icoPath))
                {
                    return new Icon(icoPath);
                }

                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream("app.ico"))
                {
                    if (stream != null) return new Icon(stream);
                }
            }
            catch { }
            return null;
        }

        private GroupBox MakeGroup(string t, int x, int y, int w, int h)
        {
            GroupBox g = new GroupBox();
            g.Text = t;
            g.Location = new Point(x, y);
            g.Size = new Size(w, h);
            g.ForeColor = Color.FromArgb(186, 194, 222);
            g.BackColor = Color.Transparent;
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
            if (val < min) val = min;
            if (val > max) val = max;
            n.Value = val;
            n.Increment = inc;
            n.BackColor = Color.FromArgb(49, 50, 68);
            n.ForeColor = Color.White;
            parent.Controls.Add(n);
            return n;
        }
        #endregion

        #region Hover Recording & Playback
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
                    if (target != IntPtr.Zero) ScreenToClient(target, ref clientPt);

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

                Thread.Sleep(25);
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

        private void TogglePlayback()
        {
            if (isRecording) StopRecording();

            if (isPlaying) StopPlayback();
            else StartPlayback();
        }

        private void StartPlayback()
        {
            if (frames.Count == 0)
            {
                MessageBox.Show("Wala pay na-record!\n\n1. F8 start record\n2. Lihok mouse\n3. F8 stop\n4. F6 play!",
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
                lblStatus.Text = "[ PLAYING BACKGROUND ]  Dili mo-lihok imong mouse! Pwede Alt+Tab. F6 para hunong.";
                lblStatus.ForeColor = Color.FromArgb(166, 227, 161);
            }
            else
            {
                lblStatus.Text = "[ PLAYING CURSOR ]  Mouse is auto-moving! F6 o ESC para hunong.";
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
                    if ((GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0)
                    {
                        isPlaying = false;
                        break;
                    }

                    RecordedFrame f = frames[i];
                    frameCounter++;

                    if (isBackgroundMode)
                    {
                        IntPtr target = f.TargetHwnd;
                        if (!IsWindow(target))
                        {
                            POINT sp = new POINT();
                            sp.X = f.ScreenX;
                            sp.Y = f.ScreenY;
                            target = WindowFromPoint(sp);
                        }

                        if (target != IntPtr.Zero)
                        {
                            IntPtr lParam = MakeLParam(f.ClientX, f.ClientY);

                            PostMessage(target, WM_SETCURSOR, target, (IntPtr)((0x0200 << 16) | 1));
                            PostMessage(target, WM_MOUSEMOVE, IntPtr.Zero, lParam);

                            if (f.IsLeftClick)
                            {
                                PostMessage(target, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, lParam);
                                Interlocked.Increment(ref totalClicks);
                            }
                            if (f.IsLeftRelease) PostMessage(target, WM_LBUTTONUP, IntPtr.Zero, lParam);
                            if (f.IsRightClick)
                            {
                                PostMessage(target, WM_RBUTTONDOWN, (IntPtr)MK_RBUTTON, lParam);
                                Interlocked.Increment(ref totalClicks);
                            }
                            if (f.IsRightRelease) PostMessage(target, WM_RBUTTONUP, IntPtr.Zero, lParam);

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

                    if (i + 1 < frames.Count && isPlaying)
                    {
                        long delta = frames[i + 1].TimestampMs - f.TimestampMs;
                        int sleepMs = (int)(delta / speedFactor);
                        if (sleepMs > 0 && sleepMs < 10000) Thread.Sleep(sleepMs);
                    }
                }

                if (!loopForever && loopsDone >= maxLoops) break;
                if (isPlaying && loopDelayMs > 0) Thread.Sleep(loopDelayMs);
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

            if (btn == 1)
                inputs[0].mi.dwFlags = isDown ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP;
            else if (btn == 2)
                inputs[0].mi.dwFlags = isDown ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP;
            else
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
