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
        private static readonly Dictionary<string, Tuple<string,string>> SearchDic =
            new Dictionary<string, Tuple<string,string>>();
        private static readonly List<Tuple<string,bool>> History=new List<Tuple<string,bool>>();
        private static int HistoryPointer;

        static aSearch()
        {
            InitDic();
        }

        public static void Search(string InputString, bool ControlKeyDown)
        {
            string Target = "iexplore.exe";
            string Param;
            History.Add(new Tuple<string, bool>(InputString, ControlKeyDown));
            if (InputString.Contains(".") && !InputString.Contains(" "))
                Param = httpDirect(InputString);
            else if (ControlKeyDown)
                Param = httpExpand(InputString);
            else
            {
                int IndexOfSpace = InputString.IndexOf(' ');
                string key = IndexOfSpace >= 0 ? InputString.Substring(0, IndexOfSpace) : "";
                Target = SearchDic.ContainsKey(key)
                    ? FullSearchString(key,InputString.Substring(IndexOfSpace + 1),SearchDic[key].Item2, out Param)
                        : FullSearchString("gg",InputString,SearchDic["gg"].Item2, out Param);
            }
            if (Target != "test")
                Process.Start(Target, Param);
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

        /*
        Initialize dictionary of search providers from application settings.
        We're looking for strings whose Name starts with Search and are of the type
        Shortcut,Target,Separator
        */
        private static void InitDic()
        {
			// You have to prime the actual values pump by asking for one by name.
	        var unused = Settings.Default.SEARCHdic;

            foreach (SettingsPropertyValue currentProperty in Settings.Default.PropertyValues)
            {
                if (currentProperty.Name.StartsWith("SEARCH"))
                {
                    //parse
                    string[] values = currentProperty.PropertyValue.ToString().Split(',');
                    var key = values[0];
                    var target = values[1];
                    var separator = values[2];
                    SearchDic.Add(key, new Tuple<string, string>(target, separator));
                }
            }
        }

        private static string FullSearchString(string key, string SearchInput, string FormatInfo, out string Param)
        {
            string target = SearchDic[key].Item1;
            Param = string.Empty;
            string[] parts = FormatInfo.Split('=');
            string FormatKey = parts[0];
            string FormatVal = parts[1];
            switch (FormatKey)
            {
                case "SEP":
                    if (FormatVal.Length == 1)
                        return target+SearchInput.Replace(' ', FormatVal[0]);
                    return target+SearchInput.Replace(" ", FormatVal);
                case "PARM":
                    Param=FormatVal.Replace("$", SearchInput);
                    return target;
            }
            return string.Empty;
        }

        private static string httpExpand(string srch)
        {
            //return ("http://www." + srch + ".com");
            //return ("https://" + srch + ".com");
            return httpDirect(srch) + ".com";
        }

        private static string httpDirect(string srch)
        {
            //return ("http://" + srch);
            //return ("https://" + srch);
            return (srch);
        }

        #endregion

        #region obso
        /*
            Initialize dictionary of search providers from application settings.
            We're looking for strings of the type
            Shortcut[i], Prefix[i], Separator[i]
            where i ranges up sequentially from 1 without gap
        */
        /*
        private static void InitDicObso()
        {
            string key, prefix, separator;
            int i = 1;
            while (true)
            {
                try
                {
                    key = (string)Properties.Settings.Default["Shortcut" + i];
                }
                catch (Exception)
                {
                    return;
                }
                try
                {
                    prefix = (string)Properties.Settings.Default["Prefix" + i];
                    separator = (string)Properties.Settings.Default["Separator" + i];
                }
                catch (Exception)
                {
                    return;
                }
                SearchDic.Add(key, new Tuple<string, string>(prefix, separator));
                i++;
            }
        }
        */

        /*
        SearchDic.Add("gg", googleSearch);
        SearchDic.Add("amaz", amazSearch);
        SearchDic.Add("map", mapSearch);
        SearchDic.Add("wik", wikSearch);
        SearchDic.Add("imdb", imdbSearch);
        SearchDic.Add("test", testSearch);
        SearchDic.Add("so", soSearch);
        */

        /*
        private static string mapSearch(string srch)
        {
            const string Prefix = "https://www.google.com/maps/search/";
            return (Prefix + srch.Replace(' ', '+'));
        }

        private static string testSearch(string srch)
        {
            return "test";
        }

        private static string wikSearch(string srch)
        {
            const string Prefix = "http://www.wikipedia.org/wiki/Special:Search/";
            return (Prefix + srch.Replace(' ', '_'));
        }

        private static string soSearch(string srch)
        {
            const string Prefix = "https://www.stackoverflow.com/search?q=";
            return (Prefix + srch.Replace(' ', '+'));
        }

        private static string googleSearch(string srch)
        {
            const string Prefix = "https://www.google.com/search?q=";
            return (Prefix + srch.Replace(' ', '+'));
        }

        private static string imdbSearch(string srch)
        {
            const string Prefix = "http://www.imdb.com/find/?q=";
            return (Prefix + srch.Replace(' ', '+'));
        }

        private static string amazSearch(string srch)
        {
            const string Prefix = "http://www.amazon.com/s/?keywords=";
            return (Prefix + srch.Replace(" ", "%20"));
        }
        */
        #endregion

    }
}
