using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Store;
using Data;

namespace MiaAI
{
    /// <summary>
    /// Interaction logic for ReminderNotification.xaml
    /// </summary>
    public partial class ReminderNotification : Window
    {
        int id;
        SoundPlayer sp = new SoundPlayer(@"C:\Windows\Media\Alarm01.wav");
        Reminder item = new Reminder();
        public ReminderNotification(int ID)
        {
            InitializeComponent();
            sp.PlayLooping();
            StopButton.Click += StopButton_Click;
            id = ID;
            item = ReminderManager.GetReminder(id);
            Data.Text = item.reminder.ToUpper();
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                var workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
                var corner = transform.Transform(new Point(workingArea.Right, workingArea.Bottom));

                this.Left = corner.X - this.ActualWidth - 100;
                this.Top = corner.Y - this.ActualHeight;
            }));

        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            sp.Stop();
            if (Repeat.IsChecked.HasValue && Repeat.IsChecked.Value)
            {
                item.datetime=item.datetime.AddMinutes(9);
                item.Notified = false;
                ReminderManager.SaveReminder(item);
            }
            this.Close();
        }
    }
}
