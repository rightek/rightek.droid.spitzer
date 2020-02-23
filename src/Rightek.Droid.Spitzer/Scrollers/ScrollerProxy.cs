using Android.Content;

namespace Rightek.Droid.Spitzer.Scrollers
{
    public abstract class Scroller
    {
        public static Scroller GetScroller(Context context)
        {
            if ((int)Android.OS.Build.VERSION.SdkInt < (int)Android.OS.BuildVersionCodes.Gingerbread) return new PreGingerScroller(context);
            else if ((int)Android.OS.Build.VERSION.SdkInt < (int)Android.OS.BuildVersionCodes.IceCreamSandwich) return new GingerScroller(context);
            return new IcsScroller(context);
        }

        public abstract bool ComputeScrollOffset();

        public abstract void Fling(int startX, int startY, int velocityX, int velocityY, int minX, int maxX, int minY, int maxY, int overX, int overY);

        public abstract void ForceFinished(bool finished);

        public abstract bool IsFinished();

        public abstract int GetCurrX();

        public abstract int GetCurrY();
    }
}