using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TcpListenerServer
{
    public static class ConfigurationManager
    {
        public static ServiceConfiguration GetConfiguration()
        {
            try
            {
                var processModule = Process.GetCurrentProcess().MainModule;
                if (processModule == null)
                    throw new NullReferenceException("Process.GetCurrentProcess().MainModule");

                var pathToOriginalExe = processModule.FileName;

                var pathToContentRoot = Path.GetDirectoryName(pathToOriginalExe);
                if (pathToContentRoot == null)
                    throw new NullReferenceException($"Path.GetDirectoryName('{pathToOriginalExe}')");

                var filepath = Path.Combine(pathToContentRoot, "TcpListener.config");

                if (!File.Exists(filepath))
                    throw new FileNotFoundException($"{filepath} not found");

                using (var reader = XmlReader.Create(filepath))
                {
                    return (ServiceConfiguration)new XmlSerializer(typeof(ServiceConfiguration)).Deserialize(reader);
                }
            }
            catch (Exception)
            {
                var configuration = new ServiceConfiguration();

                using (var textWriter = new StreamWriter("./TcpListener.config", false))
                    new XmlSerializer(typeof(ServiceConfiguration)).Serialize(textWriter, configuration);

                return configuration;
            }

        }
    }
}