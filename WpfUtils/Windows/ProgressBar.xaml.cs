using System.Windows;
using Utils;

namespace GapApp.WpfUtils.Windows
{
    /// <summary>
    /// Lógica de interacción para ProgressBar.xaml v1
    /// </summary>
    public partial class ProgressBar : Window
    {
        public ProgressBar(string? title = null)
        {
            if(!title.IsNullOrEmptyOrDbNull())
                this.Title = title;

            InitializeComponent();
        }

        public void UpdateProgress(int percentage)
        {
            // When progress is reported, update the progress bar control.
            pbLoad.Value = percentage;

            // When progress reaches 100%, close the progress bar window.
            if (percentage == 100)
            {
                Close();
            }
        }
    }
}
