using System;
using System.Windows.Input;

namespace BluetoothLEPair.Input
{
    public class SimpleCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private bool _isEnabled;
        private readonly Action _simpleAction;

        public SimpleCommand(Action a, bool isEnabled = true)
        {
            _simpleAction = a;
            _isEnabled = isEnabled;
        }

        public bool CanExecute(object parameter)
        {
            return this.IsEnabled;
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                _simpleAction();
            }
        }

        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                bool updated = _isEnabled != value;
                _isEnabled = value;

                if (updated)
                {
                    var x = CanExecuteChanged;

                    if (x != null)
                    {
                        x(this, new EventArgs());
                    }
                }
            }
        }
    }
}