using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MongoDB.Bootstrapper.BA.Util;
using System;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace MongoDB.Bootstrapper.BA.ViewModel
{
    public class VariablesViewModel : ViewModelBase
    {
        private Engine engine_;

        public VariablesViewModel(Engine eng)
        {
            engine_ = eng;
        }

        public Version WixBundleVersion => engine_.VersionVariables["WixBundleVersion"];

        public string INSTALL_FOLDER
        {
            get
            {
                return engine_.FormatStringEx(engine_.StringVariables["INSTALL_FOLDER"]);
            }
            set
            {
                engine_.StringVariables["INSTALL_FOLDER"] = engine_.FormatStringEx(value);
                OnPropertyChanged("INSTALL_FOLDER");
            }
        }

        public string MONGO_DATA_PATH
        {
            get
            {
                return engine_.FormatStringEx(engine_.StringVariables["MONGO_DATA_PATH"]);
            }
            set
            {
                engine_.StringVariables["MONGO_DATA_PATH"] = engine_.FormatStringEx(value);
                OnPropertyChanged("MONGO_DATA_PATH");
            }
        }

        public string MONGO_LOG_PATH
        {
            get
            {
                return engine_.FormatStringEx(engine_.StringVariables["MONGO_LOG_PATH"]);
            }
            set
            {
                engine_.StringVariables["MONGO_LOG_PATH"] = engine_.FormatStringEx(value);
                OnPropertyChanged("MONGO_LOG_PATH");
            }
        }

        public void ValidateTargetFolder()
        {
            if (string.IsNullOrWhiteSpace(INSTALL_FOLDER) || (INSTALL_FOLDER.IndexOfAny(Path.GetInvalidPathChars()) >= 0) || !Path.IsPathRooted(INSTALL_FOLDER))
            {
                throw new Exception("Please enter a full path to installation folder");
            }
            if (!INSTALL_SERVER)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(MONGO_DATA_PATH) || (MONGO_DATA_PATH.IndexOfAny(Path.GetInvalidPathChars()) >= 0) || !Path.IsPathRooted(MONGO_DATA_PATH))
            {
                throw new Exception("Please enter a full path to MongoDB Server data folder");
            }
            if (string.IsNullOrWhiteSpace(MONGO_LOG_PATH) || (MONGO_LOG_PATH.IndexOfAny(Path.GetInvalidPathChars()) >= 0) || !Path.IsPathRooted(MONGO_LOG_PATH))
            {
                throw new Exception("Please enter a full path to MongoDB Server log folder");
            }
        }

        public bool MONGO_SERVICE_INSTALL
        {
            get
            {
                if (!engine_.StringVariables.Contains("MONGO_SERVICE_INSTALL") || !long.TryParse(engine_.StringVariables["MONGO_SERVICE_INSTALL"], out long l))
                {
                    engine_.NumericVariables["MONGO_SERVICE_INSTALL"] = 1;
                    return true;
                }
                return (l != 0);
            }
            set
            {
                engine_.NumericVariables["MONGO_SERVICE_INSTALL"] = value ? 1 : 0;
                if (!value)
                {
                    MONGO_SERVICE_ACCOUNT_TYPE = ServiceAccountType.ServiceCustomAccount;
                    MONGO_SERVICE_NAME = "";
                }

                OnPropertyChanged("MONGO_SERVICE_INSTALL");
            }
        }

        public enum ServiceAccountType
        {
            ServiceLocalNetwork,
            ServiceCustomAccount
        }

        public ServiceAccountType MONGO_SERVICE_ACCOUNT_TYPE
        {
            get
            {
                if (!engine_.StringVariables.Contains("MONGO_SERVICE_ACCOUNT_TYPE") || !Enum.TryParse<ServiceAccountType>(engine_.StringVariables["MONGO_SERVICE_ACCOUNT_TYPE"], out ServiceAccountType sat))
                {
                    engine_.StringVariables["MONGO_SERVICE_ACCOUNT_TYPE"] = ServiceAccountType.ServiceLocalNetwork.ToString();
                    return ServiceAccountType.ServiceLocalNetwork;
                }
                return sat;
            }
            set
            {
                engine_.StringVariables["MONGO_SERVICE_ACCOUNT_TYPE"] = value.ToString();
                OnPropertyChanged("MONGO_SERVICE_ACCOUNT_TYPE");
                OnPropertyChanged("MONGO_SERVICE_ACCOUNT");
            }
        }

        public string MONGO_SERVICE_NAME
        {
            get
            {
                return engine_.FormatStringEx(engine_.StringVariables["MONGO_SERVICE_NAME"]);
            }
            set
            {
                engine_.StringVariables["MONGO_SERVICE_NAME"] = value;
                OnPropertyChanged("MONGO_SERVICE_NAME");
            }
        }

        public string MONGO_SERVICE_ACCOUNT
        {
            get
            {
                if (MONGO_SERVICE_ACCOUNT_TYPE == ServiceAccountType.ServiceLocalNetwork)
                {
                    engine_.StringVariables["MONGO_SERVICE_ACCOUNT_DOMAIN"] = "";
                    engine_.StringVariables["MONGO_SERVICE_ACCOUNT_NAME"] = "";
                    return "";
                }

                string domain = null;
                string user = null;
                if (engine_.StringVariables.Contains("MONGO_SERVICE_ACCOUNT_NAME") && !string.IsNullOrEmpty(engine_.StringVariables["MONGO_SERVICE_ACCOUNT_NAME"]))
                {
                    user = engine_.FormatStringEx(engine_.StringVariables["MONGO_SERVICE_ACCOUNT_NAME"]);
                    ParseUserAccount(user, out domain, out user);
                    if (!string.IsNullOrEmpty(domain))
                    {
                        engine_.StringVariables["MONGO_SERVICE_ACCOUNT_DOMAIN"] = domain;
                    }
                }
                if (engine_.StringVariables.Contains("MONGO_SERVICE_ACCOUNT_DOMAIN") && !string.IsNullOrEmpty(engine_.StringVariables["MONGO_SERVICE_ACCOUNT_DOMAIN"]))
                {
                    domain = engine_.FormatStringEx(engine_.StringVariables["MONGO_SERVICE_ACCOUNT_DOMAIN"]);
                }

                if (string.IsNullOrWhiteSpace(domain) && string.IsNullOrWhiteSpace(user))
                {
                    return "";
                }

                return $"{domain ?? ""}\\{user ?? ""}";
            }
            set
            {
                ParseUserAccount(value, out string domain, out string user);
                if (string.IsNullOrEmpty(domain))
                {
                    domain = ".";
                }
                engine_.StringVariables["MONGO_SERVICE_ACCOUNT_DOMAIN"] = domain;
                engine_.StringVariables["MONGO_SERVICE_ACCOUNT_NAME"] = user;
                OnPropertyChanged("MONGO_SERVICE_ACCOUNT");
            }
        }

        private void ParseUserAccount(string fullName, out string domain, out string name)
        {
            name = fullName;
            domain = null;

            int i = fullName.IndexOf('@');
            if ((i > 0) && (i < (fullName.Length - 1)))
            {
                domain = fullName.Substring(i + 1).Trim('\\', '@');
                name = fullName.Substring(0, i).Trim('\\', '@');
                return;
            }

            i = fullName.IndexOf('\\');
            if ((i > 0) && (i < (fullName.Length - 1)))
            {
                domain = fullName.Substring(0, i).Trim('\\', '@');
                name = fullName.Substring(i + 1).Trim('\\', '@');
                return;
            }
        }

        public SecureString MONGO_SERVICE_ACCOUNT_PASSWORD
        {
            get
            {
                if (engine_.SecureStringVariables.Contains("MONGO_SERVICE_ACCOUNT_PASSWORD"))
                {
                    return engine_.SecureStringVariables["MONGO_SERVICE_ACCOUNT_PASSWORD"];
                }

                return new SecureString();
            }
            set
            {
                engine_.SecureStringVariables["MONGO_SERVICE_ACCOUNT_PASSWORD"] = value;
                OnPropertyChanged("MONGO_SERVICE_ACCOUNT_PASSWORD");
            }
        }

        public void ValidateServiceAccount()
        {
            if (!INSTALL_SERVER && !INSTALL_SERVER_SERVER || !MONGO_SERVICE_INSTALL)
            {
                return;
            }
            if (string.IsNullOrEmpty(MONGO_SERVICE_NAME) || (MONGO_SERVICE_NAME.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            {
                throw new Exception("Please enter a valid service name");
            }
            if (MONGO_SERVICE_ACCOUNT_TYPE != ServiceAccountType.ServiceCustomAccount)
            {
                return;
            }
            if (string.IsNullOrEmpty(MONGO_SERVICE_ACCOUNT))
            {
                throw new Exception("Please eneter service account name");
            }

            string domain = engine_.StringVariables["MONGO_SERVICE_ACCOUNT_DOMAIN"];
            string user = engine_.StringVariables["MONGO_SERVICE_ACCOUNT_NAME"];
            ContextType ctx = ContextType.Domain;
            if (string.IsNullOrWhiteSpace(domain) || domain.Equals(".") || domain.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
            {
                ctx = ContextType.Machine;
                domain = Environment.MachineName;
            }
            using (PrincipalContext pc = new PrincipalContext(ctx, domain))
            {
                IntPtr pPsw = IntPtr.Zero;
                try
                {
                    pPsw = Marshal.SecureStringToGlobalAllocUnicode(MONGO_SERVICE_ACCOUNT_PASSWORD);

                    // Validate credentials with minimal keep of managed plain password.
                    if (!pc.ValidateCredentials(user, Marshal.PtrToStringUni(pPsw), ContextOptions.Negotiate))
                    {
                        throw new Exception("Invalid user name or password");
                    }
                }
                finally
                {
                    if (pPsw != IntPtr.Zero)
                    {
                        Marshal.ZeroFreeGlobalAllocUnicode(pPsw);
                    }
                }
            }
        }

        #region Package selection

        public bool INSTALL_SERVER
        {
            get
            {
                if (!engine_.StringVariables.Contains("INSTALL_SERVER") || !long.TryParse(engine_.StringVariables["INSTALL_SERVER"], out long l))
                {
                    engine_.NumericVariables["INSTALL_SERVER"] = 1;
                    return true;
                }
                return (l != 0);
            }
            set
            {
                engine_.NumericVariables["INSTALL_SERVER"] = value ? 1 : 0;
                OnPropertyChanged("INSTALL_SERVER");
            }
        }

        public bool INSTALL_SERVER_SERVER
        {
            get
            {
                if (!engine_.StringVariables.Contains("INSTALL_SERVER_SERVER") || !long.TryParse(engine_.StringVariables["INSTALL_SERVER_SERVER"], out long l))
                {
                    engine_.NumericVariables["INSTALL_SERVER_SERVER"] = 1;
                    return true;
                }
                return (l != 0);
            }
            set
            {
                engine_.NumericVariables["INSTALL_SERVER_SERVER"] = value ? 1 : 0;
                OnPropertyChanged("INSTALL_SERVER_SERVER");
            }
        }

        public bool INSTALL_SERVER_CLIENT
        {
            get
            {
                if (!engine_.StringVariables.Contains("INSTALL_SERVER_CLIENT") || !long.TryParse(engine_.StringVariables["INSTALL_SERVER_CLIENT"], out long l))
                {
                    engine_.NumericVariables["INSTALL_SERVER_CLIENT"] = 1;
                    return true;
                }
                return (l != 0);
            }
            set
            {
                engine_.NumericVariables["INSTALL_SERVER_CLIENT"] = value ? 1 : 0;
                OnPropertyChanged("INSTALL_SERVER_CLIENT");
            }
        }

        public bool INSTALL_SERVER_ROUTER
        {
            get
            {
                if (!engine_.StringVariables.Contains("INSTALL_SERVER_ROUTER") || !long.TryParse(engine_.StringVariables["INSTALL_SERVER_ROUTER"], out long l))
                {
                    engine_.NumericVariables["INSTALL_SERVER_ROUTER"] = 1;
                    return true;
                }
                return (l != 0);
            }
            set
            {
                engine_.NumericVariables["INSTALL_SERVER_ROUTER"] = value ? 1 : 0;
                OnPropertyChanged("INSTALL_SERVER_ROUTER");
            }
        }

        public bool INSTALL_SERVER_IMPORTEXPORTTOOLS
        {
            get
            {
                if (!engine_.StringVariables.Contains("INSTALL_SERVER_IMPORTEXPORTTOOLS") || !long.TryParse(engine_.StringVariables["INSTALL_SERVER_IMPORTEXPORTTOOLS"], out long l))
                {
                    engine_.NumericVariables["INSTALL_SERVER_IMPORTEXPORTTOOLS"] = 1;
                    return true;
                }
                return (l != 0);
            }
            set
            {
                engine_.NumericVariables["INSTALL_SERVER_IMPORTEXPORTTOOLS"] = value ? 1 : 0;
                OnPropertyChanged("INSTALL_SERVER_IMPORTEXPORTTOOLS");
            }
        }

        public bool INSTALL_SERVER_MONITORINGTOOLS
        {
            get
            {
                if (!engine_.StringVariables.Contains("INSTALL_SERVER_MONITORINGTOOLS") || !long.TryParse(engine_.StringVariables["INSTALL_SERVER_MONITORINGTOOLS"], out long l))
                {
                    engine_.NumericVariables["INSTALL_SERVER_MONITORINGTOOLS"] = 1;
                    return true;
                }
                return (l != 0);
            }
            set
            {
                engine_.NumericVariables["INSTALL_SERVER_MONITORINGTOOLS"] = value ? 1 : 0;
                OnPropertyChanged("INSTALL_SERVER_MONITORINGTOOLS");
            }
        }

        public bool INSTALL_SERVER_MISCELLANEOUSTOOLS
        {
            get
            {
                if (!engine_.StringVariables.Contains("INSTALL_SERVER_MISCELLANEOUSTOOLS") || !long.TryParse(engine_.StringVariables["INSTALL_SERVER_MISCELLANEOUSTOOLS"], out long l))
                {
                    engine_.NumericVariables["INSTALL_SERVER_MISCELLANEOUSTOOLS"] = 1;
                    return true;
                }
                return (l != 0);
            }
            set
            {
                engine_.NumericVariables["INSTALL_SERVER_MISCELLANEOUSTOOLS"] = value ? 1 : 0;
                OnPropertyChanged("INSTALL_SERVER_MISCELLANEOUSTOOLS");
            }
        }

        public bool INSTALL_COMPASS
        {
            get
            {
                if (!engine_.StringVariables.Contains("INSTALL_COMPASS") || !long.TryParse(engine_.StringVariables["INSTALL_COMPASS"], out long l))
                {
                    engine_.NumericVariables["INSTALL_COMPASS"] = 1;
                    return true;
                }
                return (l != 0);
            }
            set
            {
                engine_.NumericVariables["INSTALL_COMPASS"] = value ? 1 : 0;
                OnPropertyChanged("INSTALL_COMPASS");
            }
        }

        public bool INSTALL_TOOLS
        {
            get
            {
                if (!engine_.StringVariables.Contains("INSTALL_TOOLS") || !long.TryParse(engine_.StringVariables["INSTALL_TOOLS"], out long l))
                {
                    engine_.NumericVariables["INSTALL_TOOLS"] = 1;
                    return true;
                }
                return (l != 0);
            }
            set
            {
                engine_.NumericVariables["INSTALL_TOOLS"] = value ? 1 : 0;
                OnPropertyChanged("INSTALL_TOOLS");
            }
        }

        public void ValidateFeatureSelection()
        {
            if (!INSTALL_SERVER && !INSTALL_COMPASS && !INSTALL_TOOLS)
            {
                throw new Exception("Please select at least one package to install");
            }
            if (INSTALL_SERVER)
            {
                if (!INSTALL_SERVER_CLIENT && !INSTALL_SERVER_IMPORTEXPORTTOOLS && !INSTALL_SERVER_MISCELLANEOUSTOOLS && !INSTALL_SERVER_MONITORINGTOOLS && !INSTALL_SERVER_ROUTER && !INSTALL_SERVER_SERVER)
                {
                    throw new Exception("Please select at least one Server feature to install");
                }
            }
        }

        #endregion
    }
}