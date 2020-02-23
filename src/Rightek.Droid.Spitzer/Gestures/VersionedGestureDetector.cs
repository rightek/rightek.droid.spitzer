using Android.Content;
using Rightek.Droid.Spitzer.Interfaces;

namespace Rightek.Droid.Spitzer.Gestures
{
    public sealed class VersionedGestureDetector
    {
        public static IGestureDetector NewInstance(Context context, IOnGestureListener listener)
        {
            int sdkVersion = (int)Android.OS.Build.VERSION.SdkInt;
            IGestureDetector detector;

            if (sdkVersion < (int)Android.OS.BuildVersionCodes.Eclair) detector = new CupcakeGestureDetector(context);
            else if (sdkVersion < (int)Android.OS.BuildVersionCodes.Froyo) detector = new EclairGestureDetector(context);
            else detector = new FroyoGestureDetector(context);

            detector.SetOnGestureListener(listener);

            return detector;
        }
    }
}