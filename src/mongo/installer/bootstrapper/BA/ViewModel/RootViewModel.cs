using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MongoDB.Bootstrapper.BA.Model;
using MongoDB.Bootstrapper.BA.Util;
using MongoDB.Bootstrapper.BA.View;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using System.Windows.Threading;

namespace MongoDB.Bootstrapper.BA.ViewModel
{
    public enum Error
    {
        UserCancelled = 1223,
    }


    [Flags]
    public enum MsiButtons
    {
        MB_ABORTRETRYIGNORE = 0x00000002,
        MB_CANCELTRYCONTINUE = 0x00000006,
        MB_OK = 0x00000000,
        MB_OKCANCEL = 0x00000001,
        MB_RETRYCANCEL = 0x00000005,
        MB_YESNO = 0x00000004,
        MB_YESNOCANCEL = 0x00000003,

        MB_BUTTONSMASK = MB_ABORTRETRYIGNORE | MB_CANCELTRYCONTINUE | MB_OK | MB_OKCANCEL | MB_RETRYCANCEL | MB_YESNO | MB_YESNOCANCEL,
    }

    public class RootViewModel : ViewModelBase
    {
        private bool canceled;
        private InstallationState installState = InstallationState.Detecting;
        private DetectionState detectState;
        public RootViewModel(MainModel model)
        {
            MainModel = model;
            Dispatcher = Dispatcher.CurrentDispatcher;
            MainModel.Bootstrapper.Error += Bootstrapper_Error;
        }

        public MainModel MainModel { get; private set; }

        public Dispatcher Dispatcher { get; private set; }

        #region TaskBar Progress

        private double taskBarProgressValue_ = 0;
        public double TaskBarProgressValue
        {
            get
            {
                return taskBarProgressValue_;
            }
            set
            {
                taskBarProgressValue_ = value;
                OnPropertyChanged("TaskBarProgressValue");
            }
        }

        private TaskbarItemProgressState taskBarProgressState_ = TaskbarItemProgressState.None;
        public TaskbarItemProgressState TaskBarProgressState
        {
            get
            {
                return taskBarProgressState_;
            }
            set
            {
                taskBarProgressState_ = value;
                OnPropertyChanged("TaskBarProgressState");
            }
        }

        #endregion

        #region Commands

        private RelayCommand minimizeCommand = null;
        public ICommand MinimizeCommand
        {
            get
            {
                if (this.minimizeCommand == null)
                {
                    this.minimizeCommand = new RelayCommand(param =>
                    {
                        MongoDbBA.View.WindowState = WindowState.Minimized;
                    });
                }

                return this.minimizeCommand;
            }
        }

        #endregion

        private void Bootstrapper_Error(object sender, ErrorEventArgs e)
        {
            lock (this)
            {
                TaskBarProgressValue = 1;
                TaskBarProgressState = TaskbarItemProgressState.Error;

                if (Canceled)
                {
                    e.Result = Result.Cancel;
                    return;
                }

                // If the error is a cancel coming from the engine during apply we want to go back to the preapply state.
                if (InstallationState.Applying == InstallState && (int)Error.UserCancelled == e.ErrorCode)
                {
                    InstallState = PreApplyState;
                    return;
                }
                if (MainModel.Command.Display == Display.Embedded)
                {
                    e.Result = (Result)MainModel.Engine.SendEmbeddedError(e.ErrorCode, e.ErrorMessage, e.UIHint);
                    return;
                }
                if (Display.Full != MainModel.Command.Display)
                {
                    return;
                }
                // On HTTP authentication errors, have the engine try to do authentication for us.
                if (ErrorType.HttpServerAuthentication == e.ErrorType || ErrorType.HttpProxyAuthentication == e.ErrorType)
                {
                    e.Result = Result.TryAgain;
                    return;
                }

                // Parse hint flags and prompt
                e.Result = PopupViewModel.ShowSync(e.UIHint, "Error", e.ErrorMessage);
           }
        }

        public IntPtr ViewWindowHandle { get; set; }

        public bool Canceled
        {
            get
            {
                return this.canceled;
            }

            set
            {
                if (this.canceled != value)
                {
                    this.canceled = value;
                    base.OnPropertyChanged("Canceled");
                }
            }
        }
        public DetectionState DetectState
        {
            get
            {
                return this.detectState;
            }

            set
            {
                if (this.detectState != value)
                {
                    this.detectState = value;

                    // Notify all the properties derived from the state that the state changed.
                    base.OnPropertyChanged("DetectState");
                }
            }
        }
        public InstallationState InstallState
        {
            get
            {
                return this.installState;
            }

            set
            {
                if (this.installState != value)
                {
                    this.installState = value;

                    // Notify all the properties derived from the state that the state changed.
                    base.OnPropertyChanged("InstallState");

                    switch (value)
                    {
                        case InstallationState.Applied:
                            TaskBarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                            break;

                        case InstallationState.Applying:
                            TaskBarProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                            break;

                        case InstallationState.Failed:
                            TaskBarProgressValue = 1;
                            TaskBarProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                            break;

                        default: // Let others handle that
                            break;
                    }
                }
            }
        }

        private ApplyRestart rebootState_ = ApplyRestart.None;
        public ApplyRestart RebootState
        {
            get
            {
                return rebootState_;
            }
            set
            {
                if (rebootState_ != value)
                {
                    rebootState_ = value;
                    OnPropertyChanged("RebootState");
                }
            }
        }
        public InstallationState PreApplyState { get; set; }

        #region View-Models

        private VariablesViewModel variablesViewModel_;
        public VariablesViewModel VariablesViewModel => variablesViewModel_ ?? (variablesViewModel_ = new VariablesViewModel(MainModel.Engine));

        private PopupViewModel popupViewModel_ = null;
        public PopupViewModel PopupViewModel => popupViewModel_ ?? (popupViewModel_ = new PopupViewModel());

        private SummaryViewModel summaryViewModel_ = null;
        public SummaryViewModel SummaryViewModel => summaryViewModel_ ?? (summaryViewModel_ = new SummaryViewModel(this));

        private ProgressViewModel progressViewModel_ = null;
        public ProgressViewModel ProgressViewModel => progressViewModel_ ?? (progressViewModel_ = new ProgressViewModel(this));

        private FinishViewModel finishViewModel_ = null;
        public FinishViewModel FinishViewModel => finishViewModel_ ?? (finishViewModel_ = new FinishViewModel(this));

        private RepairViewModel repairViewModel_ = null;
        public RepairViewModel RepairViewModel => repairViewModel_ ?? (repairViewModel_ = new RepairViewModel(this));

        #endregion
    }
}
