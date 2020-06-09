using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MongoDB.Bootstrapper.BA.ViewModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace MongoDB.Bootstrapper.BA
{
    public class ProgressViewModel : ViewModelBase
    {
        private RootViewModel root;
        private Dictionary<string, int> executingPackageOrderIndex;

        private int progressPhases;
        private int progress;
        private int cacheProgress;
        private int executeProgress;
        private string message;

        public ProgressViewModel(RootViewModel root)
        {
            this.root = root;
            this.executingPackageOrderIndex = new Dictionary<string, int>();

            this.root.PropertyChanged += this.RootPropertyChanged;

            root.MainModel.Bootstrapper.ExecuteMsiMessage += this.ExecuteMsiMessage;
            root.MainModel.Bootstrapper.ExecuteProgress += this.ApplyExecuteProgress;
            root.MainModel.Bootstrapper.PlanBegin += this.PlanBegin;
            root.MainModel.Bootstrapper.PlanPackageComplete += this.PlanPackageComplete;
            root.MainModel.Bootstrapper.ApplyPhaseCount += this.ApplyPhaseCount;
            root.MainModel.Bootstrapper.Progress += this.ApplyProgress;
            root.MainModel.Bootstrapper.CacheAcquireProgress += this.CacheAcquireProgress;
            root.MainModel.Bootstrapper.CacheComplete += this.CacheComplete;
            root.MainModel.Bootstrapper.ExecutePackageBegin += Bootstrapper_ExecutePackageBegin;
        }

        public bool ProgressEnabled
        {
            get { return this.root.InstallState == InstallationState.Applying; }
        }

        public int Progress
        {
            get
            {
                return this.progress;
            }

            set
            {
                if (this.progress != value)
                {
                    this.progress = value;
                    root.TaskBarProgressValue = value / 100.0;
                    base.OnPropertyChanged("Progress");
                }
            }
        }

        public string Message
        {
            get
            {
                return this.message;
            }

            set
            {
                if (this.message != value)
                {
                    this.message = value;
                    base.OnPropertyChanged("Message");
                }
            }
        }

        private string package_ = "";
        public string Package
        {
            get => package_;
            set
            {
                package_ = value;
                base.OnPropertyChanged("Package");
            }
        }

        private void Bootstrapper_ExecutePackageBegin(object sender, ExecutePackageBeginEventArgs e)
        {
            Package = e.PackageId;
        }

        void RootPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ("InstallState" == e.PropertyName)
            {
                base.OnPropertyChanged("ProgressEnabled");
            }
        }

        private void PlanBegin(object sender, PlanBeginEventArgs e)
        {
            lock (this)
            {
                this.executingPackageOrderIndex.Clear();
            }
        }

        private void PlanPackageComplete(object sender, PlanPackageCompleteEventArgs e)
        {
            if (ActionState.None != e.Execute)
            {
                lock (this)
                {
                    Debug.Assert(!this.executingPackageOrderIndex.ContainsKey(e.PackageId));
                    this.executingPackageOrderIndex.Add(e.PackageId, this.executingPackageOrderIndex.Count);
                }
            }
        }

        private void ExecuteMsiMessage(object sender, ExecuteMsiMessageEventArgs e)
        {
            lock (this)
            {
                switch (e.MessageType)
                {
                    case InstallMessage.ActionStart:
                        this.Message = e.Message;
                        break;

                    case InstallMessage.Warning:
                    case InstallMessage.User:
                    case InstallMessage.Info:
                        if (root.MainModel.Command.Display == Display.Embedded)
                        {
                            e.Result = (Result)root.MainModel.Engine.SendEmbeddedError((int)e.MessageType, e.Message, e.DisplayParameters);
                            break;
                        }
                        e.Result = root.PopupViewModel.ShowSync(e.DisplayParameters, e.MessageType.ToString(), e.Message);
                        break;

                    default:
                        break;
                }

                if (root.Canceled)
                {
                    e.Result = Result.Cancel;
                }
            }
        }

        private void ApplyPhaseCount(object sender, ApplyPhaseCountArgs e)
        {
            this.progressPhases = e.PhaseCount;
        }

        private void ApplyProgress(object sender, ProgressEventArgs e)
        {
            lock (this)
            {
                e.Result = this.root.Canceled ? Result.Cancel : Result.Ok;
            }
        }

        private void CacheAcquireProgress(object sender, CacheAcquireProgressEventArgs e)
        {
            lock (this)
            {
                if (progressPhases > 0)
                {
                    this.cacheProgress = e.OverallPercentage;
                    this.Progress = (this.cacheProgress + this.executeProgress) / this.progressPhases;
                }
                e.Result = this.root.Canceled ? Result.Cancel : Result.Ok;
            }
        }

        private void CacheComplete(object sender, CacheCompleteEventArgs e)
        {
            lock (this)
            {
                this.cacheProgress = 100;
                if (progressPhases > 0)
                {
                    this.Progress = (this.cacheProgress + this.executeProgress) / this.progressPhases;
                }
            }
        }

        private void ApplyExecuteProgress(object sender, ExecuteProgressEventArgs e)
        {
            lock (this)
            {

                this.executeProgress = e.OverallPercentage;
                if (progressPhases > 0)
                {
                    this.Progress = (this.cacheProgress + this.executeProgress) / this.progressPhases;
                }

                if (root.MainModel.Command.Display == Display.Embedded)
                {
                    root.MainModel.Engine.SendEmbeddedProgress(e.ProgressPercentage, this.Progress);
                }

                e.Result = this.root.Canceled ? Result.Cancel : Result.Ok;
            }
        }
    }
}
