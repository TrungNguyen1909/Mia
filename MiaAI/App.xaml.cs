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
namespace MiaAI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static event EventHandler StartListeningRequest;
        MainWindow MainUI = new MainWindow();
        public bool isMicrophone = true;
        BackgroundWorker ReminderEngine = new BackgroundWorker();
        SpeechRecognitionEngine listener = new SpeechRecognitionEngine();
        Timer timer = new Timer(60000);
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
            listener.LoadGrammar(new Grammar(new GrammarBuilder(new Choices(new string[] { "Hey Mia", "Mia" }))));
            
            try
            {
                listener.SetInputToDefaultAudioDevice();
            }
            catch { isMicrophone = false; }
            if ((!isRunningOnBattery)&&isMicrophone)
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
            if((!isRunningOnBattery)&&(listener.AudioState==AudioState.Stopped)&&(isMicrophone)) listener.RecognizeAsync();
        }

        private void MainUI_StopCommandWaiting(object sender, EventArgs e)
        {
            if ((listener.AudioState != AudioState.Stopped) && (isMicrophone))
                listener.RecognizeAsyncStop();
        }
        Boolean isRunningOnBattery =(System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Offline);
    }
}
