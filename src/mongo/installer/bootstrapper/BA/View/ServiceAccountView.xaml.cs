using MongoDB.Bootstrapper.BA.ViewModel;
using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace MongoDB.Bootstrapper.BA.View
{
    public partial class ServiceAccountView : UserControl
    {
        public ServiceAccountView(VariablesViewModel variables)
        {
            variables_ = variables;
            Loaded += ServiceAccountView_Loaded;
            InitializeComponent();
        }

        private void ServiceAccountView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(variables_.MONGO_SERVICE_ACCOUNT_PASSWORD);
                for (int i = 0; i < variables_.MONGO_SERVICE_ACCOUNT_PASSWORD.Length; ++i)
                {
                    char c = (char)Marshal.ReadInt16(valuePtr, i * 2);
                    passwordBox_.SecurePassword.AppendChar(c);
                }
                passwordBox_.Password = new string('*', variables_.MONGO_SERVICE_ACCOUNT_PASSWORD.Length);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        private VariablesViewModel variables_;

        private void passwordBox__PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            variables_.MONGO_SERVICE_ACCOUNT_PASSWORD = passwordBox_.SecurePassword.Copy();
        }
    }
}
