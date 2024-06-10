#nullable enable
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Input;
using Utils;

namespace WpfUtils
{
    //<summary>Extensiones para ComboBox</summary>
    public static class ComboBoxExtensions
    {
        public static void InitBooleanSiNo(ComboBox comboBox)
        {
            #region pendienteComboBox
            comboBox.SelectedValuePath = "Key";
            comboBox.DisplayMemberPath = "Value";
            comboBox.Items.Add(new KeyValuePair<bool, string>(true, "Sí"));
            comboBox.Items.Add(new KeyValuePair<bool, string>(false, "No"));
            #endregion
        }

        public static void InitBooleanNullSiNo(ComboBox comboBox)
        {
            #region pendienteComboBox
            comboBox.SelectedValuePath = "Key";
            comboBox.DisplayMemberPath = "Value";
            comboBox.Items.Add(new KeyValuePair<bool?, string>(null, "(Todos)"));
            comboBox.Items.Add(new KeyValuePair<bool, string>(true, "Sí"));
            comboBox.Items.Add(new KeyValuePair<bool, string>(false, "No"));
            #endregion
        }

        public static void SetKeyUp(KeyEventArgs e, DispatcherTimer timer)
        {
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Enter || e.Key == Key.LeftShift || e.Key == Key.RightShift || e.Key == Key.Home || e.Key == Key.End)
                return; // Skip navigation keys


            timer.Stop();
            timer.Start();
        }

    }
}
