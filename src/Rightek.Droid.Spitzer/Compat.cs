using Android.Views;

using Java.Lang;

namespace Rightek.Droid.Spitzer
{
    public class Compat
    {
        static readonly int SIXTY_FPS_INTERVAL = 1000 / 60;

        public static void PostOnAnimation(View view, IRunnable runnable)
        {
            if ((int)Android.OS.Build.VERSION.SdkInt >= (int)Android.OS.BuildVersionCodes.JellyBean) postOnAnimationJellyBean(view, runnable);
            else view.PostDelayed(runnable, SIXTY_FPS_INTERVAL);
        }

        static void postOnAnimationJellyBean(View view, IRunnable runnable) => view.PostOnAnimation(runnable);

        public static int GetPointerIndex(MotionEventActions action)
            => (int)Android.OS.Build.VERSION.SdkInt >= (int)Android.OS.BuildVersionCodes.Honeycomb
                ? getPointerIndexHoneyComb(action)
                : getPointerIndexEclair(action);

        static int getPointerIndexEclair(MotionEventActions action)
            => ((int)action & (int)MotionEventActions.PointerIdMask) >> (int)MotionEventActions.PointerIdShift;

        static int getPointerIndexHoneyComb(MotionEventActions action)
            => ((int)action & (int)MotionEventActions.PointerIndexMask) >> (int)MotionEventActions.PointerIndexShift;
    }
}