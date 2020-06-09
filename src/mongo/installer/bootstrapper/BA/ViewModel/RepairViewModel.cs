using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MongoDB.Bootstrapper.BA.Util;
using MongoDB.Bootstrapper.BA.ViewModel;
using System.Windows.Input;

namespace MongoDB.Bootstrapper.BA.ViewModel
{
    public class RepairViewModel : ViewModelBase
    {
        private RootViewModel root;

        public RepairViewModel(RootViewModel root)
        {
            this.root = root;
        }

        #region Commands

        private RelayCommand repairCommand_ = null;
        public ICommand RepairCommand
        {
            get
            {
                if (repairCommand_ == null)
                {
                    this.repairCommand_ = new RelayCommand(
                        param =>
                        {
                            root.MainModel.PlannedAction = LaunchAction.Repair;
                            root.MainModel.Engine.Plan(root.MainModel.PlannedAction);
                        });
                }
                return repairCommand_;
            }
        }

        private RelayCommand uninstallCommand_ = null;
        public ICommand UninstallCommand
        {
            get
            {
                if (uninstallCommand_ == null)
                {
                    this.uninstallCommand_ = new RelayCommand(
                        param =>
                        {
                            root.MainModel.PlannedAction = LaunchAction.Uninstall;
                            root.MainModel.Engine.Plan(root.MainModel.PlannedAction);
                        });
                }
                return uninstallCommand_;
            }
        }

        #endregion
    }
}
