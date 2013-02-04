using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DynamicAutoCompleteTextBox
{
    public sealed class AutoCompleteTextBox : TextBox, INotifyPropertyChanged
    {
        IEnumerable<string> _dictionary;
        PopupMenu popupMenu;
        private string _menuTrigger;
        /// <summary>
        /// A boolean value indicating if the Popup menu was triggered
        /// </summary>
        private bool _isTriggered;
        /// <summary>
        /// How many characters have been typed after the trigger character
        /// </summary>
        private int _triggerDistance;
        /// <summary>
        /// The caret Location where the last trigger was fired
        /// </summary>
        private int _triggerLocation;

        /// <summary>
        /// The character that is used as
        /// </summary>
        public string MenuTrigger
        {
            get
            {
                return _menuTrigger;
            }
            set
            {
                if (value.Length > 1)
                {
                    throw new ArgumentException("MenuTrigger string must not be more than 1 character");
                }
                SetProperty(ref _menuTrigger, value);
            }
        }

        /// <summary>
        /// DataSource for the Popup
        /// </summary>
        public IEnumerable<string> Dictionary
        {
            get
            {
                if (_dictionary == null)
                {
                    _dictionary = new SortedSet<string>();
                }
                return _dictionary;
            }
            set
            {
                this.SetProperty(ref _dictionary, value);
            }
        }

        public AutoCompleteTextBox()
        {
            popupMenu = new PopupMenu();
            _isTriggered = false;
            _triggerDistance = 1;
            this.TextChanged += AutoCompleteTextBox_TextChanged;
        }

void AutoCompleteTextBox_TextChanged(object sender, TextChangedEventArgs e)
{
    CheckIfTriggered();
    Rect position = this.GetRectFromCharacterIndex(this.SelectionStart - 1, true);
    if (_isTriggered)
    {
        popupMenu.Commands.Clear();
        string searchStr = this.Text.Substring(_triggerLocation, _triggerDistance)
            .TrimStart(MenuTrigger.ToCharArray()[0]);
        IEnumerable<string> items = _dictionary.Where(f => f.StartsWith(searchStr)).Take<string>(6);
        foreach (var item in items)
        {
            popupMenu.Commands.Add(new UICommand(item, (IUICommand command) =>
            {
                var oldSelectionStart = this.SelectionStart;
                string remainder = command.Label.Substring(searchStr.Length);
                var newSelectionStart = oldSelectionStart + remainder.Length;
                this.Text = this.Text.Insert(this.SelectionStart, remainder);
                this.SelectionStart = newSelectionStart;
                _triggerLocation = 0;
                _triggerDistance = 1;
            }));
        }
        var transform = this.TransformToVisual((UIElement)this.Parent);
        var point = transform.TransformPoint(new Point(position.Left, position.Bottom));
        try
        {
            popupMenu.ShowForSelectionAsync(new Rect(point, point), Placement.Below).AsTask();
        }
        catch (InvalidOperationException ex)
        {
        }
    }
}

        private void CheckIfTriggered()
        {
            if (this.SelectionStart > 0)
            {
                if (!_isTriggered)
                {
                    _isTriggered = this.Text.Substring(this.SelectionStart - 
                        _triggerDistance, 1).EndsWith(MenuTrigger.ToString());
                    if (_isTriggered)
                    {
                        _triggerLocation = this.SelectionStart - _triggerDistance;
                    }
                }
                else
                {
                    _isTriggered = !this.Text.Substring(this.SelectionStart - 1, 1).Equals(" ");
                    if (_isTriggered)
                    {
                        _triggerDistance = this.SelectionStart - _triggerLocation;
                    }
                    else
                    {
                        _triggerLocation = 0;
                        _triggerDistance = 1;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="autoCompleteDictionary">Comma Separated list of strings to be used as source</param>
        public AutoCompleteTextBox(string autoCompleteDictionary)
            : this()
        {
            _dictionary = GetSortedDictionaryFromString(autoCompleteDictionary);
        }

        /// <summary>
        /// Helper Method to create a SortedSet dictionary our of a comma separated list of strings.
        /// </summary>
        /// <param name="autoCompleteDictionary"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetSortedDictionaryFromString(string autoCompleteDictionary)
        {
            return new SortedSet<string>(autoCompleteDictionary.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
        {
            if (object.Equals(storage, value)) return false;

            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
