using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

namespace MongoDB.Bootstrapper.BA.Util
{
    public static class EngineEx
    {
        public static void ParseCommandLine(this Engine eng, BootstrapperApplication bootstrapper)
        {
            // Get array of arguments based on the system parsing algorithm.
            string[] args = bootstrapper.Command.GetCommandLineArgs();

            // Path to BootstrapperApplicationData.xml
            string badPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            badPath = Path.GetDirectoryName(badPath);
            badPath = Path.Combine(badPath, "BootstrapperApplicationData.xml");
            if (!File.Exists(badPath))
            {
                eng.Log(LogLevel.Error, string.Format("Strange... I expected to find the file {0}", badPath));
                return;
            }

            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(badPath);

            Regex rx = new Regex(@"^\s*(?<name>[^=']*)\s*=(?<value>.*)$");

            for (int i = 0; i < args.Length; ++i)
            {
                // Get command line var=value pair
                Match m = rx.Match(args[i]);
                if (!m.Success)
                {
                    continue;
                }

                string varName = m.Groups["name"].Value;
                string val = m.Groups["value"].Value;

                // Variable exists?
                if (!eng.StringVariables.Contains(varName))
                {
                    eng.Log(LogLevel.Error, string.Format("Variable '{0}' is undefined.", varName));
                    continue;
                }

                // Check if this variable is overidable
                string xpath = string.Format("//*[local-name()='WixStdbaOverridableVariable' and ./@Name='{0}']", varName);
                if (xDoc.DocumentElement.SelectSingleNode(xpath) == null)
                {
                    eng.Log(LogLevel.Error, string.Format("Can't set '{0}' from the command line.", varName));
                    continue;
                }

                eng.StringVariables[varName] = val;
            }
        }
        public static void EvaluateConditions(this Engine eng)
        {
            // Path to BootstrapperApplicationData.xml
            string badPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            badPath = Path.GetDirectoryName(badPath);
            badPath = Path.Combine(badPath, "BootstrapperApplicationData.xml");
            if (!File.Exists(badPath))
            {
                eng.Log(LogLevel.Error, string.Format("Strange... I expected to find the file {0}", badPath));
                throw new FileNotFoundException("BootstrapperApplicationData.xml is missing");
            }

            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(badPath);
            XmlNodeList conditions = xDoc.DocumentElement.SelectNodes("//*[local-name()='WixBalCondition']");
            foreach (XmlNode n in conditions)
            {
                XmlAttribute attr = n.Attributes["Condition"];
                if (attr == null)
                {
                    throw new XmlException("BootstrapperApplicationData.xml is corrupt");
                }

                string cond = attr.Value;

                if (eng.EvaluateCondition(cond))
                {
                    continue;
                }

                attr = n.Attributes["Message"];
                if (attr == null)
                {
                    throw new XmlException("BootstrapperApplicationData.xml is corrupt");
                }
                string msg = attr.Value;
                msg = eng.FormatStringEx(msg);
                eng.Log(LogLevel.Error, $"Condition enavluated to false: {msg}");
                throw new Exception(msg);
            }
        }

        public static string FormatStringEx(this Engine eng, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            return eng.FormatString(value);
        }
    }
}
