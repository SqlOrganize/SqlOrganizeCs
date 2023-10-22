using System.Windows.Controls;
using System.Windows.Data;

namespace WpfUtils
{
    public static class DataGridUtils
    {

        /// <summary>
        /// Obtener key and value, en el procesamiento de columnas.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static (string key, object? value) GetKeyAndValue(this DataGridCellEditEndingEventArgs e)
        {
            string key = "";
            object? value = null;

            var columnCh = e.Column as DataGridCheckBoxColumn; //los campos checkbox se procesan de forma independiente.
            if (columnCh != null)
                return (key, value);

            var columnCo = e.Column as DataGridComboBoxColumn;
            if (columnCo != null)
            {
                key = ((Binding)columnCo.SelectedValueBinding).Path.Path; //column's binding
                value = (e.EditingElement as ComboBox)!.SelectedValue;
                return (key, value);
            }

            var column = e.Column as DataGridBoundColumn;
            if (column != null)
            {
                key = ((Binding)column.Binding).Path.Path; //column's binding
                value = (e.EditingElement as TextBox)!.Text;
                return (key, value);
            }

            return (key, value);
        }

    }
}
