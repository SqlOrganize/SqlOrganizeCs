using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfUtils.Controls
{
    public class CustomComboBox : ComboBox
    {

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                e.Handled = false;// The DatePicker set this as handled, which breaks the DataGrid commit model.
            base.OnKeyDown(e);
        }

   }
}
