using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Windows.Automation;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using static UIActrl.Navigate;
using static UIActrl.ModedOperate;

using Size = System.Drawing.Size;
using Point = System.Drawing.Point;

using MK = System.Windows.Input.ModifierKeys;
using AutoElem = System.Windows.Automation.AutomationElement;

namespace UIActrl {
    public partial class Marker : Form {
        #region API
        #region HotKey
        [DllImport("user32")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32")] static extern bool RegisterHotKey(
            IntPtr hWnd, int id, MK fsModifiers, Keys vk);
        bool RegHotkey(int id, MK fsModifiers, Keys vk) {
            if (RegisterHotKey(this.Handle, id, fsModifiers, vk)) {
                this.FormClosed +=
                    (object sender, FormClosedEventArgs e) =>
                        UnregisterHotKey(this.Handle, id);
                return true;
            }
            return false;
        }
        #endregion
        #region WindowLong
        [DllImport("user32")] static extern int GetWindowLong(
             IntPtr hwnd, int nIndex);
        [DllImport("user32")] static extern uint SetWindowLong(
            IntPtr hwnd, int nIndex, int dwNewLong);
        #endregion
        [DllImport("user32")] static extern IntPtr GetDesktopWindow();
        [DllImport("user32")] static extern IntPtr GetForegroundWindow();
        #endregion
        #region 启动
        protected override CreateParams CreateParams {
            get {
                base.CreateParams.ExStyle = base.CreateParams.ExStyle
                    & ~0x40000 | 0x20 | 0x80000 | 0x08000000;
                return base.CreateParams;
            }
        }
        readonly CountedNavi Nav;
        public Marker() {
            InitializeComponent();
            SetWindowLong(this.Handle, -20, GetWindowLong(this.Handle, -20)
                 | 0x20 | 0x80000 | 0x08000000); //TRANSPARENT,LAYERED,NOACTIVATE
            Rectangle ScrBnd = Screen.PrimaryScreen.Bounds;
            Location = new Point(ScrBnd.Right, ScrBnd.Bottom);
            Size = ScrBnd.Size;
            ClearMarks();
            Location = new Point(0, 0);

            Nav = new CountedNavi(
                AutoElem.FromHandle(GetDesktopWindow())
            );
            Nav.Walker = TreeWalker.ControlViewWalker;
            Nav.CurElemChanged += MarkElemInfo;

            RegHotkey(0, MK.Alt | MK.Control, Keys.Up);
            RegHotkey(1, MK.Alt | MK.Control, Keys.Down);
            RegHotkey(2, MK.Alt | MK.Control, Keys.Left);
            RegHotkey(3, MK.Alt | MK.Control, Keys.Right);
            RegHotkey(6, MK.Alt | MK.Control, Keys.D1);

            RegHotkey(7, MK.Alt | MK.Control, Keys.D2);
            RegHotkey(5, MK.Alt | MK.Control, Keys.Enter);
        }
        #endregion
        #region 操纵
        readonly ModedOperate Oper = new ModedOperate(OpMode.Focus);
        protected override void WndProc(ref Message m) {
            if (m.Msg == 0x0312) { //WM_HOTKEY
                switch (m.WParam.ToInt32()) {
                case 0: case 1: case 2: case 3:
                    var t = Nav.NavElem((NavCode)m.WParam);
                    if (t != null) Console.WriteLine(t);
                    break;
                case 5:
                    if (Nav.CondMode) {
                        MarkedOp(Nav.CurElem);
                    } else {
                        Nav.Walker = new TreeWalker(Oper.PropCond);
                        int v = Nav.CondSibLst.Count;
                        Console.WriteLine("Set Walker " + (v > 0 ? "Succeed" : "Failed"));
                        if (v > 0) {
                            if (v == 1 || (Oper.Mode == OpMode.Focus && v < 5)) {
                                MarkedOp(Nav.CurElem);
                                Nav.NavElem(NavCode.Parent);
                            } else {
                                ShowInput();
                            }
                        }
                    }
                    break;
                case 6:
                    Nav.CurElem = AutoElem.FromHandle(GetForegroundWindow());
                    Nav.NavElem(NavCode.Child);
                    break;
                case 7:
                    ShowInput();
                    break;
                }
            }
            base.WndProc(ref m);
        }
        void MarkedOp(AutoElem elem, bool ForceRefresh = false) {
            Console.WriteLine("Op" + (Oper.Operate(elem) ? "Suces" : "Fail"));
            Nav.VerifySibList(ForceRefresh);
            ClearMarks();
            MarkElemInfo(elem);
        }
        static readonly Dictionary<string, OpMode> Cmd_Oper_Map =
            new Dictionary<string, OpMode> {
            { "st", OpMode.ScrollTo },
            { "i", OpMode.Invoke },
            { "t", OpMode.Toggle },
            { "f", OpMode.Focus },
            { "e", OpMode.Expand },
            { "d", OpMode.Dock },
            { "s", OpMode.Scroll },
            { "p", OpMode.ScrollPercent },
            { "se", OpMode.Select },
            { "so", OpMode.SelectOne },
        };
        static readonly Dictionary<string, object> Cmd_Pos_Map =
            new Dictionary<string, object> {
            { "t", DockPosition.Top },
            { "l", DockPosition.Left },
            { "b", DockPosition.Bottom },
            { "r", DockPosition.Right },
            { "n", DockPosition.None },
            { "f", DockPosition.Fill },
            { "上", DockPosition.Top },
            { "左", DockPosition.Left },
            { "下", DockPosition.Bottom },
            { "右", DockPosition.Right },
            { "无", DockPosition.None },
            { "满", DockPosition.Fill },
            { "s", DockPosition.Top },
            { "x", DockPosition.Left },
            { "z", DockPosition.Bottom },
            { "y", DockPosition.Right },
            { "w", DockPosition.None },
            { "m", DockPosition.Fill },
        };
        static readonly Dictionary<string, object> Cmd_Amount_Map =
            new Dictionary<string, object> {
            { "pu", ScrollAmount.LargeDecrement },
            { "lu", ScrollAmount.SmallDecrement },
            { "pd", ScrollAmount.LargeIncrement },
            { "ld", ScrollAmount.SmallIncrement },
            { "上页", ScrollAmount.LargeDecrement },
            { "上行", ScrollAmount.SmallDecrement },
            { "下页", ScrollAmount.LargeIncrement },
            { "下行", ScrollAmount.SmallIncrement },
            { "sy", ScrollAmount.LargeDecrement },
            { "sh", ScrollAmount.SmallDecrement },
            { "xy", ScrollAmount.LargeIncrement },
            { "xh", ScrollAmount.SmallIncrement },
        };
        void ResetInput() {
            toolStripDropDownButton1.HideDropDown();
            toolStripTextBox1.Text = "";
        }
        void ShowInput() {
            toolStripDropDownButton1.ShowDropDown();
            toolStripTextBox1.Focus();
        }
        private void ToolStripTextBox1_TextChanged(object sender, EventArgs e) {
            string text = toolStripTextBox1.Text;
            int length = text.Length;
            Console.WriteLine("TextChanged");
            if (length <= 1) return;
            if (text[length - 1] == ';') {
                string cmd = text.CutButt(1);
                if (int.TryParse(cmd, out int t)) {
                    if (Nav.CondMode) {
                        ResetInput();
                        MarkedOp(Nav.CondSibLst[t], Oper.Mode == OpMode.Expand);
                        if (Nav.CondMode) Nav.CondSibLst.Pointer = t;
                    }
                } else {
                    ResetInput();
                    string[] sp = cmd.Split(' ');
                    Oper.Mode = Cmd_Oper_Map[sp[0]];
                    if (sp.Length > 1) {
                        switch (sp[0]) {
                        case "d":
                            Oper.Args = new[] { Cmd_Pos_Map[sp[1]] };
                            break;
                        case "s":
                            Oper.Args = new[] { Cmd_Amount_Map[sp[1]], sp.Length > 2 };
                            break;
                        case "p":
                            Oper.Args = new object[] { double.Parse(sp[1]), sp.Length > 2 };
                            break;
                        }
                    }
                }
            }
        }
        #endregion
        #region 画图
        [DllImport("gdi32")] static extern bool DeleteObject(IntPtr hObject);
        [DllImport("user32")] static extern bool UpdateLayeredWindow(
            IntPtr hWnd,
            IntPtr hdcDst, IntPtr pptDst, ref Size psize,
            IntPtr hdcSrc, ref Point pptSrc,
            long crKey, ref BLENDFUNCTION pblend, long dwFlags
        );
        struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
            public BLENDFUNCTION(byte SourceConstantAlpha) 
            {  
                this.SourceConstantAlpha = SourceConstantAlpha;
                BlendOp     = 1;  //AC_SRC_OVER
                BlendFlags  = 0;  //必0
                AlphaFormat = 1;  //AC_SRC_ALPHA
            }
        }
        void MarkElemInfo(AutoElem e) {
            Graphics ImgGraph = Graphics.FromImage(new Bitmap(this.Width, this.Height));
            IntPtr ImgHdc = ImgGraph.GetHdc();
            
            Graphics g = Graphics.FromHdc(ImgHdc);
            g.Clear(Color.Transparent);
            if (Nav.CondMode) {
                var t = Nav.CondSibLst.List;
                DrawElemsBounds(t, g, Color.Black);
                DrawElemBound(e, g, Color.Gold, null);
            } else {
                DrawElemBound(e, g, Color.Black,
                    $"↓{Nav.CurChildCnt}  ↔{Nav.CurSibCnt}");
            }

            Graphics FormGraph = Graphics.FromHwnd(this.Handle);
            IntPtr WinHdc = FormGraph.GetHdc();
            BLENDFUNCTION blend = new BLENDFUNCTION(255);
            Point p = new Point(0, 0);
            Size s = new Size(Width, Height);
            UpdateLayeredWindow(this.Handle,
                WinHdc, IntPtr.Zero, ref s,
                ImgHdc, ref p,
                0, ref blend, 2);
        }
        void DrawElemBound(AutoElem ae, Graphics g, Color ForeColor, string Text) {
            if (ae == null || !ae.Available()) return;

            Rect BudRc = ae.Current.BoundingRectangle;
            if (BudRc == Rect.Empty) return;
            var ClientRc = this.RectangleToClient(BudRc.ToRectangle());

            var ForeBrush = new SolidBrush(ForeColor);
            var ForePen = new Pen(ForeBrush, 3);
            var BackBrush = new SolidBrush(ForeColor.RevColor());
            var BackPen = new Pen(BackBrush, 5);
            
            g.DrawRectangle(BackPen, ClientRc);
            g.DrawRectangle(ForePen, ClientRc);

            if (Text != "" && Text != null) {
                GraphicsPath gp = new GraphicsPath();
                gp.AddString(Text, this.Font.FontFamily, (int)Font.Style, Font.Size,
                    new Point(ClientRc.Left, ClientRc.Top), StringFormat.GenericDefault);
                var ogp = (GraphicsPath)gp.Clone();
                gp.Widen(BackPen);
                g.FillPath(BackBrush, gp);
                g.FillPath(ForeBrush, ogp);
            }
        }
        void DrawElemsBounds(List<AutoElem> ae, Graphics g, Color ForeColor) {
            if (ae == null) return;

            var ForeBrush = new SolidBrush(ForeColor);
            var ForePen = new Pen(ForeBrush, 3);
            var BackBrush = new SolidBrush(ForeColor.RevColor());
            var BackPen = new Pen(BackBrush, 5);

            Region rg = new Region(); rg.MakeEmpty();
            List<Rectangle> lst = new List<Rectangle>();
            for (int i = 0; i < ae.Count; i++) {
                Rect BudRc = ae[i].Current.BoundingRectangle;
                Rectangle ClientRc;
                if (BudRc != Rect.Empty) {
                    ClientRc = this.RectangleToClient(BudRc.ToRectangle());
                    g.DrawRectangle(BackPen, ClientRc);
                    g.DrawRectangle(ForePen, ClientRc);
                    rg.Union(ClientRc);
                } else {
                    ClientRc = Rectangle.Empty;
                }
                lst.Add(ClientRc);
            }

            GraphicsPath gp = new GraphicsPath();
            gp.AddString("0",
                    this.Font.FontFamily, (int)this.Font.Style, this.Font.Size,
                    new Point(0, 0), StringFormat.GenericDefault);
            //gp.Widen(new Pen(ForeBrush, 7));
            var TxtBnd = gp.GetBounds(); gp.Reset();
            SizeF gpS = TxtBnd.Size;
            Point TxtOffset = new Point(
                (int)-Math.Ceiling(TxtBnd.X),
                (int)-Math.Ceiling(TxtBnd.Y));
            Func<SizeF,int,Size> bt = (a,b) => 
                new Size((int)Math.Ceiling(a.Width * b),
                (int)Math.Ceiling(a.Height));
            Size[] TxtSizes = { bt(gpS, 0), bt(gpS, 1), bt(gpS, 2), bt(gpS, 3) };

            for (int i = 0; i < lst.Count; i++) {
                if (lst[i].IsEmpty) continue;
                var t = GetTextPlace(rg, lst[i], TxtSizes[i.ToString().Length]);
                rg.Union(t);
                t.Offset(TxtOffset);
                gp.AddString(i.ToString(),
                    this.Font.FontFamily, (int)Font.Style, Font.Size,
                    new Point(t.Left, t.Top), StringFormat.GenericDefault);
            }
            g.SmoothingMode = SmoothingMode.HighQuality;
            var ogp = (GraphicsPath)gp.Clone();
            gp.Widen(BackPen);
            g.FillPath(BackBrush, gp);
            g.FillPath(ForeBrush, ogp);
        }
        Rectangle GetTextPlace(Region Blocked, Rectangle ToText, Size Txt) {
            Rectangle rc = new Rectangle(0, 0, Txt.Width, Txt.Height);
            //rc.Location = ToText.Location; return rc;
            int inLeft = ToText.Left;    int outLeft = inLeft - Txt.Width;
            int inTop = ToText.Top;      int outTop = inTop - Txt.Height;
            int outRight = ToText.Right; int inRight = outRight - Txt.Width;
            int midWidth = (outLeft + outRight) / 2;
            int botm = ToText.Bottom;

            rc.Location = new Point(midWidth, outTop);
                if (!Blocked.IsVisible(rc)) return rc;
            rc.Location = new Point(midWidth, botm);
                if (!Blocked.IsVisible(rc)) return rc;

            rc.Location = new Point(inLeft, outTop);
                if (!Blocked.IsVisible(rc)) return rc;
            rc.Location = new Point(inLeft, botm);
                if (!Blocked.IsVisible(rc)) return rc;

            rc.Location = new Point(inRight, outTop);
                if (!Blocked.IsVisible(rc)) return rc;
            rc.Location = new Point(inRight, botm);
                if (!Blocked.IsVisible(rc)) return rc;

            rc.Location = new Point(outLeft, inTop);
                if (!Blocked.IsVisible(rc)) return rc;
            rc.Location = new Point(outRight, inTop);
                if (!Blocked.IsVisible(rc)) return rc;

            rc.Location = new Point(outLeft, outTop);
                if (!Blocked.IsVisible(rc)) return rc;
            rc.Location = new Point(outLeft, botm);
                if (!Blocked.IsVisible(rc)) return rc;
            rc.Location = new Point(outRight, outTop);
                if (!Blocked.IsVisible(rc)) return rc;
            rc.Location = new Point(outRight, botm);
                if (!Blocked.IsVisible(rc)) return rc;

            rc.Location = ToText.Location;
            return rc;
        }
        void ClearMarks() {
            Graphics ImgGraph = Graphics.FromImage(new Bitmap(this.Width, this.Height));
            IntPtr ImgHdc = ImgGraph.GetHdc();
            
            Graphics g = Graphics.FromHdc(ImgHdc);
            g.Clear(Color.Transparent);

            Graphics FormGraph = Graphics.FromHwnd(this.Handle);
            IntPtr WinHdc = FormGraph.GetHdc();
            BLENDFUNCTION blend = new BLENDFUNCTION(255);
            Point p = new Point(0, 0);
            Size s = new Size(Width, Height);
            UpdateLayeredWindow(this.Handle,
                WinHdc, IntPtr.Zero, ref s,
                ImgHdc, ref p,
                0, ref blend, 2);
        }
        #endregion
    }
}
