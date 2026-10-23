using System;
using System.Reflection;

namespace csLib
{
    /// <summary>
    ///   <para>AssemblyAccessors is a static class used to retrieve information about the current assembly.</para>
    ///   <para>Although most often used in the About form (also in csLib) it can be usefully employeed in any startup or splash form.</para>
    ///   <para>The properties of this form are not documented in the source code or here, as they are deemed obvious. Each of them is a static string.</para>
    /// </summary>
    static class AssemblyAccessors
    {
        #region Special-Purpose
        public static DateTime BuildDate
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return new DateTime(2000, 1, 1)
                    .AddDays(version.Build)
                    .AddSeconds(version.Revision * 2);
            }
        }

        public static string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        #endregion

        #region Helpers
        private static readonly Assembly assembly =
            typeof(AssemblyAccessors).Assembly;

        private static string GetAttributeValue<T>(Func<T, string> select)
            where T : Attribute
        {
            object[] attributes =
                assembly.GetCustomAttributes(typeof(T), false);

            return attributes.Length == 0
                ? ""
                : select((T)attributes[0]);
        }
        #endregion

        public static string AssemblyTitle
        {
            get
            {
                var title = GetAttributeValue<AssemblyTitleAttribute>(a => a.Title);
                return title == "" ? System.IO.Path.GetFileNameWithoutExtension(assembly.Location) : title;
            }
        }

        public static string AssemblyDescription =>
            GetAttributeValue<AssemblyDescriptionAttribute>(a => a.Description);

        public static string AssemblyProduct =>
            GetAttributeValue<AssemblyProductAttribute>(a => a.Product);

        public static string AssemblyCopyright =>
            GetAttributeValue<AssemblyCopyrightAttribute>(a => a.Copyright);

        public static string AssemblyCompany =>
            GetAttributeValue<AssemblyCompanyAttribute>(a => a.Company);

        public static string AssemblyConfiguration =>
            GetAttributeValue<AssemblyConfigurationAttribute>(a => a.Configuration);
    }
}
