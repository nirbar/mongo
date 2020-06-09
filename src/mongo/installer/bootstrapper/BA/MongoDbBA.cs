using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MongoDB.Bootstrapper.BA.Model;
using MongoDB.Bootstrapper.BA.Util;
using MongoDB.Bootstrapper.BA.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Threading;

namespace MongoDB.Bootstrapper.BA
{
    public enum DetectionState
    {
        Absent,
        Present,
        Newer,
        Older
    }
    public enum InstallationState
    {
        Initializing,
        Detecting,
        Detected,
        Planning,
        Applying,
        Applied,
        Failed,
    }
    public class MongoDbBA : BootstrapperApplication
    {
        public MainModel MainModel { get; private set; }
        static public View.RootView View { get; private set; }
        // TODO: We should refactor things so we dont have a global View.

        private RootViewModel rootViewModel_ = null;
        protected override void Run()
        {
            try
            {
#if DEBUG
                Debugger.Launch();
#endif
                this.Engine.Log(LogLevel.Verbose, "Running MongoDB BA.");
                MainModel = new Model.MainModel(this);
                rootViewModel_ = new RootViewModel(MainModel);

                // Kick off detect which will populate the view models.
                this.Engine.Detect();
                Engine.CloseSplashScreen();

                // Create a Window to show UI.
                if (MainModel.Command.Display == Display.Passive ||
                    MainModel.Command.Display == Display.Full)
                {
                    this.Engine.Log(LogLevel.Verbose, "Creating a UI.");
                    View = new View.RootView(rootViewModel_);
                    View.Show();
                }

                Dispatcher.Run();
            }
            catch (Exception ex)
            {
                Engine.Log(LogLevel.Error, string.Format("Fatal error {0}: {1} at \n{2}", ex.GetType().ToString(), ex.Message, ex.StackTrace));
            }
            finally
            {
                MainModel.UpdateModel.Cancel();
                rootViewModel_.Dispatcher.InvokeShutdown();
                this.Engine.Quit(MainModel.Result);
            }
        }

        protected override void OnDetectRelatedMsiPackage(DetectRelatedMsiPackageEventArgs args)
        {
            base.OnDetectRelatedMsiPackage(args);
            if ("MongoDbServer".Equals(args.PackageId))
            {
                IEnumerable<ProductInstallation> products = ProductInstallation.GetProducts(args.ProductCode, null, UserContexts.Machine);
                if (products != null)
                {
                    return;
                }
                foreach (ProductInstallation product in products)
                {
                    // Find same major.minor
                    if ((product.ProductVersion.Major != rootViewModel_.VariablesViewModel.WixBundleVersion.Major) || (product.ProductVersion.Minor != rootViewModel_.VariablesViewModel.WixBundleVersion.Minor))
                    {
                        continue;
                    }
                    if (string.IsNullOrEmpty(product.InstallLocation))
                    {
                        continue;
                    }

                    rootViewModel_.VariablesViewModel.INSTALL_FOLDER = product.InstallLocation;

                    string mongodCfg = Path.Combine(rootViewModel_.VariablesViewModel.INSTALL_FOLDER, "BIN", "mongod.cfg");
                    if (!File.Exists(mongodCfg))
                    {
                        continue;
                    }

                    string[] mongodLines = File.ReadAllLines(mongodCfg);
                    if (mongodLines == null)
                    {
                        continue;
                    }

                    foreach (string line in mongodLines)
                    {
                        int i = line.IndexOf("dbPath:");
                        if (i >= 0)
                        {
                            string dbPath = line.Substring(i + "dbPath:".Length);
                            rootViewModel_.VariablesViewModel.MONGO_DATA_PATH = dbPath.Trim();
                            continue;
                        }

                        i = line.IndexOf("path:");
                        if (i >= 0)
                        {
                            string logPath = line.Substring(i + "path:".Length);
                            rootViewModel_.VariablesViewModel.MONGO_LOG_PATH = logPath.Trim();
                            continue;
                        }
                    }
                }
            }
        }

