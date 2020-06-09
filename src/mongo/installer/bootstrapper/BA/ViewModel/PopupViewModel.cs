using System.Windows.Input;
using System.Windows;
using System;
using MongoDB.Bootstrapper.BA.Util;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MongoDB.Bootstrapper.BA.ViewModel
{
    public class PopupViewModel : ViewModelBase
    {
        private bool isOpen_ = false;
        private string caption_ = string.Empty;
        private string text_ = string.Empty;
        private ManualResetEvent resetEvent_ = new ManualResetEvent(false);

        public Result ShowSync(int hint, string caption, string text)
        {
            Result right = Result.None;
            Result middle = Result.None;
            Result left = Result.None;
            string rightText = null;
            string midText = null;
            string leftText = null;
            MsiButtons msiButtons = (MsiButtons)hint & MsiButtons.MB_BUTTONSMASK;
            switch (msiButtons)
            {
                case MsiButtons.MB_ABORTRETRYIGNORE:
                    left = Result.Abort;
                    leftText = left.ToString();

                    middle = Result.Retry;
                    midText = middle.ToString();

                    right = Result.Ignore;
                    rightText = right.ToString();
                    break;

                case MsiButtons.MB_CANCELTRYCONTINUE:
                    left = Result.Cancel;
                    leftText = left.ToString();

                    middle = Result.Retry;
                    midText = middle.ToString();

                    right = Result.Continue;
                    rightText = right.ToString();
                    break;

                case MsiButtons.MB_OK:
                    right = Result.Ok;
                    rightText = right.ToString();
                    break;

                case MsiButtons.MB_OKCANCEL:
                    middle = Result.Ok;
                    midText = middle.ToString();

                    right = Result.Cancel;
                    rightText = right.ToString();
                    break;

                case MsiButtons.MB_RETRYCANCEL:
                    middle = Result.Retry;
                    midText = middle.ToString();

                    right = Result.Cancel;
                    rightText = right.ToString();
                    break;

                case MsiButtons.MB_YESNO:
                    middle = Result.Yes;
                    midText = middle.ToString();

                    right = Result.No;
                    rightText = right.ToString();
                    break;

                case MsiButtons.MB_YESNOCANCEL:
                    left = Result.Yes;
                    leftText = left.ToString();

                    middle = Result.No;
                    midText = middle.ToString();

                    right = Result.Cancel;
                    rightText = right.ToString();
                    break;

                default:
                    return Result.None;
            }

            Result res = Result.None;
            Show(
                caption
                , text
                , rightText, () => res = right
                , midText, () => res = middle
                , leftText, () => res = left
                )
                .Wait();

            return res;
        }

        public Task Show(string caption, string text, string rightCommandText, Action rightCommand = null, string middleCommandText = null, Action middleCommand = null, string leftCommandText = null, Action leftCommand = null)
        {
            resetEvent_.Reset();

            Caption = caption;
            Text = text;

            rightAction_ = rightCommand;
            RightCommandText = rightCommandText;

            middleAction_ = middleCommand;
            MiddleCommandText = middleCommandText;

            leftAction_ = leftCommand;
            LeftCommandText = leftCommandText;
            
            IsOpen = true;
            return Task.Factory.StartNew(() => resetEvent_.WaitOne());
        }

        public bool IsOpen
        {
            get
            {
                return isOpen_;
            }
            private set
            {
                isOpen_ = value;
                OnPropertyChanged("IsOpen");
            }
        }

        public string Caption
        {
            get
            {
                return caption_;
            }
            set
            {
                caption_ = value;
                OnPropertyChanged("Caption");
            }
        }

        public string Text
        {
            get
            {
                return text_;
            }
            set
            {
                text_ = value;
                OnPropertyChanged("Text");
            }
        }

        private void Click(Action action)
        {
            IsOpen = false;

            rightAction_ = null;
            RightCommandText = null;

            middleAction_ = null;
            MiddleCommandText = null;

            leftAction_ = null;
            LeftCommandText = null;

            Caption = null;
            Text = null;

            action?.Invoke();
            resetEvent_.Set();
        }

        #region Right-most command

        private Action rightAction_ = null;
        public ICommand RightCommand => new RelayCommand(a => Click(rightAction_));

        private string rightCommandText_ = string.Empty;
        public string RightCommandText
        {
            get
            {
                return rightCommandText_;
            }
            set
            {
                rightCommandText_ = value;
                OnPropertyChanged("RightCommandText");
                OnPropertyChanged("RightCommandVisibility");
            }
        }

        public Visibility RightCommandVisibility => string.IsNullOrEmpty(RightCommandText) ? Visibility.Hidden : Visibility.Visible;

        #endregion

        #region Middle command

        private Action middleAction_ = null;
        public ICommand MiddleCommand => new RelayCommand(a => Click(middleAction_));

        private string middleCommandText_ = string.Empty;
        public string MiddleCommandText
        {
            get
            {
                return middleCommandText_;
            }
            set
            {
                middleCommandText_ = value;
                OnPropertyChanged("MiddleCommandText");
                OnPropertyChanged("MiddleCommandVisibility");
            }
        }

        public Visibility MiddleCommandVisibility => string.IsNullOrEmpty(MiddleCommandText) ? Visibility.Hidden : Visibility.Visible;

        #endregion

        #region Left-most command

        private Action leftAction_ = null;
        public ICommand LeftCommand => new RelayCommand(a => Click(leftAction_));

        private string leftCommandText_ = string.Empty;
        public string LeftCommandText
        {
            get
            {
                return leftCommandText_;
            }
            set
            {
                leftCommandText_ = value;
                OnPropertyChanged("LeftCommandText");
                OnPropertyChanged("LeftCommandVisibility");
            }
        }

        public Visibility LeftCommandVisibility => string.IsNullOrEmpty(LeftCommandText) ? Visibility.Hidden : Visibility.Visible;

        #endregion
    }
}
