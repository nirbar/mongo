using System;
using System.IO;
using System.Net;
using System.Xml;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MongoDB.Bootstrapper.BA.ViewModel;

namespace MongoDB.Bootstrapper.BA.Util
{
    public enum UpdateCheckResult
    {
        UpdateAvailable = 1000,
        UpdateNotAvailable = 1001,
        UpdateStarted = 1002,
        CheckError = 1003,
        DownloadError = 1004
    }
    public class MongoDbUpdate : ViewModelBase, IDisposable
    {
        private Engine eng_;
        private Uri feedUri_;
        private WebClient feedClient_ = new WebClient();
        private WebClient updateClient_ = new WebClient();
        private Version myVersion_;

        // Update details.
        private Uri updateUri_ = null;
        private long updateSize_ = 0;

        // retries
        private int checkRetry_ = 3;
        private int downloadRetry_ = 3;

        public MongoDbUpdate(Engine eng, string feedUrl)
        {
            try
            {
                eng_ = eng;
                feedUri_ = new Uri(feedUrl);
                myVersion_ = eng.VersionVariables["WixBundleVersion"];
                feedClient_.DownloadProgressChanged += FeedDownloadProgressChanged;
                feedClient_.DownloadStringCompleted += FeedDownloadStringCompleted;

                updateClient_.DownloadProgressChanged += UpdateDownloadProgressChanged;
                updateClient_.DownloadFileCompleted += UpdateDownloadCompleted;
            }
            catch(Exception ex)
            {
                eng_.Log(LogLevel.Error, "Failed initializing update check: " + ex.Message);
                HasErrors = true;
            }
        }

        public void BeginCheck()
        {
            try
            {
                Cancel();
                feedClient_.DownloadStringAsync(feedUri_);
            }
            catch(Exception ex)
            {
                eng_.Log(LogLevel.Error, "Failed starting update check: " + ex.Message);
                HasErrors = true;
            }
        }

        public void BeginDownload()
        {
            try
            {
                Cancel();
                if (updateUri_ != null)
                {
                    string name = Path.GetFileName(updateUri_.LocalPath);
                    downloadedFile_ = Path.Combine(Path.GetTempPath(), name);
                    updateClient_.DownloadFileAsync(updateUri_, downloadedFile_);
                }
            }
            catch (Exception ex)
            {
                eng_.Log(LogLevel.Error, "Failed starting download: " + ex.Message);
                HasErrors = true;
            }
        }

        public void Cancel()
        {
            if (feedClient_.IsBusy)
            {
                feedClient_.CancelAsync();
            }
            if (updateClient_.IsBusy)
            {
                updateClient_.CancelAsync();
            }
            if(!string.IsNullOrWhiteSpace(downloadedFile_) && File.Exists(downloadedFile_))
            {
                try
                {
                    File.Delete(downloadedFile_);
                }
                catch(Exception ex)
                {
                    eng_.Log(LogLevel.Error, "Failed deleting temporary file: " + ex.Message);
                }
            }
        }

        private void Clear()
        {
            if(!string.IsNullOrWhiteSpace(downloadedFile_) && File.Exists(downloadedFile_))
            {
                try
                {
                    File.Delete(downloadedFile_);
                }
                catch (Exception ex)
                {
                    eng_.Log(LogLevel.Error, "Failed deleting temporary file: " + ex.Message);
                }
            }

            CheckProgress = 0;
            updateUri_ = null;
            updateSize_ = 0;
            IsMandatory = false;
            UpdateAvailable = false;
            AvailableVersion = new Version(0, 0, 0, 0);
            AvailableTitle = string.Empty;
            DownloadProgress = 0;
            DownloadComplete = false;
            DownloadedFile = string.Empty;
        }

        #region Progress events

        void FeedDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            CheckProgress = e.Cancelled ? 0 : 100;
            if (e.Cancelled)
            {
                Clear();
            }
            else if (e.Error != null)
            {
                Clear();
                if (!RetryCheck())
                {
                    eng_.Log(LogLevel.Error, "Failed downloading feed");
                    HasErrors = true;
                }
            }
            else
            {
                try
                {
                    ParseFeed(e.Result);
                }
                catch(Exception ex)
                {
                    if (!RetryCheck())
                    {
                        eng_.Log(LogLevel.Error, "Failed parsing feed: " + ex.Message);
                    }
                }
            }
        }

        private bool RetryCheck()
        {
            if (--checkRetry_ > 0)
            {
                eng_.Log(LogLevel.Error, string.Format("Failed checking update feed. Trying up to {0} more time(s)", checkRetry_));
                BeginCheck();
                return true;
            }
            else
            {
                eng_.Log(LogLevel.Error, "Failed checking update feed");
                HasErrors = true;
                return false;
            }
        }

        private bool RetryDownload()
        {
            if (--downloadRetry_ > 0)
            {
                eng_.Log(LogLevel.Error, string.Format("Failed downloading update. Trying up to {0} more time(s)", downloadRetry_));
                BeginDownload();
                return true;
            }
            else
            {
                eng_.Log(LogLevel.Error, "Failed downloading update");
                HasErrors = true;
                return false;
            }
        }

