using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MongoDB.Bootstrapper.BA.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace MongoDB.Bootstrapper.BA.View
{
    public partial class RootView : Window, INotifyPropertyChanged
    {
        public RootViewModel RootViewModel { get; private set; }
        public PopupViewModel PopupViewModel => RootViewModel.PopupViewModel;
        public VariablesViewModel VariablesViewModel => RootViewModel.VariablesViewModel;
        public RepairViewModel RepairViewModel => RootViewModel.RepairViewModel;
        public SummaryViewModel SummaryViewModel => RootViewModel.SummaryViewModel;
        public FinishViewModel FinishViewModel => RootViewModel.FinishViewModel;
        public ProgressViewModel ProgressViewModel => RootViewModel.ProgressViewModel;

        public RootView(RootViewModel root)
        {
            RootViewModel = root;
            RootViewModel.PropertyChanged += RootViewModel__PropertyChanged;
            
            DataContext = this;
            InitializeComponent();
            RootViewModel.ViewWindowHandle = new WindowInteropHelper(this).EnsureHandle();
            SetStatePage();
        }

        protected override void OnClosed(EventArgs e)
        {
            Dispatcher.InvokeShutdown();
            base.OnClosed(e);
        }

        private void RootViewModel__PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("InstallState"))
            {
                Dispatcher.Invoke((Action)delegate () { SetStatePage(); });
            }
        }

        private void Background_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void application_Stop(object sender, ExecutedRoutedEventArgs e)
        {
            switch (RootViewModel.InstallState)
            {
                case InstallationState.Applying: // During apply phase, we need to wait for current package to complete or send a MSI message so we can cancel
                    RootViewModel.PopupViewModel.Show("Cancel?", "Are you sure you want to cancel?"
                        , "Yes", () => RootViewModel.Canceled = true
                        , "No"
                        );
                    break;

                case InstallationState.Applied:
                case InstallationState.Failed:
                    Close();
                    break;

                default:
                    RootViewModel.PopupViewModel.Show("Cancel?", "Are you sure you want to cancel?"
                        , "Yes", () => Close()
                        , "No"
                        );
                    break;
            }
        }

        #region Wizard navigations

        private enum Pages
        {
            Unknown,
            Detecting,
            /*TODO
            UpdateCheck,
            UpdateAvailable,
            */
            Eula,
            FeatureSelection,
            InstallFolder,
            Service,
            Summary,
            Repair,
            Progress,
            Finish,
        }
        private Stack<Pages> pages_ = new Stack<Pages>();

        public event PropertyChangedEventHandler PropertyChanged;
        private FrameworkElement currentView_ = null;
        public FrameworkElement CurrentView
        {
            get => currentView_;
            set
            {
                currentView_ = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentView"));
            }
        }

        private void SetStatePage()
        {
            switch (RootViewModel.InstallState)
            {
                case InstallationState.Detecting:
                    pages_.Clear();
                    pages_.Push(Pages.Detecting);
                    break;

                case InstallationState.Detected: // Detect complete
                    if (RootViewModel.DetectState == DetectionState.Present)
                    {
                        pages_.Clear();
                        pages_.Push(Pages.Repair);
                    }
                    else
                    {
                        pages_.Clear();
                        pages_.Push(Pages.Eula);
                    }
                    break;


                case InstallationState.Applying:
                    pages_.Clear();
                    pages_.Push(Pages.Progress);
                    break;

                case InstallationState.Applied:
                case InstallationState.Failed:
                    pages_.Clear();
                    pages_.Push(Pages.Finish);
                    break;
            }
            SetCurrentView();
        }

        private void SetCurrentView()
        {
            if (pages_.Count < 1)
            {
                return;
            }

            Pages page = pages_.First();
            switch (page)
            {
                default:
                    RootViewModel.MainModel.Engine.Log(LogLevel.Error, $"Can't navigate to {page}");
                    break;
                case Pages.Detecting:
                    Dispatcher.Invoke((Action)delegate () { CurrentView = new DetectingView(); });
                    break;
                case Pages.Eula:
                    Dispatcher.Invoke((Action)delegate () { CurrentView = new EulaView(); });
                    break;
                case Pages.FeatureSelection:
                    Dispatcher.Invoke((Action)delegate () { CurrentView = new FeatureSelectionView(); });
                    break;
                case Pages.Finish:
                    Dispatcher.Invoke((Action)delegate () { CurrentView = new FinishView(); });
                    break;
                case Pages.InstallFolder:
                    Dispatcher.Invoke((Action)delegate () { CurrentView = new InstallLocationView(RootViewModel.VariablesViewModel); });
                    break;
                case Pages.Progress:
                    Dispatcher.Invoke((Action)delegate () { CurrentView = new ProgressView(); });
                    break;
                case Pages.Repair:
                    Dispatcher.Invoke((Action)delegate () { CurrentView = new RepairView(); });
                    break;
                case Pages.Service:
                    Dispatcher.Invoke((Action)delegate () { CurrentView = new ServiceAccountView(RootViewModel.VariablesViewModel); });
                    break;
                case Pages.Summary:
                    Dispatcher.Invoke((Action)delegate () { CurrentView = new SummaryView(); });
                    break;
            }
        }

        private void navigation_Next(object sender, ExecutedRoutedEventArgs e)
        {
            if (pages_.Count == 0)
            {
                return;
            }

            Pages page = pages_.First();
            Pages nextPage = Pages.Unknown;
            switch (page)
            {
                default:
                    RootViewModel.MainModel.Engine.Log(LogLevel.Error, $"Can't navigate next from {page}");
                    break;
                case Pages.Eula:
                    nextPage = Pages.FeatureSelection;
                    break;
                case Pages.FeatureSelection:
                    nextPage = Pages.InstallFolder;
                    break;
                case Pages.InstallFolder:
                    if (VariablesViewModel.INSTALL_SERVER && VariablesViewModel.INSTALL_SERVER_SERVER)
                    {
                        nextPage = Pages.Service;
                    }
                    else
                    {
                        nextPage = Pages.Summary;
                    }
                    break;
                case Pages.Service:
                    nextPage = Pages.Summary;
                    break;
            }
            ValidatePage(nextPage);
        }

        private Task validationTask_ = null;
        public bool IsValidating => (((validationTask_?.Status ?? TaskStatus.Created) < TaskStatus.RanToCompletion) && ((validationTask_?.Status ?? TaskStatus.Created) > TaskStatus.Created));
        private void ValidatePage(Pages nextPage)
        {
            if (pages_.Count < 1)
            {
                return;
            }

            Pages page = pages_.First();
            switch (page)
            {
                case Pages.FeatureSelection:
                    validationTask_ = new Task(() =>
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsValidating"));
                        VariablesViewModel.ValidateFeatureSelection();
                    });
                    break;
                case Pages.InstallFolder:
                    validationTask_ = new Task(() =>
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsValidating"));
                        VariablesViewModel.ValidateTargetFolder();
                    });
                    break;
                case Pages.Service:
                    validationTask_ = new Task(() =>
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsValidating"));
                        VariablesViewModel.ValidateServiceAccount();
                    });
                    break;
            }

            // Page with no validation
            if (validationTask_ == null)
            {
                pages_.Push(nextPage);
                SetCurrentView();
                return;
            }

            validationTask_.ContinueWith(t =>
            {
                pages_.Push(nextPage);
                SetCurrentView();
            }, TaskContinuationOptions.OnlyOnRanToCompletion);

            validationTask_.ContinueWith(t =>
            {
                PopupViewModel.Show("Error", (t.Exception.InnerException ?? t.Exception).Message, "OK");
            }, TaskContinuationOptions.OnlyOnFaulted);

            validationTask_.ContinueWith(t =>
            {
                validationTask_ = null;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsValidating"));
            });

            validationTask_.Start();
        }

        private void navigation_Back(object sender, ExecutedRoutedEventArgs e)
        {
            if (pages_.Count <= 1)
            {
                return;
            }

            pages_.Pop();
            SetCurrentView();
        }

        #endregion
    }
}
