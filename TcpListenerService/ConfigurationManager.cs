using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TcpListenerServer
{
    public static class ConfigurationManager
    {
        public static string GetConfigPath()
        {
            var processModule = Process.GetCurrentProcess().MainModule;
            if (processModule == null)
                throw new NullReferenceException("Process.GetCurrentProcess().MainModule");

            var pathToOriginalExe = processModule.FileName;

            var pathToContentRoot = Path.GetDirectoryName(pathToOriginalExe);
            if (pathToContentRoot == null)
                throw new NullReferenceException($"Path.GetDirectoryName('{pathToOriginalExe}')");

            return Path.Combine(pathToContentRoot, "TcpListener.config");
        }

        public static TcpServerConfiguration GetConfiguration(string filepath)
        {
            try
            {
                if (!File.Exists(filepath))
                    throw new FileNotFoundException($"{filepath} not found");

                using (var reader = XmlReader.Create(filepath))
                {
                    return (TcpServerConfiguration)new XmlSerializer(typeof(TcpServerConfiguration)).Deserialize(reader);
                }
            }
            catch (Exception)
            {
                var configuration = new TcpServerConfiguration();

                using (var textWriter = new StreamWriter(filepath, false))
                    new XmlSerializer(typeof(TcpServerConfiguration)).Serialize(textWriter, configuration);

                return configuration;
            }

        }
    }
}