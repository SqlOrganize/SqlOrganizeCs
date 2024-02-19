using SomeWpfApp;
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
        /// Comportamiento general para persistir una celda
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="mainEntityName"></param>
        /// <returns>v1 (2023-11)</returns>
        public static bool DataGridCellEditEndingEventArgs_CellEditEnding<T>(this DataGridCellEditEndingEventArgs e, string mainEntityName, string key, object? value) where T : class, new()
        {
            string? fieldId = null;
            string entityName = mainEntityName;
            string fieldName = key;
            if (key.Contains("__"))
                (fieldId, fieldName, entityName) = ContainerApp.db.KeyDeconstruction(entityName, key);

            return e.Row.DataGridRow_RecursiveEdit<T>(mainEntityName, entityName, fieldName, value, fieldId, false);
        }


        /// <summary>
        /// Comportamiento general para persistir una celda (metodo recursivo)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="mainEntityName"></param>
        /// <param name="exceptionIfMainEntityExists">Dispara una excepcion si se esta modificando la entidad principal y si ya existe su valor con las modificaciones realizadas</param>
        /// <remarks>v2 (2024-02): Compatible con v1. Se agrego el atributo exceptionIfMainEntityExists</remarks>
        public static bool DataGridRow_RecursiveEdit<T>(this DataGridRow row, string mainEntityName, string entityName, string fieldName, object? value, string? fieldId = null, bool reload = false, bool exceptionIfMainEntityExists = true) where T : class, new()
        {
            IDictionary<string, object?> source = row.DataContext.Dict();

            EntityValues v = ContainerApp.db.Values(entityName, fieldId).Set(source);
            var val = v.GetOrNull(fieldName);
            if (val.IsNullOrEmptyOrDbNull())
            { 
                if (value.IsNullOrEmptyOrDbNull())
                    return reload;
            }
            else
                if(val.Equals(value))
                    return reload;

            v.Sset(fieldName, value);
            IDictionary<string, object?>? rowDb = ContainerApp.dao.RowByUniqueFieldOrValues(fieldName, v);
            if (!rowDb.IsNullOrEmpty()) //con el nuevo valor ingresados se obtuvo un nuevo campo unico, no se realiza persistencia y se cambian los valores para reflejar el nuevo valor consultado
            {
                if (fieldId.IsNullOrEmpty() && exceptionIfMainEntityExists)
                    throw new System.Exception("Los datos ingresados en la edición de la celda ya pertenecen a otra fila. No se cumple la restricción de unicidad");

                v.Values(rowDb!);
                T data = v.Get().Obj<T>();
                (row.Item as T).CopyValues<T>(data, sourceNotNull: true);
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

        /// <summary>
        /// Comportamiento general para persistir una celda checkbox (DataGridCheckBoxColumn) v2 (2024-02)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="mainEntityName"></param>
        /// <returns></returns>
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

            ContainerApp.db.Persist().UpdateValueIds(entityName, fieldName, value, v.Get(ContainerApp.db.config.id)).Exec().RemoveCache();
            if (fieldId != null)
                return true;

            return false;
        }

    }
}