        private bool serverMsiInstalled = false;
        protected override void OnDetectPackageComplete(DetectPackageCompleteEventArgs args)
        {
            base.OnDetectPackageComplete(args);
            if ("MongoDbServer".Equals(args.PackageId))
            {
                serverMsiInstalled = ((args.State == PackageState.Present) || (args.State == PackageState.Obsolete) || (args.State == PackageState.Superseded));
            }
        }

        protected override void OnDetectMsiFeature(DetectMsiFeatureEventArgs args)
        {
            if ("MongoDbServer".Equals(args.PackageId) && serverMsiInstalled)
            {
                switch (args.FeatureId)
                {
                    case "ServerService":
                        rootViewModel_.VariablesViewModel.MONGO_SERVICE_INSTALL = (args.State == FeatureState.Local);
                        break;
                    case "ServerNoService":
                        rootViewModel_.VariablesViewModel.MONGO_SERVICE_INSTALL = (args.State == FeatureState.Absent);
                        break;
                    case "Server":
                        rootViewModel_.VariablesViewModel.INSTALL_SERVER_SERVER = (args.State == FeatureState.Local);
                        break;
                    case "Client":
                        rootViewModel_.VariablesViewModel.INSTALL_SERVER_CLIENT = (args.State == FeatureState.Local);
                        break;
                    case "MonitoringTools":
                        rootViewModel_.VariablesViewModel.INSTALL_SERVER_MONITORINGTOOLS = (args.State == FeatureState.Local);
                        break;
                    case "ImportExportTools":
                        rootViewModel_.VariablesViewModel.INSTALL_SERVER_IMPORTEXPORTTOOLS = (args.State == FeatureState.Local);
                        break;
                    case "Router":
                        rootViewModel_.VariablesViewModel.INSTALL_SERVER_ROUTER = (args.State == FeatureState.Local);
                        break;
                    case "MiscellaneousTools":
                        rootViewModel_.VariablesViewModel.INSTALL_SERVER_MISCELLANEOUSTOOLS = (args.State == FeatureState.Local);
                        break;
                    case "InstallCompassFeature":
                    case "ProductFeature":
                        break;
                    default:
                        MainModel.Engine.Log(LogLevel.Error, $"Unknown server feature '{args.FeatureId}'");
                        break;
                }
            }

            base.OnDetectMsiFeature(args);
        }

        protected override void OnPlanMsiFeature(PlanMsiFeatureEventArgs args)
        {
            if (args.PackageId.Equals("MongoDbServer"))
            {
                switch (args.FeatureId)
                {
                    case "ServerService":
                        args.State = rootViewModel_.VariablesViewModel.MONGO_SERVICE_INSTALL ? FeatureState.Local : FeatureState.Absent;
                        break;
                    case "ServerNoService":
                        args.State = rootViewModel_.VariablesViewModel.MONGO_SERVICE_INSTALL ? FeatureState.Absent : FeatureState.Local;
                        break;
                    case "Server":
                        args.State = rootViewModel_.VariablesViewModel.INSTALL_SERVER_SERVER ? FeatureState.Local : FeatureState.Absent;
                        break;
                    case "Client":
                        args.State = rootViewModel_.VariablesViewModel.INSTALL_SERVER_CLIENT ? FeatureState.Local : FeatureState.Absent;
                        break;
                    case "MonitoringTools":
                        args.State = rootViewModel_.VariablesViewModel.INSTALL_SERVER_MONITORINGTOOLS ? FeatureState.Local : FeatureState.Absent;
                        break;
                    case "ImportExportTools":
                        args.State = rootViewModel_.VariablesViewModel.INSTALL_SERVER_IMPORTEXPORTTOOLS ? FeatureState.Local : FeatureState.Absent;
                        break;
                    case "Router":
                        args.State = rootViewModel_.VariablesViewModel.INSTALL_SERVER_ROUTER ? FeatureState.Local : FeatureState.Absent;
                        break;
                    case "MiscellaneousTools":
                        args.State = rootViewModel_.VariablesViewModel.INSTALL_SERVER_MISCELLANEOUSTOOLS ? FeatureState.Local : FeatureState.Absent;
                        break;
                    case "InstallCompassFeature":
                        args.State = FeatureState.Absent;
                        break;
                    case "ProductFeature":
                        args.State = FeatureState.Local;
                        break;
                    default:
                        MainModel.Engine.Log(LogLevel.Error, $"Unknown server feature '{args.FeatureId}'");
                        break;
                }
            }

            base.OnPlanMsiFeature(args);
        }

