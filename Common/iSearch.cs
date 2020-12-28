using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
#if WINFORMS
using System.Windows.Forms;
#elif WPF
using System.Windows.Input;
#endif
using csLib;

namespace iSearch
{
    public class aSearch
    {
        private readonly Dictionary<string, SearchItem> SearchDic =
            new Dictionary<string, SearchItem>();
        private readonly SearchHistory history=new SearchHistory();
        private string Browser;
        internal PrivateProfile pp;

        public aSearch()
        {
            Init();
        }

        private void Init()
        {
            string IniFile = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "iSearch.ini");
            pp = new PrivateProfile(IniFile);
            InitSearchDicFromIni();
            Browser = pp.ReadString("Options", "Browser", "");
        }

        public void Search(string InputString, bool ControlKeyDown)
        {
            history.Add(InputString, ControlKeyDown);

            if (InputString.Contains(".") && !InputString.Contains(" "))
            {
                RunBrowser(httpDirect(InputString)); //send the string directly to the browser
                return;
            }
            else if (ControlKeyDown)
            {
                RunBrowser(httpExpand(InputString)); //this adds .com and sends to browser
                return;
            }

            string URI;
            int IndexOfSpace = InputString.IndexOf(' ');
            if (IndexOfSpace < 0) //check for single token input
            {
                /*
                 * Single token here
                 * If it's in the search dictionary, just go to that domain.
                 * Else google the token
                 */
                URI = SearchDic.ContainsKey(InputString)
                    ? SearchDic[InputString].Site
                    : SearchDic["gg"].ReplaceDollar(InputString);
            }
            else
            {
                //token has a space in it
                string key = InputString.Substring(0, IndexOfSpace); //key is first token
                /*
                 * If it's in the search dictionary, create target and parm
                 * param is always the parameter.
                 * target is only set if it's an internal program, else empty string
                 */
                URI = SearchDic.ContainsKey(key)
                    ? SearchDic[key].ReplaceDollar(InputString.Substring(IndexOfSpace + 1))
                    : SearchDic["gg"].ReplaceDollar(InputString);
            }

            if (URI.Contains("http"))
                RunBrowser(URI);
            else
                Process.Start(GetTarget(URI), GetParm(URI));
        }

        #region URI manipulators
        string GetTarget(string uri)
        {
            int Index = uri.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            return uri.Substring(0, Index + 4);
        }

        string GetParm(string uri)
        {
            int Index = uri.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (Index + 5 < uri.Length)
                return uri.Substring(Index + 5);
            return string.Empty;
        }
#endregion

        void RunBrowser(string target)
        {
            if (Browser != "")
                Process.Start(Browser, target);
            else
                Process.Start(target);
        }

        public string GetStringFromHistory(KeyEventArgs e)
        {
            return history.GetStringFromHistory(e);
        }

        #region Search Providers and Dictionary Initialization
        private void InitSearchDicFromIni()
        {
            SearchDic.Clear();
            Dictionary<string, string> SectionDic = pp.ReadSectionDataAsDictionary("Search");
            foreach (KeyValuePair<string, string> entry in SectionDic)
            {
                string[] values = entry.Value.Split(',');
                var site = values[0];
                var searchstring = values[1];
                var separator = values[2];
                SearchDic.Add(entry.Key, new SearchItem(site, searchstring, separator));
            }
            SectionDic = pp.ReadSectionDataAsDictionary("Alias");
            foreach (KeyValuePair<string, string> entry in SectionDic) 
                SearchDic.Add(entry.Key, new SearchItem(SearchDic[entry.Value].Site, SearchDic[entry.Value].SearchString, SearchDic[entry.Value].Separator));
        }

        private string httpExpand(string srch) => httpDirect(srch) + ".com";

        private string httpDirect(string srch) => (srch);
#endregion
    }

    internal class SearchItem
    {
        internal SearchItem(string site, string searchstring, string separator)
        {
            Site = site.ToLower();
            SearchString = searchstring;
            Separator = separator;
        }

        internal string ReplaceDollar(string parms)
        {
            string input = parms;
            if (Separator != string.Empty)
                input = parms.Replace(" ", Separator);
            return SearchString.Replace("$", input);
        }

        internal bool IsLocal() => Site.EndsWith(".exe");

        internal string Target()
        {
            int Index = SearchString.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            return SearchString.Substring(0, Index + 4);
        }

        internal string Parm()
        {
            int Index = SearchString.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (Index + 5 < SearchString.Length)
                return SearchString.Substring(Index + 5);
            return string.Empty;
        }

        internal string Site { get; }
        internal string SearchString { get; }
        internal string Separator { get; }
    }
}