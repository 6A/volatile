namespace Volatile
{
    using System.IO;
    using System.Reflection;

    static class DATA
    {
        public static UnmanagedMemoryStream Base64Exe
        {
            get
            {
                Assembly a = Assembly.GetExecutingAssembly();
                return (UnmanagedMemoryStream)a.GetManifestResourceStream(a.GetName().Name + '.' + "Exe.txt");
            }
        }

        public static UnmanagedMemoryStream Base64Content
        {
            get
            {
                Assembly a = Assembly.GetExecutingAssembly();
                return (UnmanagedMemoryStream)a.GetManifestResourceStream(a.GetName().Name + '.' + "DATA.txt");
            }
        }
    }
}
