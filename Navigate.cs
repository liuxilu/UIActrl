using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Automation;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Point = System.Drawing.Point;
using AutoElem = System.Windows.Automation.AutomationElement;

namespace UIActrl {
    class Navigate {
        [DllImport("user32")] static extern IntPtr GetDesktopWindow();
        #region Events
        public delegate void CurElemChangedDele(AutoElem elem);
        public event CurElemChangedDele CurElemChanged;
        protected void OnCurElemChanged(AutoElem elem)
            => CurElemChanged?.Invoke(elem);
        public delegate void CondSibLstChangedDele(PointedList<AutoElem> elem);
        public event CondSibLstChangedDele CondSibLstChanged;
        protected void OnCondSibLstChanged(PointedList<AutoElem> elem)
            => CondSibLstChanged?.Invoke(elem);
        #endregion
        #region Members
        public bool CondMode { get; private set; } = false;

        protected AutoElem _curElem;
        public virtual AutoElem CurElem {
            get {
                VerifyCurElem();
                return _curElem;
            }
            set {
                _curElem = value;
                VerifyCurElem();
                OnCurElemChanged(_curElem);
            }
        }
        protected void VerifyCurElem() {
            if (!_curElem.Available()) {
                _curElem = AutoElem.FromHandle(GetDesktopWindow());
                Console.WriteLine("Desktop Fallback");
                if (CondMode) Walker = UncondWalker;
            }
        }

        protected TreeWalker UncondWalker;
        protected TreeWalker _walker;
        public TreeWalker Walker {
            get => _walker;
            set {
                //if (CondMode) { //先回到无条件模式
                //    CondMode = false;
                //    Walker = UncondWalker;
                //}
                CondMode = value != TreeWalker.RawViewWalker
                        && value != TreeWalker.ContentViewWalker
                        && value != TreeWalker.ControlViewWalker;
                if (CondMode) {
                    CondSibLst = new PointedList<AutoElem>(CurElem
                        .FindAll(TreeScope.Subtree, value.Condition).ToList());
                    if (CondSibLst.Count == 0) {
                        Walker = UncondWalker;
                        return;
                    }
                    UnCondRoot = CurElem;
                    CurElem = CondSibLst[0];
                } else {
                    CondSibLst = new PointedList<AutoElem>(new List<AutoElem>());
                    UncondWalker = value;
                    if (UnCondRoot.Available())
                        CurElem = UnCondRoot;
                }
                _walker = value;
            }
        }
        #endregion

        public Navigate(AutoElem Elem) {
            UncondWalker = TreeWalker.RawViewWalker;
            Walker = UncondWalker;
            CurElem = Elem;
            condSibLst.PointerSetted +=
                (_) => CurElem = condSibLst.Current;
        }
        public enum NavCode {
            Parent, Child, Prev, Next
        }
        public string NavElem(NavCode NavCode)
            => CondMode ? NavElemCond(NavCode) : NavElemUncond(NavCode);

        protected AutoElem UnCondRoot = null;
        protected PointedList<AutoElem> condSibLst =
            new PointedList<AutoElem>(new List<AutoElem>());
        public PointedList<AutoElem> CondSibLst {
            get => condSibLst;
            protected set {
                condSibLst = value;
                OnCondSibLstChanged(condSibLst);
            }
        }
        public void VerifySibList(bool ForceRefresh = false) {
            if (!UnCondRoot.Available()) {
                Walker = UncondWalker;
                return;
            }
            if (ForceRefresh) {
                CondSibLst = new PointedList<AutoElem>(UnCondRoot
                    .FindAll(TreeScope.Subtree, Walker.Condition).ToList());
                if (CondSibLst.Count == 0) {
                    Walker = UncondWalker;
                } else {
                    CurElem = CondSibLst[0];
                }
            } else {
                foreach (var i in CondSibLst.List) {
                    if (!i.Available()) {
                        CondSibLst = new PointedList<AutoElem>(UnCondRoot
                            .FindAll(TreeScope.Subtree, Walker.Condition).ToList());
                        if (CondSibLst.Count == 0) {
                            Walker = UncondWalker;
                        } else {
                            CurElem = CondSibLst[0];
                        }
                        break;
                    }
                }
            }
        }
        protected virtual string NavElemCond(NavCode NavCode) {
            string t = "";
            switch (NavCode) {
                case NavCode.Parent:
                    Walker = UncondWalker;
                    t = "CondMode Off";
                    break;
                case NavCode.Child:
                    t = "InvalidOperation";
                    break;
                case NavCode.Prev:
                    CurElem = CondSibLst.Prev();
                    t = "GotoPrev";
                    break;
                case NavCode.Next:
                    CurElem = CondSibLst.Next();
                    t = "GotoNext";
                    break;
            }
            return t;
        }
        protected virtual string NavElemUncond(NavCode NavCode) {
            AutoElem t = null;
            string ErrMsg = "";
            switch (NavCode) {
                case NavCode.Parent:
                    t = Walker.GetParent(CurElem);
                    ErrMsg = "No Parent";
                    break;
                case NavCode.Child:
                    t = Walker.GetFirstChild(CurElem);
                    ErrMsg = "No Child";
                    break;
                case NavCode.Prev:
                    t = Walker.GetPreviousSibling(CurElem);
                    if (t == null) {
                        t = RollSib(CurElem, false);
                        ErrMsg = "No Sib";
                    }
                    break;
                case NavCode.Next:
                    t = Walker.GetNextSibling(CurElem);
                    if (t == null) {
                        t = RollSib(CurElem, true);
                        ErrMsg = "No Sib";
                    }
                    break;
            }
            if (t == null) {
                return ErrMsg;
            } else {
                CurElem = t;
                return null;
            }
        }
        protected AutoElem RollSib(AutoElem e, bool Latest) {
            AutoElem t = Walker.GetParent(e);
            if (t != null) {
                if (Latest) {
                    return Walker.GetFirstChild(t);
                } else {
                    return Walker.GetLastChild(t);
                }
            }

            Func<AutoElem, AutoElem> action;
            if (Latest) {
                action = a => Walker.GetPreviousSibling(a);
            } else {
                action = a => Walker.GetNextSibling(a);
            }

            if (action(e) == null) return null;
            t = e;
            do {
                e = t;
                t = action(e);
            } while (t != null);

            return e;
        }
    }
    class CountedNavi : Navigate {
        public override AutoElem CurElem {
            set { //基类待改
                if (value.Available()) {
                    _curElem = value;
                    //VerifyCurElem();
                    if (!CondMode) {
                        CntCurSib();
                        CntCurChild();
                    }
                    OnCurElemChanged(_curElem);
                    Console.WriteLine($"c {value.GetCurrentPropertyValue(AutoElem.HelpTextProperty)}");
                } else {
                    System.Diagnostics.Debugger.Break();
                }
            }
        }

