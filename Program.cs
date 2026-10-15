using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace IDMClone
{
    static class Program
    {
        private static EventWaitHandle s_eventWaitHandle;

        [STAThread]
        static void Main(string[] args)
        {
            SecurityGuard.Initialize();

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, args2) =>
            {
                string msg = args2.Exception != null ? args2.Exception.ToString() : "An unexpected error occurred.";
                try { System.IO.File.WriteAllText("crash.log", "ThreadException: " + msg); } catch { }
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, args2) =>
            {
                Exception ex = args2.ExceptionObject as Exception;
                string msg = ex != null ? ex.ToString() : "A background error occurred.";
                try { System.IO.File.WriteAllText("crash.log", "Unhandled AppDomain Exception: " + msg); } catch { }
            };

            bool startInBackground = false;
            if (args != null && args.Length > 0)
            {
                foreach (var arg in args)
                {
                    if (arg.Equals("--background", StringComparison.OrdinalIgnoreCase) ||
                        arg.Equals("-tray", StringComparison.OrdinalIgnoreCase) ||
                        arg.Equals("/tray", StringComparison.OrdinalIgnoreCase) ||
                        arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase))
                    {
                        startInBackground = true;
                        break;
                    }
                }
            }

            // Check if another instance of Turbo X is already running
            EventWaitHandle existingHandle;
            if (EventWaitHandle.TryOpenExisting("TurboX_ShowInstance_Event_Abir", out existingHandle))
            {
                existingHandle.Set();
                existingHandle.Close();
                return;
            }

            s_eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "TurboX_ShowInstance_Event_Abir");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                MainForm mainForm = new MainForm(startInBackground);

                ThreadPool.RegisterWaitForSingleObject(s_eventWaitHandle, (state, timedOut) =>
                {
                    try
                    {
                        if (mainForm != null && !mainForm.IsDisposed)
                        {
                            mainForm.BeginInvoke(new Action(() => mainForm.RestoreMainWindow()));
                        }
                    }
                    catch { }
                }, null, -1, false);

                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("startup_error.log", ex.ToString());
            }
        }
    }
}
