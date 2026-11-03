package com.scrapguard.mobile;
import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.net.Uri;
import com.unity3d.player.UnityPlayer;

public final class UpdateBridge {
    public static void openStore(final String receiver) {
        final Activity activity=UnityPlayer.currentActivity;
        activity.runOnUiThread(() -> {
            // Never accept a remote URL or package; use this installed app's package identity.
            Uri uri=Uri.parse("https://play.google.com/store/apps/details?id="+activity.getPackageName());
            try {
                activity.startActivity(new Intent(Intent.ACTION_VIEW,uri).setPackage("com.android.vending"));
            } catch (ActivityNotFoundException missingPlay) {
                try { activity.startActivity(new Intent(Intent.ACTION_VIEW,uri)); }
                catch (ActivityNotFoundException missingBrowser) { UnityPlayer.UnitySendMessage(receiver,"OnUpdateStoreError",""); }
            }
        });
    }
}
