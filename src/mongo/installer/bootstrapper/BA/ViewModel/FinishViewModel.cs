using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MongoDB.Bootstrapper.BA.Util;
using System.ComponentModel;
using System.Windows.Input;
using MongoDB.Bootstrapper.BA.ViewModel;

namespace MongoDB.Bootstrapper.BA
{
    public class FinishViewModel : ViewModelBase
    {
        private RootViewModel root;

        public FinishViewModel(RootViewModel root)
        {
            this.root = root;
            this.root.PropertyChanged += this.RootPropertyChanged;
        }

        void RootPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ("InstallState" == e.PropertyName)
            {
                base.OnPropertyChanged("CompleteText");
            }
            else if ("RebootState" == e.PropertyName)
            {
                base.OnPropertyChanged("RebootEnabled");
            }
        }

        public string CompleteText
        {
            get
            {
                switch(root.InstallState)
                {
                    case InstallationState.Applied:
                        return "Setup completed successfully";

                    case InstallationState.Failed:
                        if (root.Canceled)
                        {
                            return "Setup was canceled";
                        }
                        else
                        {
                            return "Setup failed";
                        }

                    default:
                        return "";
                }
            }
        }

        public ICommand rebootCommand_ = null;
        public ICommand RebootCommand
        {
            get
            {
                if (this.rebootCommand_ == null)
                {
                    this.rebootCommand_ = new RelayCommand(param => 
                        {
                            root.MainModel.RebootRequested = true;
                            MongoDbBA.View.Close();
                        }
                        , o => (root.RebootState == ApplyRestart.RestartRequired)
                        );
                }

                return this.rebootCommand_;
            }
        }

        public bool RebootEnabled
        {
            get
            {
                return RebootCommand.CanExecute(this);
            }
        }
    }
}
