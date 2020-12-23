using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Windows.Input;
using iSearch.Properties;

namespace iSearch
{
    public static class aSearch
    {
        private static readonly Dictionary<string, SearchItem> SearchDic =
            new Dictionary<string, SearchItem>();

        private static readonly List<Tuple<string, bool>> History = new List<Tuple<string, bool>>();
        private static int HistoryPointer;
        private static readonly string Browser;

        static aSearch()
        {
            InitSearchDic();
            //Browser is either set to some executable or null
            try
            {
                Browser = Settings.Default["Browser"].ToString();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static void Search(string InputString, bool ControlKeyDown)
        {
            History.Add(new Tuple<string, bool>(InputString, ControlKeyDown));

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
        static string GetTarget(string uri)
        {
            int Index = uri.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            return uri.Substring(0, Index + 4);
        }

        static string GetParm(string uri)
        {
            int Index = uri.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (Index + 5 < uri.Length)
                return uri.Substring(Index + 5);
            return string.Empty;
        }
        #endregion

        static void RunBrowser(string target)
        {
            if (Browser != "")
                Process.Start(Browser, target);
            else
                Process.Start(target);
        }

        public static string GetStringFromHistory(KeyEventArgs e)
        {
            if (History.Count == 0)
                return string.Empty;
            if (e.Key == Key.Up)
                HistoryPointer = HistoryPointer == History.Count - 1 ? HistoryPointer : HistoryPointer + 1;
            else if (e.Key == Key.Down)
                HistoryPointer = HistoryPointer == 0 ? 0 : HistoryPointer - 1;
            return History[HistoryPointer].Item1;
        }

        #region Search Providers and Dictionary Initialization

        private static void InitSearchDic()
        {
            /*
            Initialize dictionary of search providers from application settings.
            We're looking for strings whose Name starts with Search and are of the type
            Shortcut (i.e. key, Target (often domain URL), SearchString, Separator

            The SearchString should contain $ which is replaced by the input after
            spaces are converted to the separator string
            */

            // You have to prime the actual values pump by asking for one by name.
            var unused = Settings.Default.SEARCHdic;
            foreach (SettingsPropertyValue currentProperty in Settings.Default.PropertyValues)
            {
                if (currentProperty.Name.StartsWith("SEARCH"))
                {
                    /*
                     * parse: sample is
                     * imdb,http://www.imdb.com,/find/?q=,SEP=+
                     */
                    string[] values = currentProperty.PropertyValue.ToString().Split(',');
                    var key = values[0];
                    var site = values[1];
                    var searchstring = values[2];
                    var separator = values[3];
                    SearchDic.Add(key, new SearchItem(site, searchstring, separator));
                }
                else if (currentProperty.Name.StartsWith("ALIAS"))
                {
                    string[] values = currentProperty.PropertyValue.ToString().Split(',');
                    var key  = values[0];
                    var alias = values[1];
                    SearchDic.Add(key,new SearchItem(SearchDic[alias].Site, SearchDic[alias].SearchString,SearchDic[alias].Separator));
                }
            }
        }

        private static string httpExpand(string srch)
        {
            return httpDirect(srch) + ".com";
        }

        private static string httpDirect(string srch)
        {
            return (srch);
        }

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

        internal bool IsLocal()
        {
            return Site.EndsWith(".exe");
        }

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