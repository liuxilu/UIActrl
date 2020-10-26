using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Automation;
using System.Drawing;

namespace UIActrl
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Marker());
        }
        //Automation
        public static bool Available(this AutomationElement elem) {
            if (elem == null) return false;
            try {
                _ = elem.Current.Name;
            } catch (ElementNotAvailableException) {
                return false;
            }
            return true;
        }
        public static List<AutomationElement> ToList(this AutomationElementCollection c) {
            var l = new List<AutomationElement>();
            var e = c.GetEnumerator();
            while (e.MoveNext())
                l.Add((AutomationElement)e.Current);
            return l;
        }
        public static AutomationElementCollection FindAllChild(this AutomationElement e)
            => e.FindAll(TreeScope.Children, Condition.TrueCondition);
        public static AutomationElement FindFirstChild(this AutomationElement e)
            => e.FindFirst(TreeScope.Children, Condition.TrueCondition);

        ///*
        public static Condition And(this Condition c, Condition cs) =>
            new AndCondition(c, cs);
        public static Condition AndNot(this Condition c, Condition cs) =>
            new AndCondition(c, new NotCondition(cs));
        public static Condition Or(this Condition c, Condition cs) =>
            new OrCondition(c, cs);
        public static Condition OrNot(this Condition c, Condition cs) =>
            new OrCondition(c, new NotCondition(cs));
        public static Condition And(this Condition c, params Condition[] cs) =>
            new AndCondition(c, new AndCondition(cs));
        public static Condition AndNot(this Condition c, params Condition[] cs) =>
            new AndCondition(c, new NotCondition(new AndCondition(cs)));
        public static Condition Or(this Condition c, params Condition[] cs) =>
            new OrCondition(c, new OrCondition(cs));
        public static Condition OrNot(this Condition c, params Condition[] cs) =>
            new OrCondition(c, new NotCondition(new OrCondition(cs)));
        //*/
        /*
        public static Condition OrProp(this Condition c,
        AutomationProperty Prop, object Val) =>
            new OrCondition(c, new PropertyCondition(Prop, Val));
        public static Condition OrProp(this Condition c,
        params (AutomationProperty Prop, object Val)[] p) {
            List<Condition> l = new List<Condition>();
            foreach (var i in p)
                l.Add(new PropertyCondition(i.Prop, i.Val));
            return new AndCondition(c, new OrCondition(l.ToArray()));
        }
        public static Condition AndProp(this Condition c,
        (AutomationProperty Prop, object Val) p) =>
            new AndCondition(c, new PropertyCondition(p.Prop, p.Val));
        public static Condition AndProp(this Condition c,
        params (AutomationProperty Prop, object Val)[] p) {
            List<Condition> l = new List<Condition>();
            foreach (var i in p)
                l.Add(new PropertyCondition(i.Prop, i.Val));
            return new AndCondition(c, new AndCondition(l.ToArray()));
        }
       */
        //String
        public static string RevSubStr(this string s, int n)
            => s.Substring(s.Length - n);
        public static string CutButt(this string s, int n)
            => s.Substring(0, s.Length - n);
        //List
        public static List<T> TakeLast<T>(this List<T> l, int n) {
            l.Reverse();
            l = l.Take(n).ToList();
            l.Reverse();
            return l;
        }
        //Drawing
        public static Color RevColor(this Color c) =>
            Color.FromArgb(c.A, 255 - c.R, 255 - c.G, 255 - c.B);
        public static Color Opacity(this Color c) =>
            Color.FromArgb(255, c.R, c.G, c.B);
        //Rect
        public static Point Center(this System.Windows.Rect rc) =>
            new Point((int)(rc.Left + rc.Width / 2), (int)(rc.Top + rc.Height / 2));
        public static Point Center(this Rectangle rc) =>
            new Point(rc.Left + rc.Width / 2, rc.Top + rc.Height / 2);
        public static Rectangle ToRectangle(this System.Windows.Rect rc) =>
            new Rectangle((int)rc.X, (int)rc.Y, (int)rc.Width, (int)rc.Height);
    }
}
