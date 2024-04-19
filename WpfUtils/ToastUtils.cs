using CommunityToolkit.WinUI.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfUtils
{
    public class ToastUtils
    {
        /// <summary>Toast exception with fileName and lineNumber v1</summary>
        /// <remarks>https://github.com/Pericial/GAP/issues/92#issuecomment-1988339359</remarks>
        public static void ShowExceptionMessageWithFileNameAndLineNumber(Exception ex, string title = "")
        {
            #region Toast exception with fileName and lineNumber v1
            var st = new StackTrace(ex, true); // Get the top stack frame
            var frame = st.GetFrame(0); // Get the line number from the stack frame
            var lineNumber = frame.GetFileLineNumber();
            string fileName = frame.GetFileName();
            new ToastContentBuilder()
                .AddText(title)
                .AddText("ERROR " + fileName + "-" + lineNumber.ToString() + ": " + ex.Message)
            .Show();
            #endregion
        }
    }
}
