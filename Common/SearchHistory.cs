using System;
using System.Collections.Generic;
using System.Windows.Input;
#if WINFORMS
using System.Windows.Forms;
#endif


namespace iSearch
{
    class SearchHistory
    {
        private readonly List<Tuple<string, bool>> History = new List<Tuple<string, bool>>();
        private int Pointer;

        public string GetStringFromHistory(KeyEventArgs e)
        {
            if (History.Count == 0)
                return string.Empty;
            /*
             * What the hell is going on here?
             * Well, WPF and Winforms are different in how KeyEventArgs are defined,
             * as well as Key vs Keys
             * Why, he whined?
             */

            #if WINFORMS
                if (e.KeyCode == Keys.Down)
                    Pointer = Pointer == History.Count - 1 ? Pointer : Pointer + 1;
                else if (e.KeyCode == Keys.Up)
                    Pointer = Pointer == 0 ? 0 : Pointer - 1;
            #elif WPF
                if (e.Key == Key.Down)
                    Pointer = Pointer == History.Count - 1 ? Pointer : Pointer + 1;
                else if (e.Key == Key.Up)
                    Pointer = Pointer == 0 ? 0 : Pointer - 1;
            #endif
            return History[Pointer].Item1;
        }

        public void Add(string input, bool ctrl)
        {
            History.Add(new Tuple<string, bool>(input, ctrl));
        }
    }
}