        #region counters
        //CntChild
        protected bool ChildCntCache = false;
        public int CurChildCnt { get => CurChildLst.Count; }
        public List<AutoElem> CurChildLst { get; private set; }
        protected void CntCurChild() {
            if (!ChildCntCache) {
                AutomationElementCollection c = 
                    _curElem.FindAll(TreeScope.Children, Walker.Condition);
                CurChildLst = c.ToList();
            } else {
                ChildCntCache = false;
            }
        }
        //CntSib
        protected bool SibCntCache = false;
        protected AutoElem FormerParent = AutoElem.RootElement;
        public int CurSibCnt { get => CurSibLst.Count; }
        public List<AutoElem> CurSibLst { get; private set; }
        protected void CntCurSib() {
            AutoElem t = Walker.GetParent(_curElem);
            if (t != FormerParent) {
                if (!SibCntCache) {
                    _ = SibCount(_curElem);
                } else {
                    SibCntCache = false;
                }
                FormerParent = t;
            }
        }
        protected int SibCount(AutoElem e) {
            AutoElem t = Walker.GetParent(e);
            if (t != null) {
                AutomationElementCollection c = 
                    t.FindAll(TreeScope.Children, Walker.Condition);
                CurSibLst = c.ToList();
                return c.Count;
            }

            CurSibLst = new List<AutoElem>();
            t = e;
            var t1 = t;
            while (t1 != null) {
                t = t1;
                t1 = Walker.GetNextSibling(t);
            }

            int SibCount = 0;
            while (t != null) {
                SibCount += 1;
                CurSibLst.Add(t);
                t = Walker.GetPreviousSibling(t);
            }

            return SibCount;
        }
        #endregion

        public CountedNavi(AutoElem Elem) :
            base(Elem) { }
        protected override string NavElemUncond(NavCode NavCode) {
            AutoElem t = null;
            string ErrMsg = "";
            switch (NavCode) {
                case NavCode.Parent:
                    t = Walker.GetParent(CurElem);
                    if (t != null) {
                        CurChildLst = CurSibLst;
                        ChildCntCache = true;
                    }
                    ErrMsg = "No Parent";
                    break;
                case NavCode.Child:
                    t = Walker.GetFirstChild(CurElem);
                    if (t != null) {
                        CurSibLst = CurChildLst;
                        SibCntCache = true;
                    }
                    ErrMsg = "No Child";
                    break;
                case NavCode.Prev:
                    t = Walker.GetPreviousSibling(CurElem);
                    if (t == null) {
                        t = RollSib(CurElem, false);
                        ErrMsg = "No Sib";
                    }
                    break;
                case NavCode.Next:
                    t = Walker.GetNextSibling(CurElem);
                    if (t == null) {
                        t = RollSib(CurElem, true);
                        ErrMsg = "No Sib";
                    }
                    break;
            }
            if (t == null) {
                return ErrMsg;
            } else {
                CurElem = t;
                return null;
            }
        }
        protected new AutoElem RollSib(AutoElem e, bool Latest) {
            if (e == CurElem) if (CurSibCnt == 1) return e;
            return base.RollSib(e, Latest);
        }
    }
}
