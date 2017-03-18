using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Data;

namespace Store
{
    public static class ReminderManager
    {
        static ReminderManager()
        { }
        public static Reminder GetReminder(int id)
        {
            return ReminderRepoXML.GetReminder(id);
        }
        public static List<Reminder> GetReminder()
        {
            return new List<Reminder>(ReminderRepoXML.GetReminders());
        }
        public static int SaveReminder(Reminder item)
        {
            return ReminderRepoXML.SaveReminder(item);
        }
        public static int DeleteReminder(int id)
        {
            return ReminderRepoXML.DeleteReminder(id);
        }
    }
}
