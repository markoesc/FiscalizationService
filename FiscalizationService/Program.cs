using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MarkoEsc.FiscalizationService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += Application_ThreadException;
            Application.Run(new frmMain());
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show(ExtractException(e.Exception), "Application error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }

        public static string ExtractException(Exception ex)
        {
            string message = ex.Message;

            if (ex.InnerException != null)
                message += Environment.NewLine + ExtractException(ex.InnerException);

            return message;
        }
    }
}
