using Android.Content;
using Android.Views;

using Java.Lang;

namespace Rightek.Droid.Spitzer.Gestures
{
    public class FroyoGestureDetector : EclairGestureDetector
    {
        protected readonly ScaleGestureDetector Detector;

        public FroyoGestureDetector(Context context) : base(context)
        {
            OnScaleGestureListener mScaleListener = new OnScaleGestureListener(this);
            Detector = new ScaleGestureDetector(context, mScaleListener);
        }

        public override bool IsScaling() => Detector.IsInProgress;

        public override bool OnTouchEvent(MotionEvent e)
        {
            Detector.OnTouchEvent(e);
            return base.OnTouchEvent(e);
        }

        private class OnScaleGestureListener : Java.Lang.Object, Android.Views.ScaleGestureDetector.IOnScaleGestureListener
        {
            FroyoGestureDetector froyoGestureDetector;

            public OnScaleGestureListener(FroyoGestureDetector froyoGestureDetector)
            {
                this.froyoGestureDetector = froyoGestureDetector;
            }

            #region IOnScaleGestureListener

            public bool OnScale(ScaleGestureDetector detector)
            {
                float scaleFactor = detector.ScaleFactor;

                if (Float.InvokeIsNaN(scaleFactor) || Float.InvokeIsInfinite(scaleFactor)) return false;

                froyoGestureDetector.Listener.OnScale(scaleFactor, detector.FocusX, detector.FocusY);

                return true;
            }

            public bool OnScaleBegin(ScaleGestureDetector detector) => true;

            public void OnScaleEnd(ScaleGestureDetector detector) { }

            #endregion IOnScaleGestureListener
        }
    }
}