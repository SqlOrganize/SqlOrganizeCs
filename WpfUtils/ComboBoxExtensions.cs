#nullable enable
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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

    }
}
