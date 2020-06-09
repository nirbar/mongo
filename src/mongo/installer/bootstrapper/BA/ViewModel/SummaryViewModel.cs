using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MongoDB.Bootstrapper.BA.Util;
using System.Windows.Input;

namespace MongoDB.Bootstrapper.BA.ViewModel
{
    public class SummaryViewModel : ViewModelBase
    {
        private RootViewModel root;

        public SummaryViewModel(RootViewModel root)
        {
            this.root = root;
        }

        #region Commands

        private ICommand installCommand_;
        public ICommand InstallCommand
        {
            get
            {
                if (this.installCommand_ == null)
                {
                    this.installCommand_ = new RelayCommand(
                        param =>
                        {
                            root.MainModel.PlannedAction = LaunchAction.Install;
                            root.MainModel.Engine.Plan(root.MainModel.PlannedAction);
                        });
                }
                return this.installCommand_;
            }
        }

        #endregion
    }
}