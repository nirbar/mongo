using MongoDB.Bootstrapper.BA.ViewModel;
using System.Windows;
using System.Windows.Controls;
using WinForms = System.Windows.Forms;

namespace MongoDB.Bootstrapper.BA.View
{
    public partial class InstallLocationView : UserControl
    {
        public InstallLocationView(VariablesViewModel variables)
        {
            variables_ = variables;
            InitializeComponent();
        }

        private VariablesViewModel variables_;

        private void browse_Click(object sender, RoutedEventArgs e)
        {
            WinForms.FolderBrowserDialog fbd = new WinForms.FolderBrowserDialog();
            fbd.ShowNewFolderButton = false;
            fbd.SelectedPath = variables_.INSTALL_FOLDER ?? "";
            if (fbd.ShowDialog() == WinForms.DialogResult.OK)
            {
                variables_.INSTALL_FOLDER = fbd.SelectedPath;
            }
        }

        private void browseLogs_Click(object sender, RoutedEventArgs e)
        {
            WinForms.FolderBrowserDialog fbd = new WinForms.FolderBrowserDialog();
            fbd.ShowNewFolderButton = false;
            fbd.SelectedPath = variables_.MONGO_LOG_PATH ?? "";
            if (fbd.ShowDialog() == WinForms.DialogResult.OK)
            {
                variables_.MONGO_LOG_PATH = fbd.SelectedPath;
            }
        }

        private void browseData_Click(object sender, RoutedEventArgs e)
        {
            WinForms.FolderBrowserDialog fbd = new WinForms.FolderBrowserDialog();
            fbd.ShowNewFolderButton = false;
            fbd.SelectedPath = variables_.MONGO_DATA_PATH ?? "";
            if (fbd.ShowDialog() == WinForms.DialogResult.OK)
            {
                variables_.MONGO_DATA_PATH = fbd.SelectedPath;
            }
        }
    }
}
