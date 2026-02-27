using ImGuiNET;

namespace Titled_Gui.Notifications
{
    internal class Library
    {
        public static List<string> notifis = new();
        public static string NotificationText = string.Empty;
        public static void LoadOnce()
        {
            Console.WriteLine("\nLoading notis once.");
            ImGui.Begin("Titled_NotificationHud");

            ImGui.Text(NotificationText);

            ImGui.End();
        }

        public static void SendNotification(string title, string message)
        {
            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(message))
            {
                notifis.Add(title);
                Console.WriteLine($"\nSending notification: {title} - {message}");
                NotificationText = $"{title}: {message}";
            }
        }

        public static void ClearAllNotifications()
        {
            foreach (var item in notifis)
            {
                notifis.Clear();
            }
            NotificationText = "";
        }

        public static void Wrap()
        {

        }
    }
}