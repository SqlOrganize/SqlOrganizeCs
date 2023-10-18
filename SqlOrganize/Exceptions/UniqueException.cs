using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlOrganize.Exceptions
{

    /// <summary>
    /// Excepción para indicar que no puede definirse valor unico
    /// </summary>
    public class UniqueException : Exception
    {
        public UniqueException(string? message) : base(message)
        {
        }
    }
}
