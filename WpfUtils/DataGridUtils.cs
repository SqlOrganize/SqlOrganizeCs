using Fines2Wpf;
using Fines2Wpf.Model;
using Google.Protobuf.WellKnownTypes;
using SqlOrganize;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Data;
using Utils;

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

        public static bool DataGridCellEditEndingEventArgs_CellEditEnding<T>(this DataGridCellEditEndingEventArgs e, string mainEntityName) where T : class, new()
        {
            if(e.EditAction != DataGridEditAction.Commit)
                return false;

            var result = e.GetKeyAndValue();
            string key = result.key;
            object? value = result.value;

            if (key.IsNullOrEmpty())
                return false;

            DataGridRow row = e.Row;

            string? fieldId = null;
            string entityName = mainEntityName;
            string fieldName = key;
            if (key.Contains("__"))
                (fieldId, fieldName, entityName) = ContainerApp.db.KeyDeconstruction(entityName, key);

            return row.DataGridRow_RecursiveEdit<T>(mainEntityName, entityName, fieldName, value, fieldId, false);
        }

        public static bool DataGridRow_RecursiveEdit<T>(this DataGridRow row, string mainEntityName, string entityName, string fieldName, object? value, string? fieldId = null, bool reload = false) where T : class, new()
        {
            IDictionary<string, object?> source = row.DataContext.Dict();

            EntityValues v = ContainerApp.db.Values(entityName, fieldId).Set(source);
            if (v.GetOrNull(fieldName).Equals(value))
                return reload;

            v.Sset(fieldName, value);
            IDictionary<string, object>? rowDb = ContainerApp.dao.RowByUniqueFieldOrValues(fieldName, v);
            if (!rowDb.IsNullOrEmpty()) //con el nuevo valor ingresados se obtuvo un nuevo campo unico, no se realiza persistencia y se cambian los valores para reflejar el nuevo valor consultado
            {
                v.Set(rowDb!);
                (row.Item as T).CopyValues<T>(v.Get().Obj<T>(), sourceNotNull: true);
            }
            else //con el nuevo valor ingresados no se obtuvo un nuevo campo unico, se realiza persistencia (insertar o modificar) del nuevo valor
            {
                if (!v.Check())
                {
                    row.Item.SetPropertyValue(fieldName, value);
                    return false;
                }

                ContainerApp.dao.Persist(v);
            }

            if (fieldId != null)
            {
                (fieldId, fieldName, entityName, value) = v.ParentVariables(mainEntityName);
                return DataGridRow_RecursiveEdit<T>(row, mainEntityName, entityName, fieldName, value, fieldId, true);
            }

            return reload;
        }

        public static bool DataGridCell_CheckBoxClick<T>(this DataGridCell cell, string entityName) where T : class, new()
        {
            #region definir key y value
            var column = cell!.Column as DataGridBoundColumn;
            var checkBox = cell.Content as CheckBox;
            var o = cell.DataContext as T; //es necesaria esta conversion ya que DataContext en algunas ocasiones se asigna a un recurso no valido
            if (o == null)
                return false;

            string key = ((Binding)column!.Binding).Path.Path; //column's binding.
            bool? value = checkBox!.IsChecked;
            #endregion

            bool? value_ = (bool?)o.GetPropertyValue(key);
            if (value_ == value) //debido a que se accede continuamente al metodo EntityGrid_CellCheckBoxClick, realizamos la comparacion en esta instancia para evitar continuar procesando
                return false;

            string? fieldId = null;
            string fieldName = key;

            if (key.Contains("__"))
                (fieldId, fieldName, entityName) = ContainerApp.db.KeyDeconstruction(entityName, key);

            IDictionary<string, object?> source = cell.DataContext.Dict();

            EntityValues v = ContainerApp.db.Values(entityName, fieldId).Set(source);

            v.Sset(fieldName, value);
            DataGridRow row = DataGridRow.GetRowContainingElement(cell);
            row.Item.SetPropertyValue(key, value);
            if (!v.Check())
                return false;

            List<object> ids = new List<object>() { v.Get(ContainerApp.db.config.id) };
            ContainerApp.db.Persist(entityName).UpdateValue(fieldName, value, ids).Exec().RemoveCache();
            if (fieldId != null)
                return true;

            return false;
        }

    }
}
