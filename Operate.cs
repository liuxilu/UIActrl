using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Automation;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Point = System.Drawing.Point;

using AutoElem = System.Windows.Automation.AutomationElement;
using PropCond = System.Windows.Automation.PropertyCondition;

namespace UIActrl {
    public static class Operaters {
        [DllImport("user32")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32")] static extern bool SetCursorPos(int X, int Y);
        static bool SetCursorPos(Point pt) => SetCursorPos(pt.X, pt.Y);
        [DllImport("user32")] static extern void mouse_event(
            uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);
            //MOUSEEVENTF_XXX
            const uint ME_MOVE        = 0x0001;
            const uint ME_LEFTDOWN    = 0x0002;
            const uint ME_LEFTUP      = 0x0004;
            const uint ME_RIGHTDOWN   = 0x0008;
            const uint ME_RIGHTUP     = 0x0010;
            const uint ME_MIDDLEDOWN  = 0x0020;
            const uint ME_MIDDLEUP    = 0x0040;
            const uint ME_XDOWN       = 0x0080;
            const uint ME_XUP         = 0x0100;
            const uint ME_WHEEL       = 0x0800;
            const uint ME_VIRTUALDESK = 0x4000;
            const uint ME_ABSOLUTE    = 0x8000;
        private static void LeftClick(Point pt) {
            mouse_event(ME_LEFTDOWN, pt.X, pt.Y, 0, 0);
            mouse_event(ME_LEFTUP, pt.X, pt.Y, 0, 0);
        }

        /*private static void OpElem(AutoElem e) {

            /*var Center = ((Rect)last.GetCurrentPropertyValue(
            AutomationElement.BoundingRectangleProperty)).Center();
            Console.WriteLine(Center);
            SetCursorPos(Center);
            LeftClick(Center); LeftClick(Center);
            foreach (var i in e.GetSupportedPatterns()) {
                Console.WriteLine($"{i.Id}: {i.ProgrammaticName}");
            }
        }*/
        public static bool Auto(AutoElem e) {
            if ((bool)e.GetCurrentPropertyValue(AutoElem.IsOffscreenProperty))
                if (ScrollTo(e)) return true;
            if (Toggle(e)) return true;
            if (Select(e)) return true;
            if (Invoke(e)) return true;
            return false;
        }
        #region Focus
        [DllImport("user32")] static extern int GetFocus();
        [DllImport("user32")] static extern int SetFocus(IntPtr hWnd);
        [DllImport("user32")] static extern int GetActiveWindow();
        [DllImport("user32")] static extern bool AttachThreadInput(
            int idAttach, int idAttachTo, bool fAttach);
        [DllImport("user32")] static extern int GetWindowThreadProcessId(
            IntPtr hWnd, out int ProcessId);
        [DllImport("kernel32")] static extern int GetCurrentThreadId();
        public static bool Focus(AutoElem e) {
            if ((bool)e.GetCurrentPropertyValue(AutoElem.IsKeyboardFocusableProperty)) {
                e.SetFocus();
                Console.WriteLine("UIA SetFocused");
                return true;
            } else {
                Console.WriteLine("Not UIA KbdFocusable");
            }
            
            IntPtr ElemHwnd = new IntPtr(e.Current.NativeWindowHandle);
            if (IntPtr.Zero.Equals(ElemHwnd)) {
                Console.WriteLine("ElemHwnd Fail");
                return false;
            }

            int CurThd = GetCurrentThreadId();
            if (CurThd == 0) {
                Console.WriteLine("GetCurThd Fail");
                return false;
            }
            int ElemThd = GetWindowThreadProcessId(ElemHwnd, out _);
            if (ElemThd == 0) {
                Console.WriteLine("GetElemThd Fail");
                return false;
            }
            if (!AttachThreadInput(CurThd, ElemThd, true)) {
                Console.WriteLine("Attach Fail");
                AttachThreadInput(CurThd, ElemThd, false);
                return false;
            }

            GetActiveWindow();

            Console.WriteLine(GetFocus());
            int v2 = SetFocus(ElemHwnd);
            Console.WriteLine($"{v2}");

            AttachThreadInput(CurThd, ElemThd, false);
            return v2 != 0;
        }
        #endregion
        public static bool Invoke(AutoElem e) {
            if (e.TryGetCurrentPattern(InvokePattern.Pattern, out object obj)) {
                InvokePattern Pattern = obj as InvokePattern;
                Pattern.Invoke();
                Console.WriteLine("invoked");
                return true;
            } else {
                return false;
            }
        }
        public static bool ScrollTo(AutoElem e) {
            if (e.TryGetCurrentPattern(ScrollItemPattern.Pattern, out object obj)) {
                ScrollItemPattern Pattern = obj as ScrollItemPattern;
                Pattern.ScrollIntoView();
                Console.WriteLine("scrolled to");
                return true;
            } else {
                return false;
            }
        }
        public static bool Toggle(AutoElem e) {
            if (e.TryGetCurrentPattern(TogglePattern.Pattern, out object obj)) {
                var Pattern = obj as TogglePattern;
                Pattern.Toggle();
                Console.WriteLine("toggled");
                return true;
            } else {
                return Expand(e);
            }
        }
        public static bool Expand(AutoElem e) {
            if (e.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out object obj)) {
                var Pattern = obj as ExpandCollapsePattern;
                var state = Pattern.Current.ExpandCollapseState;
                if (ExpandCollapseState.Collapsed == state
                || ExpandCollapseState.PartiallyExpanded == state) {
                    Pattern.Expand();
                    Console.WriteLine("expanded");
                } else if (ExpandCollapseState.Expanded == state) {
                    Pattern.Collapse();
                    Console.WriteLine("collapsed");
                }
                return true;
            } else {
                return false;
            }
        }
        public static bool Select(AutoElem e) {
            if (e.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object obj)) {
                var Pattern = obj as SelectionItemPattern;
                if (Pattern.Current.IsSelected) {
                    Pattern.RemoveFromSelection();
                } else {
                    Pattern.AddToSelection();
                }
                return true;
            } else {
                return false;
            }
        }
        public static bool SelectOne(AutoElem e) {
            if (e.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object obj)) {
                var Pattern = obj as SelectionItemPattern;
                Pattern.Select();
                return true;
            } else {
                return false;
            }
        }
        public static bool Dock(AutoElem e, DockPosition Pos) {
            if (e.TryGetCurrentPattern(DockPattern.Pattern, out object obj)) {
                var Pattern = obj as DockPattern;
                Pattern.SetDockPosition(Pos);
                Console.WriteLine("docked");
                return true;
            } else {
                return false;
            }
        }
        public static bool ScrollPercent(AutoElem e,
        double Percent, bool HScroll = false) {
            if (e.TryGetCurrentPattern(ScrollPattern.Pattern, out object obj)) {
                var Pattern = obj as ScrollPattern;
                if (HScroll) {
                    Pattern.SetScrollPercent(Percent, -1);
                    Console.WriteLine("Hpercented");
                } else {
                    Pattern.SetScrollPercent(-1, Percent);
                    Console.WriteLine("Vpercented");
                }
                return true;
            } else {
                return false;
            }
        }
        public static bool Scroll(AutoElem e,
        ScrollAmount Amount, bool HScroll = false) {
            if (e.TryGetCurrentPattern(ScrollPattern.Pattern, out object obj)) {
                var Pattern = obj as ScrollPattern;
                if (HScroll) {
                    Pattern.ScrollHorizontal(Amount);
                    Console.WriteLine("Hscrolled");
                } else {
                    Pattern.ScrollVertical(Amount);
                    Console.WriteLine("Vscrolled");
                }
                return true;
            } else {
                return false;
            }
        }
    }
    public class ModedOperate {
        public enum OpMode {
            Native,
            /*无参*/ Focus, Invoke, Toggle, ScrollTo, Expand, Select, SelectOne,
            /*有参*/ Dock, Scroll, ScrollPercent
        }
        /*
        ItemContainer
        MultipleView
        SynchronizedInput
        Text
        Transform
        Window
        Value
        RangeValue
        VirtualizedItem

        Grid
        GridItem
        */
        private static readonly Dictionary<OpMode, Func<AutoElem, bool>>
            Mode_NoParmOper_Map =
            new Dictionary<OpMode, Func<AutoElem, bool>>{
                { OpMode.Focus,     Operaters.Focus     },
                { OpMode.Invoke,    Operaters.Invoke    },
                { OpMode.Toggle,    Operaters.Toggle    },
                { OpMode.ScrollTo,  Operaters.ScrollTo  },
                { OpMode.Expand,    Operaters.Expand    },
                { OpMode.Select,    Operaters.Select    },
                { OpMode.SelectOne, Operaters.SelectOne },
            };
        private static readonly Dictionary<OpMode, Func<AutoElem, object[], bool>>
            Mode_ParmedOper_Map =
            new Dictionary<OpMode, Func<AutoElem, object[], bool>>{
                { OpMode.Dock,    DockWrap    },
                { OpMode.Scroll,  ScrollWrap  },
                { OpMode.ScrollPercent, PercentWrap },
            };
        private static readonly Dictionary<OpMode, Condition> Mode_PropCond_Map =
            new Dictionary<OpMode, Condition>{
                { OpMode.Focus, new PropCond(
                    AutoElem.IsInvokePatternAvailableProperty, true) },
                { OpMode.Invoke, new PropCond(
                    AutoElem.IsInvokePatternAvailableProperty, true) },
                { OpMode.Toggle, new PropCond(
                    AutoElem.IsTogglePatternAvailableProperty, true) },
                { OpMode.ScrollTo, new PropCond(
                    AutoElem.IsScrollItemPatternAvailableProperty, true) },
                { OpMode.Expand, new PropCond(
                    AutoElem.IsExpandCollapsePatternAvailableProperty, true)
                    .AndNot(new PropCond(
                        ExpandCollapsePattern.ExpandCollapseStateProperty,
                        ExpandCollapseState.LeafNode)) },
                { OpMode.Select, new PropCond(
                    AutoElem.IsSelectionItemPatternAvailableProperty, true) },
                { OpMode.SelectOne, new PropCond(
                    AutoElem.IsSelectionItemPatternAvailableProperty, true) },
                { OpMode.Dock, new PropCond(
                    AutoElem.IsDockPatternAvailableProperty, true) },
                { OpMode.Scroll, new PropCond(
                    AutoElem.IsScrollPatternAvailableProperty, true) },
                { OpMode.ScrollPercent, new PropCond(
                    AutoElem.IsScrollPatternAvailableProperty, true) },
            };

        private OpMode mode;
        public OpMode Mode {
            get => mode;
            set {
                mode = value;
                Console.WriteLine(value);
            }
        }
        public object[] Args { get; set; }
        public Condition PropCond {
            get => Mode_PropCond_Map[Mode];
        }
        public ModedOperate(OpMode mode) {
            Mode = mode;
        }
        public bool Operate(AutoElem Elem) {
            if (Mode == OpMode.Native) throw new NotImplementedException();
            if (Mode_NoParmOper_Map.TryGetValue(Mode, out Func<AutoElem, bool> f)) {
                return f(Elem);
            } else {
                return Mode_ParmedOper_Map[Mode](Elem, Args);
            }
        }
        private static bool DockWrap(AutoElem e, object[] arg) =>
            Operaters.Dock(e, (DockPosition)arg[0]);
        private static bool ScrollWrap(AutoElem e, object[] arg) =>
            Operaters.Scroll(e, (ScrollAmount)arg[0], (bool)arg[1]);
        private static bool PercentWrap(AutoElem e, object[] arg) =>
            Operaters.ScrollPercent(e, (double)arg[0], (bool)arg[1]);
    }
}
