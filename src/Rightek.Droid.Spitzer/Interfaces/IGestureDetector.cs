using Android.Views;

namespace Rightek.Droid.Spitzer.Interfaces
{
    public interface IGestureDetector
    {
        bool OnTouchEvent(MotionEvent ev);

        bool IsScaling();

        void SetOnGestureListener(IOnGestureListener listener);
    }
}