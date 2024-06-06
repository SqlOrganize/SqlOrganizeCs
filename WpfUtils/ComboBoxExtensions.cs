#nullable enable
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Utils;

///OBSOLETO!!!!
namespace WpfUtils
{
    //Comportamiento general para DataGrid
    public static class AutocompleteUtils
    {
        ///<summary>Método General Autocomplete v2.2 - GotFocus</summary>
        public static void GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as ComboBox).IsDropDownOpen = true;
        }

        ///<summary>Método General Autocomplete v2.2 - TextChangedCompare</summary>
        public static bool TextChangedCompare(ComboBox cb, string? label)
        {
            if (cb.Text.IsNullOrEmpty())
                cb.IsDropDownOpen = true;
            if (cb.SelectedIndex > -1)
            {
                if (cb.Text.Equals(label))
                    return false;
                cb.Text = "";
                return true;
            }

            return true;
        }

        ///<summary>Método General Autocomplete v2.2 - SelectionChanged</summary>
        public static void SelectionChanged(object sender, RoutedEventArgs e)
        {
            var cb = (ComboBox)sender;

            if (cb.SelectedIndex < 0)
                cb.IsDropDownOpen = true;
        }

        ///<summary>Método General Para Timers v2 - TextChangedTimer</summary>
        public static void TextChangedTimer(string text, DispatcherTimer? timer, EventHandler e)
        {
            if (timer == null)
            {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(300);
                timer.Tick += e;
            }

            timer.Stop(); // Resets the timer
            timer.Tag = text; // This should be done with EventArgs
            timer.Start();
        }

    }
}
