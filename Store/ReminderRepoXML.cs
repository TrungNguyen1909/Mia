using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Data;

namespace Store
{
    public class ReminderRepoXML
    {
        static string storeLocation;
        static List<Reminder> Reminders;

        static ReminderRepoXML()
        {
            // set the db location
            storeLocation = DatabaseFilePath;
            Reminders = new List<Reminder>();

            // deserialize XML from file at dbLocation
            ReadXml();
        }

        static void ReadXml()
        {
            if (File.Exists(storeLocation))
            {
                var serializer = new XmlSerializer(typeof(List<Reminder>));
                using (var stream = new FileStream(storeLocation, FileMode.Open))
                {
                    Reminders = (List<Reminder>)serializer.Deserialize(stream);
                }
            }
        }
        static void WriteXml()
        {
            UpdateID();
            var serializer = new XmlSerializer(typeof(List<Reminder>));
            using (var writer = new StreamWriter(storeLocation))
            {
                serializer.Serialize(writer, Reminders);
            }
        }
        static void UpdateID()
        {
            var last = 0;
            foreach(Reminder t in Reminders)
            {
                t.ID = ++last;
            }
        }
        public static string DatabaseFilePath
        {
            get
            {
                var storeFilename = "ReminderDB.xml";
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),storeFilename);
                return path;
            }
        }

        public static Reminder GetReminder(int id)
        {
            for (var t = 0; t < Reminders.Count; t++)
            {
                if (Reminders[t].ID == id)
                    return Reminders[t];
            }
            return new Reminder() { ID = id };
        }

        public static IEnumerable<Reminder> GetReminders()
        {
            return Reminders;
        }

        /// <summary>
        /// Insert or update a Reminder
        /// </summary>
        public static int SaveReminder(Reminder item)
        {
            var max = 0;
            if (Reminders.Count > 0)
                max = Reminders.Max(x => x.ID);

            if (item.ID == 0)
            {
                item.ID = ++max;
                Reminders.Add(item);
            }
            else
            {
                var i = Reminders.Find(x => x.ID == item.ID);
                i = item; // replaces item in collection with updated value
            }

            WriteXml();
            return max;
        }

        public static int DeleteReminder(int id)
        {
            for (var t = 0; t < Reminders.Count; t++)
            {
                if (Reminders[t].ID == id)
                {
                    Reminders.RemoveAt(t);
                    WriteXml();
                    return 1;
                }
            }

            return -1;
        }
    }
}
