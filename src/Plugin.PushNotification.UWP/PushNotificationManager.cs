using Newtonsoft.Json;
using Plugin.PushNotification.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Windows.Networking.PushNotifications;
using Windows.Storage;

namespace Plugin.PushNotification
{
    /// <summary>
    /// Implementation for Feature
    /// </summary>
    public class PushNotificationManager : IPushNotification
    {
        const string TokenKey = "Token";

        public IPushNotificationHandler NotificationHandler { get; set; }

        public string Token { get { return ApplicationData.Current.LocalSettings.Values.ContainsKey(TokenKey) ? ApplicationData.Current.LocalSettings.Values[TokenKey]?.ToString() : string.Empty; } }

        public event PushNotificationTokenEventHandler OnTokenRefresh;
        public event PushNotificationResponseEventHandler OnNotificationOpened;
        public event PushNotificationDataEventHandler OnNotificationReceived;
        public event PushNotificationDataEventHandler OnNotificationDeleted;
        public event PushNotificationErrorEventHandler OnNotificationError;

        private PushNotificationChannel channel;

        public NotificationUserCategory[] GetUserNotificationCategories()
        {
            throw new NotImplementedException();
        }

        public static void Initialize()
        {
            CrossPushNotification.Current.NotificationHandler = CrossPushNotification.Current.NotificationHandler ?? new DefaultPushNotificationHandler();
        }

        public static void Initialize(IPushNotificationHandler pushNotificationHandler)
        {
            CrossPushNotification.Current.NotificationHandler = pushNotificationHandler;
            Initialize();
        }

        public async Task RegisterForPushNotifications()
        {
            channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            channel.PushNotificationReceived += Channel_PushNotificationReceived;
            ApplicationData.Current.LocalSettings.Values[TokenKey] = channel.Uri;
            OnTokenRefresh?.Invoke(CrossPushNotification.Current, new PushNotificationTokenEventArgs(channel.Uri));
        }

        public void UnregisterForPushNotifications()
        {
            if (channel != null)
                channel.PushNotificationReceived -= Channel_PushNotificationReceived;

            ApplicationData.Current.LocalSettings.Values.Remove(TokenKey);
        }

        private void Channel_PushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            if (args.NotificationType == PushNotificationType.Raw)
            {
                foreach (var pair in JsonConvert.DeserializeObject<Dictionary<string, string>>(args.RawNotification.Content))
                    data.Add(pair.Key, pair.Value);
            }
            else if (args.NotificationType == PushNotificationType.Toast)
            {
                foreach (XmlAttribute attribute in args.ToastNotification.Content.DocumentElement.Attributes)
                    data.Add(attribute.Name, attribute.Value);
            }

            OnNotificationReceived?.Invoke(CrossPushNotification.Current, new PushNotificationDataEventArgs(data));
        }
    }
}