        #region Hooks

        protected override void OnDetectBegin(DetectBeginEventArgs args)
        {
            base.OnDetectBegin(args);

            // Help only?
            if (LaunchAction.Help == MainModel.Command.Action)
            {
                Engine.Log(LogLevel.Verbose, "Showing help screen");
                return;
            }

            if (args.Installed)
            {
                rootViewModel_.DetectState = DetectionState.Present;
            }
            else
            {
                rootViewModel_.DetectState = DetectionState.Absent;
                MainModel.UpdateModel.BeginCheck();
            }

            MainModel.PlannedAction = LaunchAction.Unknown;
        }

        protected override void OnDetectRelatedBundle(DetectRelatedBundleEventArgs args)
        {
            base.OnDetectRelatedBundle(args);

            if (args.Operation == RelatedOperation.Downgrade)
            {
                rootViewModel_.DetectState = DetectionState.Newer;
            }

            // If same version or higher exists, treat it as an update.
            else if ((args.Operation == RelatedOperation.MajorUpgrade) || ((args.Operation == RelatedOperation.None) && (args.RelationType == RelationType.Upgrade)))
            {
                rootViewModel_.DetectState = DetectionState.Older;
            }
        }

        protected override void OnPlanRelatedBundle(PlanRelatedBundleEventArgs args)
        {
            args.State = RequestState.None; // We don't change other versions, allowing multi instance. 
            //TODO Replace versions with same Major.Minor

            base.OnPlanRelatedBundle(args);
        }

        protected override void OnDetectComplete(DetectCompleteEventArgs e)
        {
            base.OnDetectComplete(e);
            // Help only?
            if (LaunchAction.Help == MainModel.Command.Action)
            {
                return;
            }

            // Parse the command line string before any planning.
            try
            {
                Engine.ParseCommandLine(this);
            }
            catch (Exception ex)
            {
                Engine.Log(LogLevel.Error, "Failed parsing command line: " + ex.Message);
            }

            // Evaluate conditions
            try
            {
                Engine.EvaluateConditions();
            }
            catch (Exception ex)
            {
                Engine.Log(LogLevel.Error, ex.Message);
                if (MainModel.Command.Display == Display.Full)
                {
                    rootViewModel_.PopupViewModel.ShowSync((int)MsiButtons.MB_OK, "Error", ex.Message);
                }

                if ((MainModel.Command.Display == Display.Full) || (MainModel.Command.Display == Display.Passive))
                {
                    rootViewModel_.Dispatcher.Invoke(
                        (Action)delegate ()
                        {
                            MongoDbBA.View.Close();
                        });
                }
                rootViewModel_.Dispatcher.InvokeShutdown();
                this.Engine.Quit(-1);
                return;
            }

            rootViewModel_.InstallState = InstallationState.Detected;

            if (LaunchAction.Uninstall == MainModel.Command.Action)
            {
                Engine.Log(LogLevel.Verbose, "Invoking automatic plan for uninstall");
                MainModel.PlannedAction = LaunchAction.Uninstall;
                Engine.Plan(MainModel.PlannedAction);
            }
            else if (e.Status >= 0)
            {
                if (MainModel.Command.Display != Display.Full)
                {
                    // If we're not waiting for the user to click install, dispatch plan with the default action.
                    Engine.Log(LogLevel.Verbose, "Invoking automatic plan for non-interactive mode.");
                    MainModel.PlannedAction = MainModel.Command.Action;

                    if (MainModel.Command.Action == LaunchAction.Install)
                    {
                        try
                        {
                            rootViewModel_.VariablesViewModel.ValidateFeatureSelection();
                            rootViewModel_.VariablesViewModel.ValidateTargetFolder();
                            rootViewModel_.VariablesViewModel.ValidateServiceAccount();
                        }
                        catch (Exception ex)
                        {
                            MainModel.Result = -1;
                            Engine.Log(LogLevel.Error, $"Failde validating parameters: {ex.Message}");
                            rootViewModel_.Dispatcher.Invoke((Action)delegate () { View?.Close(); });
                            rootViewModel_.Dispatcher.InvokeShutdown();
                            return;
                        }
                    }
                    Engine.Plan(MainModel.PlannedAction);
                }
            }
            else
            {
                rootViewModel_.InstallState = InstallationState.Failed;
            }
        }

