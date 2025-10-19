using System;
using System.IO;
using System.Reflection;

namespace Algoloop.Wpf.Model
{
    public static class MainService
    {
        public static string GetProgramFolder()
        {
            string unc = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(unc) ?? string.Empty;
        }

        public static string GetProgramDataFolder()
        {
            string programDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AboutModel.Product);
            
            if (!Directory.Exists(programDataFolder))
            {
                Directory.CreateDirectory(programDataFolder);
            }

            return programDataFolder;
        }
    }
}
