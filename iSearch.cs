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
		private static readonly Dictionary<string, Tuple<string, string,string>> SearchDic =
			new Dictionary<string, Tuple<string, string, string>>();
		private static readonly List<Tuple<string, bool>> History = new List<Tuple<string, bool>>();
		private static int HistoryPointer;
		private static readonly string Browser;

		static aSearch()
		{
			InitSearchDic();
			Browser = Settings.Default.Browser;
		}

		public static void Search(string InputString, bool ControlKeyDown)
		{
			string Target = Browser;
			//string Target = "iexplore.exe";
			string Param = string.Empty;
			History.Add(new Tuple<string, bool>(InputString, ControlKeyDown));
			if (InputString.Contains(".") && !InputString.Contains(" "))
				Param = httpDirect(InputString);    //this will send the string directly to the browser
			else if (ControlKeyDown)
				Param = httpExpand(InputString);    //this adds .com and sends to browser
			else
			{
				int IndexOfSpace = InputString.IndexOf(' ');
				if (IndexOfSpace < 0) //check for single token input
				{
					/*
					 * Single token here
					 * If it's in the search dictionary, just go to that domain.
					 * Else google the token
					 */
					Target = SearchDic.ContainsKey(InputString) ? SearchDic[InputString].Item1 : FullSearchString("gg", InputString, SearchDic["gg"].Item3, out Param);
				}
				else
				{
					//token has a space in it
					string key = InputString.Substring(0, IndexOfSpace);    //key is first token
					/*
					 * If it's in the search dictionary, create target and parm
					 */
					Target = SearchDic.ContainsKey(key)
						? FullSearchString(key, InputString.Substring(IndexOfSpace + 1), SearchDic[key].Item3,
							out Param)
						: FullSearchString("gg", InputString, SearchDic["gg"].Item3, out Param);
				}
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
		Shortcut, Target (often domain URL), Search, Separator (or parameter)
		*/
		private static void InitSearchDic()
		{
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
					var Target = values[1];
					var SearchTemplate = values[2];
					var Separator = values[3];
					SearchDic.Add(key, new Tuple<string, string, string>(Target, SearchTemplate,Separator));
				}
			}
		}

		private static string FullSearchString(string key, string SearchInput, string FormatInfo, out string Param)
		{
			string Target = SearchDic[key].Item1;
			string SearchTemplate = SearchDic[key].Item2;
			Param = string.Empty;
			string[] parts = FormatInfo.Split('=');
			string FormatKey = parts[0];
			string FormatVal = parts[1];
			switch (FormatKey)
			{
				case "SEP":
					if (FormatVal.Length == 1)
						return Target + SearchTemplate + SearchInput.Replace(' ', FormatVal[0]);
					return Target + SearchTemplate + SearchInput.Replace(" ", FormatVal);
				case "PARM":
					Param = FormatVal.Replace("$", SearchInput);
					return Target;
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
	}
}
