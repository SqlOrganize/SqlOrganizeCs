#nullable enable
using CommunityToolkit.WinUI.Notifications;
using SqlOrganize;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Utils;

namespace WpfUtils
{
    //Comportamiento general para DataGrid
    public static class DataGridUtils
    {

        /// <summary>Obtener key and value, en el procesamiento de columnas</summary>
        /// <remarks>No utilizado, se deja como referencia</remarks>
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
                key = ((Binding)column.Binding).Path.Path; //column's binding (using System.Windows.Data)
                value = (e.EditingElement as TextBox)!.Text;
                return (key, value);
            }

            return (key, value);
        }

        /// <summary>Comportamiento general para persistir una celda v1</summary>
        public static bool DataGridCellEditEndingEventArgs_CellEditEnding<T>(this Db db, DataGridCellEditEndingEventArgs e, string mainEntityName, string key, object? value) where T : class, new()
        {
            if(e.EditAction != DataGridEditAction.Commit)
                return false;

            string? fieldId = null;
            string entityName = mainEntityName;
            string fieldName = key;
            if (key.Contains("__"))
                (fieldId, fieldName, entityName) = db.KeyDeconstruction(entityName, key);

            return db.DataGridRow_RecursiveEdit<T>(e.Row, mainEntityName, entityName, fieldName, value, fieldId, false);
        }

        /// <summary>Comportamiento general para persistir una celda (metodo recursivo) v2</summary>
        /// <param name="exceptionIfMainEntityExists">Dispara una excepcion si se esta modificando la entidad principal y si ya existe su valor con las modificaciones realizadas</param>
        public static bool DataGridRow_RecursiveEdit<T>(this Db db, DataGridRow row, string mainEntityName, string entityName, string fieldName, object? value, string? fieldId = null, bool reload = false, bool exceptionIfMainEntityExists = true) where T : class, new()
        {
            IDictionary<string, object?> source = row.DataContext.Dict();

            EntityValues v = db.Values(entityName, fieldId).Set(source);
            var fieldValue = v.GetOrNull(fieldName);
            if(fieldValue.IsNullOrEmptyOrDbNull()) {
                if (value.IsNullOrEmptyOrDbNull())
                    return (reload) ? v.Check() : false;
            }
            else
            {
                if (fieldValue!.Equals(value))
                    return (reload) ? v.Check() : false;
            }

            v.Sset(fieldName, value);
            IDictionary<string, object?>? rowDb = v.RowByUniqueFieldOrValues(fieldName);
            if (!rowDb.IsNullOrEmpty()) //con el nuevo valor ingresados se obtuvo un nuevo campo unico, no se realiza persistencia y se cambian los valores para reflejar el nuevo valor consultado
            {
                if (fieldId.IsNullOrEmpty() && exceptionIfMainEntityExists)
                    throw new Exception("Los datos ingresados en la edición de la celda ya pertenecen a otra fila. No se cumple la restricción de unicidad");

                v.Values(rowDb!);
                T data = v.Get().Obj<T>();
                (row.Item as T).CopyValues<T>(data, sourceNotNull: true);
            }
            else //con el nuevo valor ingresados no se obtuvo un nuevo campo unico, se realiza persistencia (insertar o modificar) del nuevo valor
            {
                if (!v.Check())
                {
                    new ToastContentBuilder()
                    .AddText("ERROR: " + v.Logging.ToString())
                    .Show();
                    row.Item.SetPropertyValue(fieldName, value);
                    return false;
                }

                v.Persist();
                new ToastContentBuilder()
                    .AddText("Registro realizado de " + entityName)
                    .Show();
            }

            if (fieldId != null)
            {
                (fieldId, fieldName, entityName, value) = v.ParentVariables(mainEntityName);
                return db.DataGridRow_RecursiveEdit<T>(row, mainEntityName, entityName, fieldName, value, fieldId, true);
            }

            return reload;
        }

        ///<summary>Comportamiento general para persistir una celda checkbox (DataGridCheckBoxColumn) v2 (2024-02)</summary>
        public static bool DataGridCell_CheckBoxClick<T>(this Db db, DataGridCell cell, string entityName) where T : class, new()
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
                (fieldId, fieldName, entityName) = db.KeyDeconstruction(entityName, key);

            IDictionary<string, object?> source = cell.DataContext.Dict();

            EntityValues v = db.Values(entityName, fieldId).Set(source);

            v.Sset(fieldName, value);
            DataGridRow row = DataGridRow.GetRowContainingElement(cell);
            row.Item.SetPropertyValue(key, value);
            if (!v.Check())
                return false;

            db.Persist().UpdateValueIds(entityName, fieldName, value, v.Get(db.config.id)).Exec().RemoveCache();
            if (fieldId != null)
                return true;

            return false;
        }

        /// <summary>Codigo general para eliminar una fila en un datagrid v2</summary>        
        public static void DeleteRowFromDataGrid<T>(this Db db, string entityName, ObservableCollection<T> oc, T data, string title = "")
        {
            try
            {
                if (!data.GetPropertyValue("id").IsNullOrEmpty())
                    db.Persist().DeleteIds(entityName, data.GetPropertyValue("id")!).Exec().RemoveCache();
                oc.Remove(data);
            }
            catch (Exception ex)
            {
                ToastUtils.ShowExceptionMessageWithFileNameAndLineNumber(ex, title);
            }
        }

        public static void SaveRowFromDataGrid(this Db db, string entityName, Data data, string title = "")
        {
            var p = db.Persist();
            try
            {
                p.Persist(entityName, data).Exec().RemoveCache();
                new ToastContentBuilder()
                 .AddText(title)
                 .AddText("Registro realizado")
                 .Show();
            }
            catch (Exception ex)
            {
                ToastUtils.ShowExceptionMessageWithFileNameAndLineNumber(ex, title);
            }
        }

      

    }
}
