using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Speech.Recognition;
using System.ComponentModel;
using Store;
using Data;
using System.Timers;
using System.Diagnostics;
using MiaAI.Properties;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;

namespace MiaAI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Public field
        public static event EventHandler StartListeningRequest;
        public bool isMicrophone = true;
        #endregion
        #region Private field
        MainWindow MainUI = new MainWindow();
        Settings settings;
        BackgroundWorker ReminderEngine = new BackgroundWorker();
        SpeechRecognitionEngine listener = new SpeechRecognitionEngine();
        System.Timers.Timer timer = new System.Timers.Timer(60000);
        AppDomain CurrentDomain = AppDomain.CurrentDomain;
        #endregion
        #region Event Handler
        private void Listener_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence >= 0.7)
            {
                if (MainUI.Visibility == Visibility.Hidden)
                {
                    MainUI.Show();
                }
                StartListeningRequest.Invoke(this, new EventArgs());
            }
        }
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!SingleInstance.Start())
            {
                SingleInstance.ShowFirstInstance();
                Application.Current.Shutdown();
            }
            else
            {
                listener.LoadGrammar(new Grammar(new GrammarBuilder(new Choices(new string[] { "Hey Mia", "Mia" }))));
                CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomainHandler);

                try
                {
                    listener.SetInputToDefaultAudioDevice();
                }
                catch { isMicrophone = false; }
                settings = new Settings();

                if ((!isRunningOnBattery) && isMicrophone && ((bool)settings["HeyMia"] == true))
                {
                    listener.RecognizeAsync();
                    Debug.WriteLine("Started waiting for command.");
                }
                else Debug.WriteLine("Handoff is turned off on battery.");
                listener.SpeechRecognized += Listener_SpeechRecognized;
                MainUI.StopCommandWaiting += MainUI_StopCommandWaiting;
                MainUI.StartCommandWaiting += MainUI_StartCommandWaiting;
                ReminderEngine.DoWork += StartReminderService;
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
                ReminderEngine.RunWorkerAsync();
                if (e.Args.Length == 0)
                    MainUI.Show();
                else MainUI.Hide();
            }
        }

        private void AppDomainHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            MessageBox.Show("AppDomainHandler caught : " + e.Message+ "\nRuntime terminating:" + args.IsTerminating, "Error",MessageBoxButton.OK,MessageBoxImage.Error);
            
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(!ReminderEngine.IsBusy)
            ReminderEngine.RunWorkerAsync();
        }

        private void StartReminderService(object sender, DoWorkEventArgs e)
        {
            Debug.WriteLine("Reminder Task started!");
            var DB = ReminderManager.GetReminder();
            foreach (Reminder rm in DB)
            {
                if ((rm.datetime <= DateTime.Now) && (!rm.Notified))
                {
                    rm.Notified = true;
                    ReminderManager.SaveReminder(rm);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var Rn = new ReminderNotification(rm.ID);
                        Rn.Show();
                    }));
                }
            }
            Debug.WriteLine("Reminder task Stopped!");

        }

        private void MainUI_StartCommandWaiting(object sender, EventArgs e)
        {
            if((!isRunningOnBattery)&&(listener.AudioState==AudioState.Stopped)&&(isMicrophone) && ((bool)settings["HeyMia"] == true)) listener.RecognizeAsync();
        }

        private void MainUI_StopCommandWaiting(object sender, EventArgs e)
        {
            if ((listener.AudioState != AudioState.Stopped) && (isMicrophone))
                listener.RecognizeAsyncStop();
        }
        #endregion
        #region Check System Information
        Boolean isRunningOnBattery =(System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Offline);
        public static string AssemblyGuid()
        {
                object[] attributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(GuidAttribute), false);
                if (attributes.Length == 0)
                {
                    return String.Empty;
                }
                return ((GuidAttribute)attributes[0]).Value;
        }
        #endregion
        static public class SingleInstance
        {
            public static readonly int WM_SHOWFIRSTINSTANCE =
                WinApi.RegisterWindowMessage("WM_SHOWFIRSTINSTANCE|{0}", AssemblyGuid());
            static Mutex mutex;
            static public bool Start()
            {
                bool onlyInstance = false;
                string mutexName = String.Format("Local\\{0}", AssemblyGuid());

                // if you want your app to be limited to a single instance
                // across ALL SESSIONS (multiple users & terminal services), then use the following line instead:
                // string mutexName = String.Format("Global\\{0}", ProgramInfo.AssemblyGuid);

                mutex = new Mutex(true, mutexName, out onlyInstance);
                return onlyInstance;
            }
            static public void ShowFirstInstance()
            {
                WinApi.PostMessage(
                    (IntPtr)WinApi.HWND_BROADCAST,
                    WM_SHOWFIRSTINSTANCE,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }
            static public void Stop()
            {
                mutex.ReleaseMutex();
            }
        }
        static public class WinApi
        {
            [DllImport("user32")]
            public static extern int RegisterWindowMessage(string message);

            public static int RegisterWindowMessage(string format, params object[] args)
            {
                string message = String.Format(format, args);
                return RegisterWindowMessage(message);
            }

            public const int HWND_BROADCAST = 0xffff;
            public const int SW_SHOWNORMAL = 1;

            [DllImport("user32")]
            public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

            [DllImportAttribute("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            [DllImportAttribute("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);
            
        }
    }
}
