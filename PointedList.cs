using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIActrl {
    class PointedList<T> {
        public delegate void PointerSettedDele(int pointer);
        public event PointerSettedDele PointerSetted;
        protected void OnPointerSetted(int pointer)
            => PointerSetted?.Invoke(pointer);

        private int pointer;
        public int Pointer {
            get => pointer;
            set {
                if (value < 0 || value > Count - 1) {
                    throw new ArgumentException();
                }
                pointer = value;
                OnPointerSetted(value);
            }
        }
        public int Count { get; private set; }
        public T this[int i] => List[i];
        public T Current => List[Pointer];
        public List<T> List { get; private set; }

        public PointedList(List<T> List) {
            this.List = List;
            Count = List.Count;
        }
        public PointedList(T[] Array) {
            this.List = Array.ToList();
            Count = Array.Length;
        }
        public T Next() {
            pointer++;
            if (pointer >= Count) pointer = 0;
            return List[pointer];
        }
        public T Prev() {
            pointer--;
            if (pointer < 0) pointer = Count - 1;
            return List[pointer];
        }
        public bool PointTo(T p) {
            for (int i = 0; i < Count - 1; i++) {
                if (List[i].Equals(p)) {
                    pointer = i;
                    return true;
                }
            }
            return false;
        }
    }
}