        void FeedDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            CheckProgress = e.ProgressPercentage;
        }

        void UpdateDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadProgress = (int)((e.TotalBytesToReceive == 0) ? e.BytesReceived / updateSize_ : e.ProgressPercentage);
        }

        void UpdateDownloadCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Clear();
            }
            else if(e.Error != null)
            {
                Clear();
                if (!RetryDownload())
                {
                    eng_.Log(LogLevel.Error, "Failed downloading update");
                    HasErrors = true;
                }
            }
            else
            {
                DownloadProgress = 100;
                DownloadComplete = true;
            }
        }

        #endregion

        private void ParseFeed(string feed)
        {
            XmlDocument feedXml = new XmlDocument();
            feedXml.LoadXml(feed);

            XmlNamespaceManager ns = new XmlNamespaceManager(feedXml.NameTable);
            ns.AddNamespace("as", "http://appsyndication.org/2006/appsyn");
            ns.AddNamespace("def", "http://www.w3.org/2005/Atom");

            string search = "/def:feed/def:entry";
            XmlNodeList nodes = feedXml.SelectNodes(search, ns);

            bool hasMandatory = false;
            Version maxVersion = null;
            string maxVersionTitle = null;
            long maxVersionSize = 0;
            string maxVersionLink = null;
            foreach (XmlNode n in nodes)
            {
                // Sanity check on the entry
                XmlNode titleNode = n.SelectSingleNode("def:title", ns);
                XmlNode versionNode = n.SelectSingleNode("as:version", ns);
                XmlNode uriNode = n.SelectSingleNode("def:link[@rel='enclosure']", ns);
                long length;
                string href;
                if ((titleNode == null)
                    || string.IsNullOrWhiteSpace(titleNode.InnerText)
                    || (versionNode == null)
                    || string.IsNullOrWhiteSpace(versionNode.InnerText)
                    || (uriNode == null)
                    || (uriNode.Attributes["href"] == null)
                    || string.IsNullOrWhiteSpace(href = uriNode.Attributes["href"].Value)
                    || (uriNode.Attributes["length"] == null)
                    || string.IsNullOrWhiteSpace(uriNode.Attributes["length"].Value)
                    || !long.TryParse(uriNode.Attributes["length"].Value, out length)
                    || (length <= 0)
                    )
                {
                    continue;
                }

                // Mandatory?
                if (titleNode.InnerText.StartsWith("MANDATORY"))
                {
                    hasMandatory = true;
                }

                // Parse version
                string verStr = versionNode.InnerText;
                verStr = verStr.TrimStart('v');
                Version ver;
                if (!Version.TryParse(verStr, out ver))
                {
                    continue;
                }

                if ((ver > myVersion_) && ((maxVersion == null) || (ver > maxVersion)))
                {
                    maxVersion = ver;
                    maxVersionLink = href;
                    maxVersionSize = length;
                    maxVersionTitle = titleNode.Value;
                }
            }

            if (maxVersion != null)
            {
                UpdateAvailable = true;
                IsMandatory = hasMandatory;
                AvailableTitle = maxVersionTitle;
                updateUri_ = new Uri(maxVersionLink);
                updateSize_ = maxVersionSize;
            }
            else
            {
                Clear();
            }
        }

        #region Properties

        #region Update Check

        private int feedProgress_ = 0;
        public int CheckProgress
        {
            get
            {
                return feedProgress_;
            }
            private set
            {
                feedProgress_ = value;
                OnPropertyChanged("CheckProgress");
                OnPropertyChanged("IsBusy");
            }
        }

        private bool isMandatory_ = false;
        public bool IsMandatory
        {
            get
            {
                return isMandatory_;
            }
            private set
            {
                isMandatory_ = value;
                OnPropertyChanged("IsMandatory");
            }
        }

        private bool updateAvailable_ = false;
        public bool UpdateAvailable
        {
            get
            {
                return updateAvailable_;
            }
            private set
            {
                updateAvailable_ = value;
                OnPropertyChanged("UpdateAvailable");
                OnPropertyChanged("IsBusy");
            }
        }

        private Version availableVersion_ = new Version(0, 0, 0, 0);
        public Version AvailableVersion
        {
            get
            {
                return availableVersion_;
            }
            private set
            {
                availableVersion_ = value;
                OnPropertyChanged("AvailableVersion");
            }
        }

        private string availableTitle_ = string.Empty;
        public string AvailableTitle
        {
            get
            {
                return availableTitle_;
            }
            private set
            {
                availableTitle_ = value;
                OnPropertyChanged("AvailableTitle");
            }
        }

        #endregion

        #region Download

        private int downloadProgress_ = 0;
        public int DownloadProgress
        {
            get
            {
                return downloadProgress_;
            }
            private set
            {
                downloadProgress_ = value;
                OnPropertyChanged("DownloadProgress");
                OnPropertyChanged("IsBusy");
            }
        }

        private bool downloadComplete_ = false;
        public bool DownloadComplete
        {
            get
            {
                return downloadComplete_;
            }
            private set
            {
                downloadComplete_ = value;
                OnPropertyChanged("DownloadComplete");
                OnPropertyChanged("IsBusy");
            }
        }

        private string downloadedFile_ = string.Empty;
        public string DownloadedFile
        {
            get
            {
                return downloadComplete_ ? downloadedFile_ : string.Empty;
            }
            private set
            {
                downloadedFile_ = value;
                OnPropertyChanged("DownloadedFile");
            }
        }

        #endregion

        public bool IsBusy
        {
            get
            {
                return feedClient_.IsBusy || updateClient_.IsBusy;
            }
        }

        private bool hasErrors_ = false;
        public bool HasErrors
        {
            get
            {
                return hasErrors_;
            }
            set
            {
                hasErrors_ = value;
                OnPropertyChanged("HasErrors");
            }
        }

        #endregion

        public void Dispose()
        {
            feedClient_.Dispose();
            updateClient_.Dispose();
        }
    }
}
