using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;

namespace csLib
{
	/// <summary>
	///   <para>
	/// PrivateProfile contains all the functionality to deal with Private Profile (.ini) files.
	/// It is a robust wrapper around the functions found in kernel.dll--see 'DLL Imports' section at bottom
	/// of code.
	/// See https://en.wikipedia.org/wiki/INI_file for details about .ini files.
	/// See https://stackoverflow.com/questions/10507927/getprivateprofilestring-buffer-length for a fascinating
	/// discourse on  buffer lengths and the like.
	/// </para>
	///   <para>
	/// OnTrack keeps (virtually) all user-specific parameters in OnTrack.ini, found in the
	/// user's appdata\local\OnTrack.
	/// OnTrack.ini is updated by various OnTrack modules during execution and is considered highly read/write, although there are some sections that should not be modified. This is described in other OnTrack documentation not connected with code. </para>
	///   <para>Installation-specific data is kept in tds.lic, found in \programdata\ontrack. This file should be
	/// considered read-only. While there are no facilities in this class to ensure the read-only state,
	/// OnTrack will fail with a 'Bad License' message the next time it's run should tds.lic be changed by this class or a text editor.
	/// </para>
	/// </summary>
	public class PrivateProfile
	{
		#region ctor
		/// <summary>
		///   <para>
		/// Initializes a new instance of the PrivateProfile class from a file name and a default section.
		/// This sets the default section.
		/// </para>
		///   <para>NB: The default section can be overridden in any method.
		/// </para>
		/// </summary>
		/// <param name="iFile"></param>
		/// <param name="sect"></param>
		public PrivateProfile(string iFile, string sect="")
		{
			File = iFile;
			Section = sect;
		}
		#endregion

		#region getter/setters
		//Getter/Setters for the .ini file name and the default section.
		//The file can only be set in the ctor.
		public string File { get; }

		public string Section { get; set; }
		#endregion

		#region section
		//Section-oriented members are rarely called by the user, unless an editor is being built
		//(just my thought)

		public Dictionary<string, string> ReadSectionDataAsDictionary(string section)
		{
			var Dict = new Dictionary<string, string>();
			var Data = ReadSectionDataAsArray(section);
			foreach (var line in Data)
			{
				var equal = line.IndexOf('=');
				var key = line.Substring(0, equal);
				var val = line.Substring(equal + 1, line.Length - equal - 1);
				if (!Dict.ContainsKey(key))
					Dict.Add(key, val);
			}
			return Dict;
		}

		public Dictionary<string, string> ReadSectionDataAsDictionary() => ReadSectionDataAsDictionary(Section);

		public string[] ReadSectionDataAsArray(string section)
		{
			string local = ReadSectionData(section);
			if (local != null)
				return local.Substring(0, local.Length - 1).Split('\0');	//get rid of terminating null
			return Array.Empty<string>();
		}

		public string[] ReadSectionDataAsArray() => ReadSectionDataAsArray(Section);

		public string ReadSectionData(string section)
		{
			const uint MAX_BUFFER = 32767;
			string local;
			IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MAX_BUFFER);
			try
			{
				uint bytesReturned = GetPrivateProfileSection(section, pReturnedString, MAX_BUFFER, File);
				if (bytesReturned == 0)
					return null;
				local = Marshal.PtrToStringAuto(pReturnedString, (int) bytesReturned);
			}
			finally
			{
				Marshal.FreeCoTaskMem(pReturnedString);
			}
			return local;
		}

