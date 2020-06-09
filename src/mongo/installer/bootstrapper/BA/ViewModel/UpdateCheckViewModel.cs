using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MongoDB.Bootstrapper.BA.Util;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;

namespace MongoDB.Bootstrapper.BA.ViewModel
{
    public class UpdateCheckViewModel : ViewModelBase
    {
        private RootViewModel root;

        public UpdateCheckViewModel(RootViewModel root)
        {
            this.root = root;
            root.MainModel.UpdateModel.PropertyChanged += updater_PropertyChanged;
        }

        void updater_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "CheckProgress":
                    OnPropertyChanged("CheckProgress");
                    OnPropertyChanged("UpdateNowEnabled");
                    if (root.MainModel.UpdateModel.CheckProgress != 0)
                    {
                        root.TaskBarProgressValue = root.MainModel.UpdateModel.CheckProgress / 100.0;
                        root.TaskBarProgressState = TaskbarItemProgressState.Normal;
                    }
                    break;

                case "UpdateAvailable":
                    OnPropertyChanged("SkipEnabled");
                    OnPropertyChanged("UpdateNowEnabled");
                    OnPropertyChanged("AskUpdateVisibility");
                    OnPropertyChanged("DownloadingVisibility");
                    OnPropertyChanged("MandatoryUpdateVisibility");
                    root.TaskBarProgressState = TaskbarItemProgressState.None;
                    if (root.MainModel.UpdateModel.UpdateAvailable)
                    {
                        //TODO navigate to UpdateAvailableView
                    }
                    break;

                case "IsMandatory":
                    OnPropertyChanged("SkipEnabled");
                    OnPropertyChanged("AskUpdateVisibility");
                    OnPropertyChanged("DownloadingVisibility");
                    OnPropertyChanged("MandatoryUpdateVisibility");
                    break;

                case "DownloadProgress":
                    OnPropertyChanged("UpdateNowEnabled");
                    OnPropertyChanged("DownloadProgress");
                    if (root.MainModel.UpdateModel.DownloadProgress != 0)
                    {
                        root.TaskBarProgressValue = root.MainModel.UpdateModel.DownloadProgress / 100.0;
                        root.TaskBarProgressState = TaskbarItemProgressState.Normal;
                    }
                    break;

                case "DownloadComplete":
                    if (root.MainModel.UpdateModel.DownloadComplete)
                    {
                        root.MainModel.Result = (int)UpdateCheckResult.UpdateStarted;
                        root.TaskBarProgressState = TaskbarItemProgressState.None;
                        System.Diagnostics.Process p = new System.Diagnostics.Process();
                        p.StartInfo = new System.Diagnostics.ProcessStartInfo(root.MainModel.UpdateModel.DownloadedFile);
                        p.Start();
                        root.Dispatcher.Invoke((Action)delegate()
                        {
                            MongoDbBA.View.Close();
                        });
                    }
                    break;

                case "IsBusy":
                    OnPropertyChanged("UpdateNowEnabled");
                    OnPropertyChanged("AskUpdateVisibility");
                    OnPropertyChanged("DownloadingVisibility");
                    OnPropertyChanged("MandatoryUpdateVisibility");
                    break;

                case "HasErrors":
                    root.MainModel.Engine.Log(LogLevel.Error, "Error detected during update process. Skipping update");
                    //TODO Navigate to Eula
                    break;
            }
        }

        public int CheckProgress
        {
            get
            {
                return root.MainModel.UpdateModel.CheckProgress;
            }
            set { }
        }

        public int DownloadProgress
        {
            get
            {
                return root.MainModel.UpdateModel.DownloadProgress;
            }
            set { }
        }

        private ICommand updateCommand_ = null;
        public ICommand UpdateNowCommand
        {
            get
            {
                if (this.updateCommand_ == null)
                {
                    this.updateCommand_ = new RelayCommand(
                        param =>
                        {
                            root.MainModel.UpdateModel.BeginDownload();
                        },
                        param =>
                        {
                            return (root.MainModel.UpdateModel.UpdateAvailable
                                && !root.MainModel.UpdateModel.DownloadComplete
                                && !root.MainModel.UpdateModel.IsBusy);
                        }
                    );
                }
                return this.updateCommand_;
            }
        }

        public bool UpdateNowEnabled
        {
            get
            {
                return UpdateNowCommand.CanExecute(this);
            }
        }

        public Visibility AskUpdateVisibility
        {
            get
            {
                return (root.MainModel.UpdateModel.UpdateAvailable && !root.MainModel.UpdateModel.IsMandatory && !root.MainModel.UpdateModel.IsBusy)
                    ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public Visibility MandatoryUpdateVisibility
        {
            get
            {
                return (root.MainModel.UpdateModel.UpdateAvailable && root.MainModel.UpdateModel.IsMandatory && !root.MainModel.UpdateModel.IsBusy)
                    ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public Visibility DownloadingVisibility
        {
            get
            {
                return (root.MainModel.UpdateModel.UpdateAvailable && root.MainModel.UpdateModel.IsBusy)
                    ? Visibility.Visible : Visibility.Hidden;
            }
        }
    }
}