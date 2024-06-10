using System.ComponentModel;
using System.Reflection;
using Utils;

namespace SqlOrganize
{
    /// <summary>
    /// Comportamiento general para las clases de datos
    /// </summary>
    public abstract class Data : INotifyPropertyChanged, IDataErrorInfo
    {
        public string entityName;

        /// <summary>Propiedad opcional para indicar que se esta actualizando</summary>
        public bool _isUpdated = false;

        /// <summary>Campos a ignorar para marcar isUpdated</summary>
        public List<string> _isUpdatedIgnore = new() { nameof(IsUpdated), nameof(Msg), nameof(Error) };

        /// <summary>Flag opcional para indicar que debe ejecutarse la validacion</summary>
        public bool _Validate = false;

        /// <summary>
        /// Si se construye una instancia de data con valores por defecto, puede ser necesario acceder a la base de datos para definirlos.
        /// </summary>
        public Db? db;


        public Data Db(Db db)
        {
            this.db = db;
            return this;
        }

        public EntityValues Values()
        {
            return db!.Values(entityName).Set(this);
        }

        public string this[string columnName]
        {
            get
            {
                if (!_Validate)
                    return "";

                // If there's no error, empty string gets returned
                return ValidateField(columnName);
            }
        }

        protected virtual string ValidateField(string columnName)
        {
            return "";
        }

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

        
        /// <summary>Cargar en false al finalizar la inicializacion</summary>
        public bool IsUpdated
        {
            get { return _isUpdated; }
            set
            {
                if(_isUpdated != value)
                {
                    _isUpdated = value;
                    NotifyPropertyChanged(nameof(IsUpdated));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] String propertyName = "")
        {
            if(!_isUpdatedIgnore.Contains(propertyName))
                IsUpdated = true;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
