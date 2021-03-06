﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.CognitiveServices.SpeechRecognition;
using System.Speech.Synthesis;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using ApiAiSDK;
using Data;
using System.Globalization;
using System.Media;
using SmartHome;
using Store;
using System.Threading;
using System.ComponentModel;
using MiaAI.Properties;
using System.Windows.Interop;

namespace MiaAI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private fields
        private ApiAi lus;
        private MicrophoneRecognitionClient micClient;
        private string SubscriptionKey = "f1301065313743309c0a865358712543";
        private SpeechSynthesizer reader = new SpeechSynthesizer();
        private Random Choices=new Random();
        private Reminder item;
        public bool IsDone=false;
        #endregion
        #region Initialize
        public MainWindow()
        {
            this.InitializeComponent();
            this.Initialize();
            IsDone = false;
            item = new Reminder();
            textBox.KeyDown += TextBox_KeyDown;
            textBox.TextChanged += TextBox_TextChanged;
            App.StartListeningRequest += StartListeningRequest;
            this.Closing += MainWindow_Closing;
        }


        private void Initialize()
        {
            textBox.Background = (VisualBrush)this.Resources["Hint"];
            try
            {
                reader.SetOutputToDefaultAudioDevice();
            }
            catch
            {
                MessageBox.Show("Speakers or headphone is not detected on your device. Please plug in to hear the assistant voice.","Sound Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            try
            {
                reader.SelectVoice("Microsoft Eva Mobile");
            }
            catch
            {
                MessageBox.Show("Please reinstall the application to fix this error!\nIf you see this error after reinstallation, your system is not supported!", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            var config = new AIConfiguration("6bb0cdf023e54c868895091294ae2a0e", SupportedLanguage.English);
            lus = new ApiAi(config);
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        }
        #endregion
        #region Background Wakeup 
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == App.SingleInstance.WM_SHOWFIRSTINSTANCE)
            {
                this.Show();
                this.Activate();
            }
            return IntPtr.Zero;
        }
        #endregion
        #region Main
        /// <summary>
        /// Configure the Microphone Client to work
        /// </summary>
        private void CreateMicrophoneRecoClient()
        {
            try
            {
                this.micClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(SpeechRecognitionMode.ShortPhrase,"en-US", SubscriptionKey);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message,"Error While Initializiing Mic Client");
                //App.Current.Shutdown(1);
            }
            #region micClientEventHandler
            micClient.OnResponseReceived += this.MicClient_OnResponseReceived;
            micClient.OnConversationError += this.MicClient_OnConversationError;
            micClient.OnPartialResponseReceived += MicClient_OnPartialResponseReceived;
            #endregion
        }
        /// <summary>
        /// Write content
        /// </summary>
        /// <param name="s">Sentence to write and Speak out</param>
        private async Task HistoryWrite(string s, bool speech = false, bool write = true,bool noSpeak=false)
        {
            if (s != null)
            {
                Dispatcher.Invoke(() =>
                {
                    if (write)
                    {
                        textBox1.AppendText(s + "\n"); this.textBox1.ScrollToEnd();
                    }
                });
                var settings = new Settings();
                if (((speech) || ((bool)settings["VoiceFeedback"])) && (!noSpeak))
                {
                    await Task.Run(() => reader.Speak(s));

                }
            }
        }
        /// <summary>
        /// Neutral Language Processing
        /// </summary>
        private async void NLP(string sentence, bool speech = false)
        {
            if (sentence != null&&sentence!="")
            {
                #region Pre-Processing
                if (IsDone) { textBox1.Document.Blocks.Clear(); image.Source = null; }
                StopCommandWaiting.Invoke(this, new EventArgs());
                textBox1.AppendText(sentence + "\n"); this.textBox1.ScrollToEnd();
               
                var Received = lus.TextRequest(sentence);
                #endregion
                if (Received.Result.ActionIncomplete)
                {
                    #region ActionIncomplete
                    await HistoryWrite(Received.Result.Fulfillment.Speech, speech);
                    if (speech)
                        StartSpeak.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    return;
                }
                #endregion
                else
                {
                    #region ActionComplete
                    DeviceManager devman = new DeviceManager();
                    switch (Received.Result.Action)
                    {
                        #region WebSearch
                        case "web.search":
                            if (Received.Result.Parameters.ContainsKey("q")) Process.Start(String.Concat("https://www.google.com/search?q=" + Received.Result.Parameters["q"]));
                            break;
                        #endregion
                        #region Smarthome
                        case "smarthome.appliances_on":
                            if (Received.Result.Parameters["all"].ToString() == "true")
                            {
                                string dev;
                                dev = Received.Result.Parameters["appliance_name"].ToString();
                                if (devman.IsTypeExist(dev))
                                {
                                    HistoryWrite("I will turn on all " + dev, speech);
                                    devman.SetAll(dev, 1);
                                }
                                else
                                    HistoryWrite("I can't find any " + dev, speech);
                            }
                            else
                            {
                                string dev;
                                dev = Received.Result.Parameters["appliance_name"].ToString() + Received.Result.Parameters["number"];
                                if (devman.IsExist(dev))
                                {
                                    HistoryWrite("I will turn on the " + dev, speech);
                                    devman.Set(dev, 1);
                                }
                                else
                                    HistoryWrite("I can't find the " + dev, speech);
                            }
                            break;
                        case "smarthome.appliances_off":

                            if (Received.Result.Parameters["all"].ToString() == "true")
                            {
                                string dev;
                                dev = Received.Result.Parameters["appliance_name"].ToString();
                                if (devman.IsTypeExist(dev))
                                {
                                    HistoryWrite("I will turn off all " + dev, speech);
                                    devman.SetAll(dev, 0);
                                }
                                else
                                    HistoryWrite("I can't find any " + dev, speech);
                            }
                            else
                            {
                                string dev;
                                dev = Received.Result.Parameters["appliance_name"].ToString() + Received.Result.Parameters["number"];
                                if (devman.IsExist(dev))
                                {
                                    HistoryWrite("I will turn off the " + dev, speech);
                                    devman.Set(dev, 0);
                                }
                                else
                                    HistoryWrite("I can't find the " + dev, speech);
                            }
                            break;
                        #endregion
                        #region Clock
                        case "clock.date":
                            if (Received.Result.Parameters["date"].ToString() != null)
                                HistoryWrite("It's " + Received.Result.Parameters["date"], speech);
                            else
                                HistoryWrite("It's " + DateTime.Now.ToShortDateString(), speech);
                            break;
                        case "clock.time":
                            HistoryWrite("It's " + DateTime.Now.ToShortTimeString(), speech);
                            break;
                        #endregion
                        #region Weather
                        case "weather.search":
                            var WE = new Weather();
                            DateTime Now = DateTime.Now;
                            DateTime Converted = DateTime.Now;
                            string location = "autoip";
                            if (Received.Result.Parameters.ContainsKey("geo-city") && Received.Result.Parameters["geo-city"].ToString() != "") { location = Received.Result.Parameters["geo-city"].ToString(); }
                            else { location = "autoip"; }
                            string time = "present";
                            if (Received.Result.Parameters.ContainsKey("date"))
                            {
                                DateTime.TryParseExact(Received.Result.Parameters["date"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out Converted);
                                if (Now < Converted) time = "future";
                            }
                            if (time == "present")
                            {
                                var data = WE.GetCurrentWeather(location);
                                if (data == null)
                                {
                                    HistoryWrite(Received.Result.Fulfillment.Speech);
                                }
                                else
                                {
                                    if (location == "autoip")
                                        HistoryWrite("The current weather at current location is " + data["weather"].ToString().ToLower() + " with the temperature of " + data["temp_c"].ToString() + " C.", speech);
                                    else
                                        HistoryWrite("The current weather in " + location + " is " + data["weather"].ToString().ToLower() + " with the temperature of " + data["temp_c"].ToString() + " C.", speech);
                                    BitmapImage bi3 = new BitmapImage();
                                    bi3.BeginInit();
                                    bi3.UriSource = new Uri(data["icon_url"].ToString(), UriKind.Absolute);
                                    bi3.EndInit();
                                    image.Stretch = Stretch.Uniform;
                                    image.Source = bi3;
                                }
                            }
                            if (time == "future")
                            {
                                var data = WE.GetForecastWeather(Converted, location);
                                if (data == null)
                                    HistoryWrite(Received.Result.Fulfillment.Speech);
                                else
                                {
                                    if (location == "autoip")
                                        HistoryWrite("The weather at current location on " + data["date"].ToString() + " will be " + data["conditions"].ToString().ToLower() + " with the temperature of " + data["low_c"].ToString() + " C at lowest and " + data["high_c"] + " C at highest.", speech);
                                    else
                                        HistoryWrite("The weather in " + location + " on " + data["date"].ToString() + " will be " + data["conditions"].ToString().ToLower() + " with the temperature of " + data["low_c"].ToString() + " C at lowest and " + data["high_c"] + " C at highest.", speech);
                                    BitmapImage bi3 = new BitmapImage();
                                    bi3.BeginInit();
                                    bi3.UriSource = new Uri(data["icon_url"].ToString(), UriKind.Absolute);
                                    bi3.EndInit();
                                    image.Stretch = Stretch.Uniform;
                                    image.Source = bi3;
                                }
                            }
                            break;
                        #endregion
                        #region Easteregg
                        case "easteregg.singing":
                            int Choice = Choices.Next() % 3;
                            var path = System.IO.Path.Combine("Singing", Convert.ToString(Choice)+".wav");
                            SoundPlayer player = new SoundPlayer(path);
                            player.Play();
                            if (Choice == 1)
                                HistoryWrite("You are my sunshine, my only sunshine. You make me happy, when skies are gray.");

                            else if (Choice == 2)
                                HistoryWrite("Oh he floats through the air with the greatest of ease, this daring young man on the flying trapeze.");
                            else HistoryWrite("Twinkle twinkle little star, how I wonder what you are!");
                            break;
                        #endregion
                        #region Reminder
                        case "reminder.add":
                            BitmapImage bir = new BitmapImage();
                            bir.BeginInit();
                            bir.UriSource = new Uri(@"\Assets\Reminder.png", UriKind.Relative);
                            bir.EndInit();
                            image.Stretch = Stretch.Uniform;
                            image.Source = bir;
                            string summary = Received.Result.Parameters["summary"].ToString();
                            time = Received.Result.Parameters["time"].ToString();
                            DateTime converted = new DateTime();

                            if (time.Contains("/"))
                            {
                                /*if (!time.Contains("T"))
                                    time = time.Substring(time.IndexOf('/') + 1);
                                else */
                                time = time.Remove(time.IndexOf('/'));
                                time = time.Trim('/');
                            }
                            if (time.Contains("T"))
                            {
                                DateTime.TryParseExact(time, "yyyy-MM-dd\\THH:mm:ss\\Z", CultureInfo.InvariantCulture, DateTimeStyles.None, out converted);
                            }
                            else
                            {
                                if (time.Contains("-"))
                                {
                                    DateTime.TryParseExact(time, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out converted);
                                }
                                else
                                    DateTime.TryParseExact(time, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out converted);
                            }
                            while (converted < DateTime.Now) converted= converted.AddDays(1);
                            summary = summary.Trim();
                            item.reminder = summary;
                            item.datetime = converted;
                            item.Notified = false;
                            string cvt;
                            if (converted.Date > DateTime.Now.Date)
                                cvt = converted.ToString("dddd, MMMM dd, yyyy h:mm tt");
                            else cvt = converted.ToShortTimeString();
                            
                            await HistoryWrite("I'll remind you to " + summary + " on " + cvt + ". Ready to confirm?", speech); 
                            if (speech)
                                Dispatcher.Invoke((Action)(() =>
                                {
                                    StartSpeak_Click(this, new RoutedEventArgs());
                                }));
                            IsDone = false;
                            return;
                        case "reminder.confirm":
                            if (item != new Reminder())
                            {
                                ReminderManager.SaveReminder(item);
                                HistoryWrite(Received.Result.Fulfillment.Speech, speech);
                                item = new Reminder();

                            }
                            else
                            {
                                HistoryWrite("I'm not sure what you said.", speech);
                            }
                            IsDone = true;
                            break;
                        case "reminder.cancel":
                            item = new Reminder();
                            HistoryWrite(Received.Result.Fulfillment.Speech, speech);
                            break;
                        #endregion
                        #region Location
                        case "location.current":
                            string loc = Location.GetCurrentAddress();
                            if (loc != null)
                            {
                                HistoryWrite("We are at " + loc + " .", speech);
                            }
                            else
                            {
                                HistoryWrite(Received.Result.Fulfillment.Speech, speech);
                            }
                            break;
                        #endregion
                        #region History
                        case "history.event":
                            List<Dictionary<string, object>> result = null;
                            DateTime ChosenDate=new DateTime();
                            bool IsToday = true;
                            if (Received.Result.Parameters["date"].ToString() == "")
                            {
                                result= History.GetHistory(DateTime.Now);
                            }
                            else
                            {
                                DateTime.TryParseExact(Received.Result.Parameters["date"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out ChosenDate);
                                result=History.GetHistory(ChosenDate);
                                IsToday = false;
                            }
                            if(result==null)
                            {
                                HistoryWrite(Received.Result.Fulfillment.Speech, speech);
                            }
                            else
                            {
                                Choice = Choices.Next()%result.Count-1;
                                var picked = result[Choice];
                                if (IsToday)
                                    HistoryWrite("On this day in " + picked["year"].ToString() + ", " +AccentsRemover.RemoveAccents( picked["text"].ToString()), speech);
                                else
                                    HistoryWrite("On "+ChosenDate.ToShortDateString() +" in " + picked["year"].ToString() + ", " +AccentsRemover.RemoveAccents( picked["text"].ToString()), speech);
                            }
                            break;
                        #endregion
                        #region Knowledge & Dictionary
                        case "dictionary.search":
                            string q = Received.Result.Parameters["q"].ToString();
                            var definition = Dictionary.Define(q);
                            if (definition == null)
                            {
                                HistoryWrite(Received.Result.Fulfillment.Speech, speech);
                            }
                            else
                            {
                                var First = definition[0];
                                HistoryWrite(q + " has a " + First.lexicalCategory + " meaning, " + First.entries[0].senses[0].definitions[0],speech,false);
                                HistoryWrite(q, false, true, true);
                                foreach(var entry in definition)
                                {
                                    HistoryWrite(entry.lexicalCategory, false, true, true);
                                    foreach(var def in entry.entries[0].senses)
                                    {
                                        HistoryWrite("-" + def.definitions[0], false, true, true);
                                    }
                                }
                            }
                            break;
                        case "knowledge.search":
                            
                            var knowledgeresult = Knowledge.GetKnowledge(Received.Result.Parameters["questionword"].ToString()+" "+Received.Result.Parameters["q"].ToString());
                            if (knowledgeresult != null)
                            {

                                //BitmapImage bitmap = new BitmapImage(new Uri(@"Assets\simple.jpg", UriKind.Relative));
                                BitmapImage bitmap=new BitmapImage();
                                BitmapImage bik = new BitmapImage();
                                try
                                {
                                    bitmap = new BitmapImage(new Uri(knowledgeresult["simpleurl"], UriKind.Absolute));
                                }
                                catch { }
                                try
                                {
                                    bik = new BitmapImage(new Uri(knowledgeresult["imageurl"], UriKind.Absolute));
                                }
                                catch { }
                                image.Source = bik;
                                Image simpleImage = new Image();
                                simpleImage.Source = bitmap;
                                simpleImage.Stretch = Stretch.Uniform;
                                InlineUIContainer container = new InlineUIContainer(simpleImage);
                                Paragraph paragraph = new Paragraph(container);
                                HistoryWrite(knowledgeresult["response"], speech);
                                textBox1.Document.Blocks.Add(paragraph);

                            }
                            else
                                Process.Start(String.Concat("https://www.google.com/search?q=" + Received.Result.Parameters["q"]));

                            break;
                        #endregion
                        default:
                            HistoryWrite(Received.Result.Fulfillment.Speech, speech);
                            break;
                    }
                    #endregion
                }
                StartCommandWaiting.Invoke(this, new EventArgs());
                IsDone = true;
            }
        }
        #endregion
        #region EventHandler
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void StartListeningRequest(object sender, EventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                StartSpeak.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }));
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            StopCommandWaiting.Invoke(this, new EventArgs());
            if (e.Key == Key.Enter)
            {
                NLP(textBox.Text);
                textBox.Clear();
            }
        }
        
        private void MicClient_OnPartialResponseReceived(object sender, PartialSpeechResponseEventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                textBox.Background = Brushes.Transparent;
                this.textBox.Text = e.PartialResult;
            }));
        }

        private void MicClient_OnConversationError(object sender, SpeechErrorEventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                textBox.Clear();
                this.textBox1.AppendText(("An error Occured"+"\n")); this.textBox1.ScrollToEnd();
                this.textBox1.AppendText("Error data:" + e.SpeechErrorText + "\n");
                this.StartCommandWaiting.Invoke(this, new EventArgs());
                
            }));
        }

        private void MicClient_OnResponseReceived(object sender, SpeechResponseEventArgs e)
        {
            try
            {
                Dispatcher.Invoke((Action)(() =>
                {
                    this.micClient.EndMicAndRecognition();
                    string Result = null;
                    this.textBox.Clear();
                    StartSpeakImage.Source = new BitmapImage(new Uri(@"Assets\microMute.jpg", UriKind.Relative));
                    if (e.PhraseResponse.Results.Length != 0)
                    {
                        Result += e.PhraseResponse.Results[0].DisplayText;
                    }
                    StartSpeak.IsEnabled = true;
                    Thread.Sleep(10);
                    NLP(Result,true);
                    //Process.Start(String.Concat("https://www.google.com/search?q=" + Result));
                    
                    

                }));
            }
            catch { }
        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBox.Text == "")
                textBox.Background = (VisualBrush)this.Resources["Hint"];
            else
                textBox.Background = Brushes.Transparent;
        }
        private void StartSpeak_Click(object sender, RoutedEventArgs e)
        {
            this.StopCommandWaiting.Invoke(this, new EventArgs());
            this.StartSpeak.IsEnabled = false;
            this.CreateMicrophoneRecoClient();
            textBox.Background =(VisualBrush) this.Resources["Hint2"];
            StartSpeakImage.Source = new BitmapImage(new Uri( @"Assets\microSpeak.jpg",UriKind.Relative));
            try
            {
                this.micClient.StartMicAndRecognition();
            }
            catch(Exception em)
            {
                MessageBox.Show(em.Message, "Error While Start Mic Client");
                //App.Current.Shutdown(2);
            }

        }
        
        #endregion
        #region Events
        public event EventHandler StopCommandWaiting;
        public event EventHandler StartCommandWaiting;
        #endregion
    }
}
