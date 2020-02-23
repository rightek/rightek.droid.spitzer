using System;

using Android.Content;
using Android.Util;
using Android.Views;
using Rightek.Droid.Spitzer.Interfaces;

namespace Rightek.Droid.Spitzer.Gestures
{
    public class CupcakeGestureDetector : IGestureDetector
    {
        public float LastTouchX;
        public float LastTouchY;
        public readonly float TouchSlop;
        public readonly float MinimumVelocity;

        protected IOnGestureListener Listener;

        static readonly string LOG_TAG = "CupcakeGestureDetector";
        VelocityTracker _velocityTracker;
        bool _isDragging;

        public void SetOnGestureListener(IOnGestureListener listener)
        {
            this.Listener = listener;
        }

        public CupcakeGestureDetector(Context context)
        {
            var configuration = ViewConfiguration.Get(context);

            MinimumVelocity = configuration.ScaledMinimumFlingVelocity;
            TouchSlop = configuration.ScaledTouchSlop;
        }

        public virtual float GetActiveX(MotionEvent ev) => ev.GetX();

        public virtual float GetActiveY(MotionEvent ev) => ev.GetY();

        #region IGestureDetector implementation

        public virtual bool OnTouchEvent(Android.Views.MotionEvent ev)
        {
            switch (ev.Action)
            {
                case MotionEventActions.Down:
                    {
                        _velocityTracker = VelocityTracker.Obtain();
                        if (null != _velocityTracker)
                        {
                            _velocityTracker.AddMovement(ev);
                        }
                        else
                        {
                            Log.Info(LOG_TAG, "Velocity tracker is null");
                        }

                        LastTouchX = GetActiveX(ev);
                        LastTouchY = GetActiveY(ev);
                        _isDragging = false;
                        break;
                    }

                case MotionEventActions.Move:
                    {
                        float x = GetActiveX(ev);
                        float y = GetActiveY(ev);
                        float dx = x - LastTouchX, dy = y - LastTouchY;

                        if (!_isDragging)
                        {
                            // Use Pythagoras to see if drag length is larger than
                            // touch slop
                            _isDragging = FloatMath.Sqrt((dx * dx) + (dy * dy)) >= TouchSlop;
                        }

                        if (_isDragging)
                        {
                            Listener.OnDrag(dx, dy);
                            LastTouchX = x;
                            LastTouchY = y;

                            if (null != _velocityTracker)
                            {
                                _velocityTracker.AddMovement(ev);
                            }
                        }
                        break;
                    }

                case MotionEventActions.Cancel:
                    {
                        // Recycle Velocity Tracker
                        if (null != _velocityTracker)
                        {
                            _velocityTracker.Recycle();
                            _velocityTracker = null;
                        }
                        break;
                    }

                case MotionEventActions.Up:
                    {
                        if (_isDragging)
                        {
                            if (null != _velocityTracker)
                            {
                                LastTouchX = GetActiveX(ev);
                                LastTouchY = GetActiveY(ev);

                                // Compute velocity within the last 1000ms
                                _velocityTracker.AddMovement(ev);
                                _velocityTracker.ComputeCurrentVelocity(1000);

                                float vX = _velocityTracker.GetXVelocity(0), vY = _velocityTracker.GetYVelocity(0);

                                // If the velocity is greater than minVelocity, call
                                // listener
                                if (Math.Max(Math.Abs(vX), Math.Abs(vY)) >= MinimumVelocity)
                                {
                                    Listener.OnFling(LastTouchX, LastTouchY, -vX, -vY);
                                }
                            }
                        }

                        // Recycle Velocity Tracker
                        if (null != _velocityTracker)
                        {
                            _velocityTracker.Recycle();
                            _velocityTracker = null;
                        }
                        break;
                    }
            }

            return true;
        }

        public virtual bool IsScaling() => false;

        #endregion IGestureDetector implementation
    }
}