using System;
using Android.Content;
using Android.Views;

namespace Rightek.Droid.Spitzer.Gestures
{
    public class EclairGestureDetector : CupcakeGestureDetector
    {
        static readonly int INVALID_POINTER_ID = -1;
        int _activePointerId = INVALID_POINTER_ID;
        int _activePointerIndex = 0;

        public EclairGestureDetector(Context context) : base(context) { }

        public override float GetActiveX(MotionEvent e)
        {
            try
            {
                return e.GetX(_activePointerIndex);
            }
            catch
            {
                return e.GetX();
            }
        }

        public override float GetActiveY(MotionEvent e)
        {
            try
            {
                return e.GetY(_activePointerIndex);
            }
            catch
            {
                return e.GetY();
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            MotionEventActions action = e.Action;
            switch (action & MotionEventActions.Mask)
            {
                case MotionEventActions.Down:
                    _activePointerId = e.GetPointerId(0);
                    break;

                case MotionEventActions.Cancel:
                case MotionEventActions.Up:
                    _activePointerId = INVALID_POINTER_ID;
                    break;

                case MotionEventActions.PointerUp:
                    // Ignore deprecation, ACTION_POINTER_ID_MASK and
                    // ACTION_POINTER_ID_SHIFT has same value and are deprecated
                    // You can have either deprecation or lint target api warning
                    int pointerIndex = Compat.GetPointerIndex(e.Action);
                    int pointerId = e.GetPointerId(pointerIndex);
                    if (pointerId == _activePointerId)
                    {
                        // This was our active pointer going up. Choose a new
                        // active pointer and adjust accordingly.
                        int newPointerIndex = pointerIndex == 0 ? 1 : 0;
                        _activePointerId = e.GetPointerId(newPointerIndex);
                        LastTouchX = e.GetX(newPointerIndex);
                        LastTouchY = e.GetY(newPointerIndex);
                    }
                    break;
            }

            _activePointerIndex = e.FindPointerIndex(_activePointerId != INVALID_POINTER_ID ? _activePointerId : 0);

            return base.OnTouchEvent(e);
        }
    }
}