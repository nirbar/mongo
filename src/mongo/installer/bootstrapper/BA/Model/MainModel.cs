using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MongoDB.Bootstrapper.BA.Util;

namespace MongoDB.Bootstrapper.BA.Model
{
    public class MainModel
    {
        public MainModel(BootstrapperApplication bootstrapper)
        {
            this.Bootstrapper = bootstrapper;
        }
        public BootstrapperApplication Bootstrapper { get; private set; }
        public Command Command { get { return this.Bootstrapper.Command; } }
        public Engine Engine { get { return this.Bootstrapper.Engine; } }
        public int Result { get; set; }

        public LaunchAction PlannedAction { get; set; }
        public bool RebootRequested { get; set; }

        private MongoDbUpdate updateModel_ = null;
        public MongoDbUpdate UpdateModel
        {
            get
            {
                if (updateModel_ == null)
                {
                    if (Engine.StringVariables.Contains("MONGODB_UPDATE_FEED"))
                    {
                        string updateFeed = Engine.StringVariables["MONGODB_UPDATE_FEED"];
                        updateModel_ = new MongoDbUpdate(Engine, updateFeed);
                    }
                }
                return updateModel_;
            }
        }
    }
}