        protected override void OnPlanPackageBegin(PlanPackageBeginEventArgs e)
        {
            base.OnPlanPackageBegin(e);
            if (Engine.StringVariables.Contains("MbaNetfxPackageId") && e.PackageId.Equals(Engine.StringVariables["MbaNetfxPackageId"], StringComparison.Ordinal))
            {
                e.State = RequestState.None;
            }
        }

        protected override void OnPlanComplete(PlanCompleteEventArgs e)
        {
            base.OnPlanComplete(e);
            if (e.Status >= 0)
            {
                rootViewModel_.PreApplyState = rootViewModel_.InstallState;
                rootViewModel_.InstallState = InstallationState.Applying;
                Engine.Apply(rootViewModel_.ViewWindowHandle);
            }
            else
            {
                rootViewModel_.InstallState = InstallationState.Failed;
            }
        }

        protected override void OnApplyComplete(ApplyCompleteEventArgs e)
        {
            base.OnApplyComplete(e);
            if (e.Status >= 0)
            {
                rootViewModel_.InstallState = InstallationState.Applied;
            }
            else
            {
                rootViewModel_.InstallState = InstallationState.Failed;
            }

            MainModel.Result = e.Status; // remember the final result of the apply.
            rootViewModel_.RebootState = e.Restart;

            // If we're not in Full UI mode, we need to alert the dispatcher to stop and close the window for passive.
            if (Display.Full != MainModel.Command.Display)
            {
                // If its passive, send a message to the window to close.
                if (Display.Passive == MainModel.Command.Display)
                {
                    Engine.Log(LogLevel.Verbose, "Automatically closing the window for non-interactive install");
                    rootViewModel_.Dispatcher.BeginInvoke((Action)delegate ()
                    {
                        View.Close();
                    }
                    );
                }
                else
                {
                    rootViewModel_.Dispatcher.InvokeShutdown();
                }
            }
            else if ((e.Status >= 0) && LaunchAction.UpdateReplace == MainModel.PlannedAction) // if we successfully applied an update close the window since the new Bundle should be running now.
            {
                Engine.Log(LogLevel.Verbose, "Automatically closing the window since update successful.");
                rootViewModel_.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    View.Close();
                }
                );
            }

            // Set the state to applied or failed unless the state has already been set back to the preapply state
            // which means we need to show the UI as it was before the apply started.
            if (rootViewModel_.InstallState != rootViewModel_.PreApplyState)
            {
                rootViewModel_.InstallState = (e.Status >= 0) ? InstallationState.Applied : InstallationState.Failed;
            }
        }
        protected override void OnShutdown(ShutdownEventArgs e)
        {
            base.OnShutdown(e);
            if (MainModel.RebootRequested)
            {
                e.Result = Result.Restart;
            }
        }

        #endregion
    }
}