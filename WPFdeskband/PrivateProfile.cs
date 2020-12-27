using System;
using System.Collections.Generic;
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
		#region Members
		private readonly String m_iniFile;
		private string m_section = "";
		#endregion

		#region Constructors (one with section, one without)
		/// <summary>
		///   <para>Initializes a new instance of the the PrivateProfile class from a file name.
		/// </para>
		///   <para> No (default) section is set here and would therefore have to be specified in each call;
		/// The alternative is to later specify the default section using the Section setter.
		/// </para>
		/// </summary>
		/// <param name="iFile">The name of the .ini file.</param>
		public PrivateProfile(String iFile)
		{
			m_iniFile = iFile;
		}
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
		public PrivateProfile(String iFile, String sect)
		{
			m_iniFile = iFile;
			m_section = sect;
		}
		#endregion

		#region general
		//Getter/Setters for the .ini file name and the default section.
		//The file can only be set in the ctor.
		public string File
		{
			get{return m_iniFile;}
		}
		public string Section
		{
			get { return m_section; }
			set { m_section = value; }
		}
		#endregion

		#region section
		//Section-oriented members are rarely called by the user, unless an editor is being built
		//(just my thought)

		public Dictionary<string, string> ReadSectionDataAsDictionary(string section)
		{
			Dictionary<string, string> Dict = new Dictionary<string, string>();
			string[] Data = ReadSectionDataAsArray(section);
			foreach (var line in Data)
			{
				int equal = line.IndexOf('=');
				var key = line.Substring(0, equal);
				var val = line.Substring(equal + 1, line.Length - equal - 1);
				Dict.Add(key, val);
			}
			return Dict;
		}

		public Dictionary<string, string> ReadSectionDataAsDictionary()
		{
			return ReadSectionDataAsDictionary(m_section);
		}
		
		public string[] ReadSectionDataAsArray(String section)
		{
			string local = ReadSectionData(section);
			if (local != null)
				return local.Substring(0, local.Length - 1).Split('\0');	//get rid of terminating null
			return new string[0];
		}

		public string[] ReadSectionDataAsArray()
		{
			return ReadSectionDataAsArray(m_section);
		}

		private string ReadSectionData(string section)
		{
			const uint MAX_BUFFER = 32767;
			string local;
			IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MAX_BUFFER);
			try
			{
				uint bytesReturned = GetPrivateProfileSection(section, pReturnedString, MAX_BUFFER, m_iniFile);
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
				uint bytesReturned = GetPrivateProfileSectionNames(pReturnedString, MAX_BUFFER, m_iniFile);
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

		public bool WriteSectionData(string section,string value)
		{
			return WritePrivateProfileSection(section, value, m_iniFile);
		}

		public bool WriteSectionData(string value)
		{
			return WriteSectionData(m_section,value);
		}

		public bool DeleteSection()
		{
			return DeleteSection(m_section);
		}

		public bool DeleteSection(String section)
		{
			return WritePrivateProfileSection(section, "", m_iniFile);
		}
		#endregion

		#region string
		/* Read/write values as strings.
		 * Not only are these members called by the end-user, but they're invoked by
		 * 'int' and 'bool' members as well.
		 *
		 * Read members without default values use a default of String.Empty
		*/
		public bool WriteString(String section, String key, String value)
		{
			return WritePrivateProfileString(section, key, value, m_iniFile);
		}

		public bool WriteString(String key, String value)
		{
			return WriteString(m_section, key, value);
		}

		public String ReadString(String section, String key, String deflt)
		{
			StringBuilder sb = new StringBuilder(256);
			GetPrivateProfileString(section, key, deflt, sb, sb.Capacity, m_iniFile);
			return sb.ToString();
		}

		public String ReadString(String key, String deflt)
		{
			return ReadString(m_section, key, deflt);
		}

		public string ReadString(string key)
		{
			return ReadString(m_section, key, "");
		}

		public bool DeleteString(String section, String key)
		{
			return WritePrivateProfileString(section, key, null, m_iniFile);
		}

		public bool DeleteString(String key)
		{
			return DeleteString(m_section, key);
		}
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
		public void WriteStringList(String prefix, List<string> value)
		{
			WriteStringList(m_section, prefix, value);
		}

		public void WriteStringList(String section, String prefix, List<string> value)
		{
			ClearStringPrefix(section, prefix);
			for (int i = 0; i < value.Count; i++)
				WriteString(section, prefix + i, value[i]);
		}

		public List<String> ReadStringList(String section, String prefix)
		{
			List<string> output = new List<string>();
			const string NONE = "";
			int i = 0;
			bool Exists = true;
			while (Exists)
			{
				string tmp = ReadString(section, prefix + i, NONE);
				Exists = (tmp != NONE);
				if (Exists)
				{
					output.Add(tmp);
					i++;
				}
			}
			return output;
		}

		public List<String> ReadStringList(String prefix)
		{
			return ReadStringList(m_section, prefix);
		}

		public void ClearStringPrefix(String section, String prefix)
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
		public bool WriteBool(String section, String key, bool value)
		{
			return WriteString(section, key, value ? "1" : "0");
		}

		public bool WriteBool(String key, bool value)
		{
			return WriteBool(m_section, key, value);
		}

		public bool ReadBool(String section, String key, bool deflt)
		{
			string def = deflt ? "1" : "0";
			string result = ReadString(section, key, def);
			return result == "1";
		}

		public bool ReadBool(String key, bool deflt)
		{
			return ReadBool(m_section, key, deflt);
		}

		public bool ReadBool(String key)
		{
			return ReadBool(m_section, key, false);
		}
		#endregion

		#region int
		/* Int members use/produce integer values and convert to string,
		 * which are then finally written with the equivalent 'string' functions
		 *
		 * Read members without default values use a default of 0
		 */
		 public int ReadInt(String section, String key, int deflt)
		{
			return (int)GetPrivateProfileInt(section, key, deflt, m_iniFile);
		}

		public int ReadInt(String key, int deflt)
		{
			return ReadInt(m_section, key, deflt);
		}

		public int ReadInt(string key)
		{
			return ReadInt(m_section, key, 0);
		}

		public bool WriteInt(String section, String key, int val)
		{
			return WriteString(section,key,val.ToString());
		}

		public bool WriteInt(String key, int val)
		{
			return WriteInt(m_section,key,val);
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
