using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Exception = Java.Lang.Exception;
using ForegroundServiceType = Android.Content.PM.ForegroundService;

namespace DirectPackageInstaller.Android;

[Service(ForegroundServiceType = ForegroundServiceType.TypeDataSync, Exported = false)]
public class ForegroundService : Service
{
    private static Dictionary<string, Action> IntentActionMap = new();
    
    public static int IDCount = 1;
    
    public int NotificationID = IDCount++;
        
    private NotificationChannel _channel;
    
    public override void OnCreate()
    {        
        BindChannel();
        BindForeground();
        
        base.OnCreate();
    }

    public override void OnDestroy()
    {
        UnbindForeground();
        base.OnDestroy();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        BindChannel();
        BindForeground();

        var ID = intent?.GetStringExtra("ID");
        if (ID != null && IntentActionMap.ContainsKey(ID))
        {
            var Action = IntentActionMap[ID];
            Action?.Invoke();
        }

        return StartCommandResult.Sticky;
    }
    
    public static void StartService(Context context, Action? Action)
    {
        var intent = new Intent(context, typeof(ForegroundService));
        intent.PutExtra("ID",IntentActionMap.Count.ToString());

        if (Action != null)
        {
            IntentActionMap[IntentActionMap.Count.ToString()] = Action;
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            context.StartForegroundService(intent);
        }
        else
        {
            context.StartService(intent);
        }
    }


    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    private void BindChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            try
            {
                _channel = new NotificationChannel("ServiceChannel", "Package transfer", NotificationImportance.Low);
                _channel.SetSound(null, null);
                _channel.SetShowBadge(false);
                _channel.EnableVibration(false);
                NotificationManager.FromContext(this)?.CreateNotificationChannel(_channel);
            }
            catch (Exception ex)
            {
                MainActivity.LogFatalError(ex);
            }
        }
    }

    private void BindForeground()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            try
            {
                var Intent = new Intent(this, typeof(NotificationReceiver));
                PendingIntentFlags Flags = Build.VERSION.SdkInt >= BuildVersionCodes.S ? PendingIntentFlags.Immutable : 0;
                var pendingIntent = PendingIntent.GetBroadcast(this, 0, Intent, Flags);
                
                var Notification = new Notification.Builder(this, "ServiceChannel");
                Notification.SetContentIntent(pendingIntent);
                Notification.SetSmallIcon(global::Android.Resource.Drawable.StatSysDownload);
                Notification.SetContentTitle("Direct Package Installer");
                Notification.SetContentText("Ready to send packages");
                Notification.SetOngoing(true);
                Notification.SetOnlyAlertOnce(true);

                if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
                    StartForeground(NotificationID, Notification.Build());
                else
                    StartForeground(NotificationID, Notification.Build(), ForegroundServiceType.TypeDataSync);
            }
            catch (Exception ex)
            {
                MainActivity.LogFatalError(ex);
            }
        }
    }

    private void UnbindForeground()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            NotificationManager.FromContext(this)?.Cancel(NotificationID);
        }
    }
    
}   
[BroadcastReceiver(Exported = false)]
public class NotificationReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        context?.StartActivity(context.PackageManager.GetLaunchIntentForPackage(context.PackageName)
            .SetFlags(ActivityFlags.BroughtToFront | ActivityFlags.ReorderToFront | ActivityFlags.NewTask));
    }
}