		public string[] ReadSectionNames()
		{
			//Reads the names of all [sections].
			//Has nothing to do with the content of any given section
			const uint MAX_BUFFER = 32767;
			string local;
			IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MAX_BUFFER);
			try
			{
				uint bytesReturned = GetPrivateProfileSectionNames(pReturnedString, MAX_BUFFER, File);
				if (bytesReturned == 0)
					return null;
				local = Marshal.PtrToStringAuto(pReturnedString, (int) bytesReturned);
			}
			finally
			{
				Marshal.FreeCoTaskMem(pReturnedString);
			}
			//following is dubious if GetPrivateProfileSectionNames throws an exception and local=""
			return local.Substring(0, local.Length - 1).Split('\0');	//get rid of terminating null
		}

		public bool WriteSectionData(string section,string value) => WritePrivateProfileSection(section, value, File);

		public bool WriteSectionData(string value) => WriteSectionData(Section,value);

		public bool WriteSectionDataAsArray(string section, string[] keyvals)
		{
			var value = string.Join("\0", keyvals) + "\0\0";
			return WritePrivateProfileSection(section, value, File);
		}

		public bool WriteSectionDataAsArray(string[] keyvals) => WriteSectionDataAsArray(Section, keyvals);

		public bool DeleteSection() => DeleteSection(Section);

		public bool DeleteSection(string section)
		{
			return WritePrivateProfileSection(section, "", File);
		}
		#endregion

		#region string
		/* Read/write values as strings.
		 * Not only are these members called by the end-user, but they're invoked by
		 * 'int' and 'bool' members as well.
		 *
		 * Read members without default values use a default of string.Empty
		*/
		public bool WriteString(string section, string key, string value) => WritePrivateProfileString(section, key, value, File);

		public bool WriteString(string key, string value) => WriteString(Section, key, value);

		public string ReadString(string section, string key, string deflt)
		{
			StringBuilder sb = new StringBuilder(256);
			GetPrivateProfileString(section, key, deflt, sb, sb.Capacity, File);
			return sb.ToString();
		}

		public string ReadString(string key, string deflt) => ReadString(Section, key, deflt);

		public string ReadString(string key) => ReadString(Section, key, "");

		public bool DeleteString(string section, string key) => WritePrivateProfileString(section, key, null, File);

		public bool DeleteString(string key) => DeleteString(Section, key);

		#endregion

		#region StringList
		/*
		 * The mru (most recently used) file functions typically write their values in a section
		 * with a prefix followed by an integer starting at 1
		 *
		 * See MRU.cs for details
		 *
		 * These StringList functions consume/produce List<string> containing the values to be read/written.
		 */
		public void WriteStringList(string prefix, List<string> value) => WriteStringList(Section, prefix, value);

		public void WriteStringList(string section, string prefix, List<string> value)
		{
			ClearStringPrefix(section, prefix);
			for (int i = 0; i < value.Count; i++)
				WriteString(section, prefix + i, value[i]);
		}

		public List<string> ReadStringList(string section, string prefix)
		{
			List<string> output = new List<string>();
			int i = 0;
			while (true)
			{
				string tmp = ReadString(section, prefix + i, "");
				if (tmp == "") break;
				output.Add(tmp);
				i++;
			}
			return output;
		}

		public List<string> ReadStringList(string prefix) => ReadStringList(Section, prefix);

		public void ClearStringPrefix(string section, string prefix)
		{
			const string NONE = "NONE";
			int i = 0;
			bool Exists = true;
			while (Exists)
			{
				string tmp = ReadString(section, prefix + i, NONE);
				Exists = (tmp != NONE);
				if (Exists)
				{
					DeleteString(section, prefix + i);
					i++;
				}
			}
		}

		/* test
		 static void Main(string[] args)
		{
			List<string> lst = args.ToList();
			PrivateProfile pp=new PrivateProfile(@"e:\tmp\eraseme.ini","sect");
			pp.WriteStringList("pref",lst);
			List<string> output=pp.ReadStringList("sect", "pref");
		}
		*/
		#endregion

		#region bool
		/* Bool members use/produce boolean values and convert to/from 0 and 1,
		 * which are then finally written with the equivalent 'string' functions
		 *
		 * Read members without default values use a default of False
		 */
		public bool WriteBool(string section, string key, bool value) => WriteString(section, key, value ? "1" : "0");

		public bool WriteBool(string key, bool value) => WriteBool(Section, key, value);

		public bool ReadBool(string section, string key, bool deflt)
		{
			string def = deflt ? "1" : "0";
			string result = ReadString(section, key, def);
			return result == "1";
		}

		public bool ReadBool(string key, bool deflt) => ReadBool(Section, key, deflt);

		public bool ReadBool(string key) => ReadBool(Section, key, false);

		#endregion

		#region int
		/* Int members use/produce integer values and convert to string,
		 * which are then finally written with the equivalent 'string' functions
		 *
		 * Read members without default values use a default of 0
		 */
		 public int ReadInt(string section, string key, int deflt) => (int)GetPrivateProfileInt(section, key, deflt, File);

		 public int ReadInt(string key, int deflt) => ReadInt(Section, key, deflt);

		 public int ReadInt(string key) => ReadInt(Section, key, 0);

		 public bool WriteInt(string section, string key, int val) => WriteString(section,key,val.ToString());

		 public bool WriteInt(string key, int val) => WriteInt(Section,key,val);
		#endregion

		#region Toml Dictionaries
		//These are what the spec calls 'Inline Tables'
		//Example:
		//name = { first = "Tom", last = "Preston-Werner" }	//use ReadTomlStringDictionary
		//point = {x=1, y=2}								//use ReadTomlFloatDictionary

		public Dictionary<string, string> ReadTomlStringDictionary(string section, string key)
		{
			string raw = ReadString(section, key, "");
			return ParseInlineTable(raw);
		}

		public Dictionary<string, string> ReadTomlStringDictionary(string key) => ReadTomlStringDictionary(Section, key);

		public bool WriteTomlStringDictionary(string section, string key, IDictionary<string, string> value)
			=> WriteString(section, key, FormatInlineTable(value));

		public bool WriteTomlStringDictionary(string key, IDictionary<string, string> value)
			=> WriteTomlStringDictionary(Section, key, value);

		public Dictionary<string, float> ReadTomlFloatDictionary(string section, string key)
		{
			var strings = ReadTomlStringDictionary(section, key);
			var output = new Dictionary<string, float>();
			foreach (var pair in strings)
				output[pair.Key] = float.Parse(pair.Value, CultureInfo.InvariantCulture);
			return output;
		}

		public Dictionary<string, float> ReadTomlFloatDictionary(string key) => ReadTomlFloatDictionary(Section, key);

		public bool WriteTomlFloatDictionary(string section, string key, IDictionary<string, float> value)
		{
			var strings = new Dictionary<string, string>();
			foreach (var pair in value)
				strings[pair.Key] = pair.Value.ToString(CultureInfo.InvariantCulture);
			return WriteTomlStringDictionary(section, key, strings);
		}

		public bool WriteTomlFloatDictionary(string key, IDictionary<string, float> value)
			=> WriteTomlFloatDictionary(Section, key, value);
		#endregion

		#region Toml Lists
		//These are what the spec calls 'Arrays'
		//Example:
		//integers = [ 1, 2, 3 ]	//use ReadTomlIntList
		//names = [a, b, c]			//use ReadTomlStringList
		//pos = [1.1, 2.2]			//use ReadTomlFloatList
		
		public List<int> ReadTomlIntList(string section, string key)
		{
			var strings = ParseArray(ReadString(section, key, ""));
			var output = new List<int>();
			foreach (string value in strings)
				output.Add(int.Parse(value, CultureInfo.InvariantCulture));
			return output;
		}

		public List<int> ReadTomlIntList(string key) => ReadTomlIntList(Section, key);

		public bool WriteTomlIntList(string section, string key, IList<int> value)
		{
			var strings = new List<string>();
			foreach (int item in value)
				strings.Add(item.ToString(CultureInfo.InvariantCulture));
			return WriteString(section, key, FormatArray(strings));
		}

		public bool WriteTomlIntList(string key, IList<int> value) => WriteTomlIntList(Section, key, value);

		public List<float> ReadTomlFloatList(string section, string key)
		{
			var strings = ParseArray(ReadString(section, key, ""));
			var output = new List<float>();
			foreach (string value in strings)
				output.Add(float.Parse(value, CultureInfo.InvariantCulture));
			return output;
		}

		public List<float> ReadTomlFloatList(string key) => ReadTomlFloatList(Section, key);

		public bool WriteTomlFloatList(string section, string key, IList<float> value)
		{
			var strings = new List<string>();
			foreach (int item in value)
				strings.Add(item.ToString(CultureInfo.InvariantCulture));
			return WriteString(section, key, FormatArray(strings));
		}

		public bool WriteTomlFloatList(string key, IList<float> value) => WriteTomlFloatList(Section, key, value);

		public List<string> ReadTomlStringList(string section, string key)
			=> ParseArray(ReadString(section, key, ""));

		public List<string> ReadTomlStringList(string key) => ReadTomlStringList(Section, key);

		public bool WriteTomlStringList(string section, string key, IList<string> value)
			=> WriteString(section, key, FormatArray(value));

		public bool WriteTomlStringList(string key, IList<string> value)
			=> WriteTomlStringList(Section, key, value);
		#endregion

		#region Toml utilities
		private static Dictionary<string, string> ParseInlineTable(string text)
		{
			var output = new Dictionary<string, string>();
			if (string.IsNullOrWhiteSpace(text))
				return output;

			string trimmed = text.Trim();
			if (trimmed.Length < 2 || trimmed[0] != '{' || trimmed[trimmed.Length - 1] != '}')
				throw new FormatException("Inline TOML table must be enclosed in braces.");

			string body = trimmed.Substring(1, trimmed.Length - 2).Trim();
			if (body.Length == 0)
				return output;

			foreach (string pair in SplitTomlPairs(body))
			{
				int equal = pair.IndexOf('=');
				if (equal <= 0 || equal == pair.Length - 1)
					throw new FormatException("Invalid TOML key/value pair in inline table.");

				string rawKey = pair.Substring(0, equal).Trim();
				string rawValue = pair.Substring(equal + 1).Trim();
				string parsedKey = UnquoteTomlString(rawKey);
				string parsedValue = UnquoteTomlString(rawValue);
				output[parsedKey] = parsedValue;
			}

			return output;
		}

		private static string FormatInlineTable(IDictionary<string, string> value)
		{
			if (value == null || value.Count == 0)
				return "{ }";

			var parts = new List<string>();
			foreach (var pair in value)
			{
				string key = FormatTomlKey(pair.Key);
				string val = FormatTomlValue(pair.Value);
				parts.Add($"{key} = {val}");
			}

			return "{ " + string.Join(", ", parts) + " }";
		}

		private static List<string> ParseArray(string text)
		{
			var output = new List<string>();
			if (string.IsNullOrWhiteSpace(text))
				return output;

			string trimmed = text.Trim();
			if (trimmed.Length < 2 || trimmed[0] != '[' || trimmed[trimmed.Length - 1] != ']')
				throw new FormatException("TOML array must be enclosed in brackets.");

			string body = trimmed.Substring(1, trimmed.Length - 2).Trim();
			if (body.Length == 0)
				return output;

			foreach (string item in SplitTomlItems(body))
				output.Add(UnquoteTomlString(item.Trim()));

			return output;
		}

		private static string FormatArray(IList<string> value)
		{
			if (value == null || value.Count == 0)
				return "[ ]";

			var parts = new List<string>();
			foreach (string item in value)
				parts.Add(FormatTomlValue(item));

			return "[ " + string.Join(", ", parts) + " ]";
		}

		private static List<string> SplitTomlPairs(string body)
		{
			var parts = new List<string>();
			var current = new StringBuilder();
			bool inQuotes = false;

			for (int i = 0; i < body.Length; i++)
			{
				char ch = body[i];
				if (ch == '"' && !IsEscaped(body, i))
				{
					inQuotes = !inQuotes;
					current.Append(ch);
				}
				else if (ch == ',' && !inQuotes)
				{
					parts.Add(current.ToString().Trim());
					current.Clear();
				}
				else
				{
					current.Append(ch);
				}
			}

			if (current.Length > 0)
				parts.Add(current.ToString().Trim());

			return parts;
		}

		private static List<string> SplitTomlItems(string body)
		{
			var parts = new List<string>();
			var current = new StringBuilder();
			bool inQuotes = false;

			for (int i = 0; i < body.Length; i++)
			{
				char ch = body[i];
				if (ch == '"' && !IsEscaped(body, i))
				{
					inQuotes = !inQuotes;
					current.Append(ch);
				}
				else if (ch == ',' && !inQuotes)
				{
					parts.Add(current.ToString().Trim());
					current.Clear();
				}
				else
				{
					current.Append(ch);
				}
			}

			if (current.Length > 0)
				parts.Add(current.ToString().Trim());

			return parts;
		}

		private static bool IsEscaped(string text, int index)
		{
			int slashCount = 0;
			for (int i = index - 1; i >= 0 && text[i] == '\\'; i--)
				slashCount++;
			return (slashCount % 2) == 1;
		}

		private static string UnquoteTomlString(string text)
		{
			if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
			{
				string inner = text.Substring(1, text.Length - 2);
				return inner
					.Replace("\\\"", "\"")
					.Replace("\\\\", "\\");
			}

			return text;
		}

		private static string FormatTomlKey(string key)
		{
			if (string.IsNullOrEmpty(key))
				return "\"\"";

			foreach (char ch in key)
			{
				if (!(char.IsLetterOrDigit(ch) || ch == '_' || ch == '-'))
					return FormatTomlString(key);
			}

			return key;
		}

		private static string FormatTomlValue(string value)
		{
			if (value == null)
				return "\"\"";

			if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _))
				return value;

			return FormatTomlString(value);
		}

		private static string FormatTomlString(string value)
		{
			return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
		}
		#endregion

		#region DLL Imports
		[DllImport("kernel32.dll")]
		static extern uint GetPrivateProfileString(
			string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);
		[DllImport("kernel32.dll")]
		static extern bool WritePrivateProfileString(string lpAppName,
		   string lpKeyName, string lpString, string lpFileName);
		[DllImport("kernel32.dll")]
		static extern uint GetPrivateProfileInt(string lpAppName, string lpKeyName,
		   int nDefault, string lpFileName);
		[DllImport("kernel32.dll")]
		static extern bool WritePrivateProfileSection(string lpAppName,
		   string lpString, string lpFileName);
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		static extern uint GetPrivateProfileSection(string lpAppName,
		   IntPtr lpReturnedString, uint nSize, string lpFileName);
		[DllImport("kernel32.dll",CharSet=CharSet.Auto)]
		static extern uint GetPrivateProfileSectionNames(IntPtr lpszReturnBuffer,
		   uint nSize, string lpFileName);
		#endregion
	}
}
