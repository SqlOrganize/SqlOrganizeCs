﻿using System.ComponentModel;
using System.Reflection;

namespace SqlOrganize
{
    /// <summary>
    /// Comportamiento general para las clases de datos
    /// </summary>
    public abstract class Data : INotifyPropertyChanged, IDataErrorInfo
    {
        /// <summary>
        /// Flag para indicar que debe ejecutarse la validacion
        /// </summary>
        public bool Validate = false;

        /// <summary>
        /// Si se construye una instancia de data con valores por defecto, puede ser necesario acceder a la base de datos para definirlos.
        /// </summary>
        public Db? db;

        public string this[string columnName]
        {
            get
            {
                if (!Validate)
                    return "";

                // If there's no error, empty string gets returned
                return ValidateField(columnName);
            }
        }

        protected abstract string ValidateField(string columnName);

        /// <summary>
        /// Verificar error en propiedades
        /// Notificar cambio en propiedad sin hay un error (para que visualice el error en el formulario)
        /// </summary>
        /// <remarks>
        /// Devuelve un string con la concatenacion de todos los errores.
        /// </remarks>
        public string Error
        {
            get
            {
                PropertyInfo[] properties = this.GetType().GetProperties();

                List<string> errors = new();
                foreach (PropertyInfo property in properties)
                    if (this[property.Name] != "")
                    {
                        NotifyPropertyChanged(property.Name);
                        errors.Add(this[property.Name]);
                    }

                if (errors.Count > 0)
                    return String.Join(" - ", errors.ToArray());

                return "";
            }
        }

        public string Msg { get; set; } = "";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
