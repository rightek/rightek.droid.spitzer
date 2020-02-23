using System;

using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;

using Java.Lang.Ref;

using Rightek.Droid.Spitzer.Gestures;
using Rightek.Droid.Spitzer.Interfaces;
using Rightek.Droid.Spitzer.Scrollers;

namespace Rightek.Droid.Spitzer
{
    public class SpitzerAttacher : Java.Lang.Object, ISpitzerImageView, View.IOnTouchListener, IOnGestureListener, ViewTreeObserver.IOnGlobalLayoutListener
    {
        public static readonly string LOG_TAG = "SpitzerAttacher";

        // let debug flag be dynamic, but still Proguard can be used to remove from
        // release builds
        readonly bool DEBUG = Log.IsLoggable(LOG_TAG, LogPriority.Debug);

        static readonly IInterpolator sInterpolator = new AccelerateDecelerateInterpolator();
        int ZOOM_DURATION = Constants.DEFAULT_ZOOM_DURATION;

        const int EDGE_NONE = -1;
        const int EDGE_LEFT = 0;
        const int EDGE_RIGHT = 1;
        const int EDGE_BOTH = 2;

        float _minScale = Constants.DEFAULT_MIN_SCALE;
        float _midScale = Constants.DEFAULT_MID_SCALE;
        float _maxScale = Constants.DEFAULT_MAX_SCALE;

        bool _allowParentInterceptOnEdge = true;

        Java.Lang.Ref.WeakReference _imageView;

        // Gesture Detectors
        GestureDetector _gestureDetector;

        IGestureDetector _scaleDragDetector;

        // These are set so we don't keep allocating them on the heap
        readonly Matrix _baseMatrix = new Matrix();

        readonly Matrix _drawMatrix = new Matrix();
        readonly Matrix _suppMatrix = new Matrix();
        readonly RectF _displayRect = new RectF();
        readonly float[] _matrixValues = new float[9];

        IOnMatrixChangedListener _matrixChangeListener;
        IOnPhotoTapListener _photoTapListener;
        IOnViewTapListener _viewTapListener;
        View.IOnLongClickListener _longClickListener;

        int _ivTop, _ivRight, _ivBottom, _ivLeft;
        FlingRunnable _currentFlingRunnable;
        int _scrollEdge = EDGE_BOTH;
        bool _zoomEnabled;
        ImageView.ScaleType _scaleType = ImageView.ScaleType.FitCenter;

        public SpitzerAttacher(ImageView imageView)
        {
            _imageView = new Java.Lang.Ref.WeakReference(imageView);
            imageView.DrawingCacheEnabled = true;
            imageView.SetOnTouchListener(this);

            ViewTreeObserver observer = imageView.ViewTreeObserver;
            if (null != observer) observer.AddOnGlobalLayoutListener(this);

            // Make sure we using MATRIX Scale Type
            setImageViewScaleTypeMatrix(imageView);

            if (imageView.IsInEditMode) return;

            // Create Gesture Detectors...
            _scaleDragDetector = VersionedGestureDetector.NewInstance(imageView.Context, this);
            _gestureDetector = new GestureDetector(imageView.Context, new MSimpleOnGestureListener(this));
            _gestureDetector.SetOnDoubleTapListener(new DoubleTapListener(this));
            SetZoomable(true);
        }

        public void Cleanup()
        {
            if (null == _imageView) return; // cleanup already done

            var imageView = (ImageView)(((Reference)_imageView).Get());

            if (null != imageView)
            {
                // Remove this as a global layout listener
                ViewTreeObserver observer = imageView.ViewTreeObserver;
                if (null != observer && observer.IsAlive) observer.RemoveGlobalOnLayoutListener(this);

                // Remove the ImageView's reference to this
                imageView.SetOnTouchListener(null);

                // make sure a pending fling runnable won't be run
                cancelFling();
            }

            if (null != _gestureDetector) _gestureDetector.SetOnDoubleTapListener(null);

            // Clear listeners too
            _matrixChangeListener = null;
            _photoTapListener = null;
            _viewTapListener = null;

            // Finally, clear ImageView
            _imageView = null;
        }

        public bool OnTouch(View v, MotionEvent e)
        {
            bool handled = false;

            if (_zoomEnabled && hasDrawable((ImageView)v))
            {
                IViewParent parent = v.Parent;
                switch (e.Action)
                {
                    case MotionEventActions.Down:
                        // First, disable the Parent from intercepting the touch
                        // event
                        if (null != parent) parent.RequestDisallowInterceptTouchEvent(true);
                        else Log.Info(LOG_TAG, "onTouch getParent() returned null");

                        // If we're flinging, and the user presses down, cancel
                        // fling
                        cancelFling();
                        break;

                    case MotionEventActions.Cancel:
                    case MotionEventActions.Up:
                        // If the user has zoomed less than min scale, zoom back
                        // to min scale
                        if (GetScale() < _minScale)
                        {
                            RectF rect = GetDisplayRect();
                            if (null != rect)
                            {
                                v.Post(new AnimatedZoomRunnable(this, GetScale(), _minScale, rect.CenterX(), rect.CenterY()));
                                handled = true;
                            }
                        }
                        break;
                }

                // Try the Scale/Drag detector
                if (null != _scaleDragDetector && _scaleDragDetector.OnTouchEvent(e)) handled = true;

                // Check to see if the user double tapped
                if (null != _gestureDetector && _gestureDetector.OnTouchEvent(e)) handled = true;
            }

            return handled;
        }

        public ImageView GetImageView()
        {
            ImageView imageView = null;

            if (null != _imageView) imageView = (ImageView)_imageView.Get();

            // If we don't have an ImageView, call cleanup()
            if (null == imageView)
            {
                Cleanup();
                Log.Info(LOG_TAG, "ImageView no longer exists. You should not use this SpitzerAttacher any more.");
            }

            return imageView;
        }

        public void SetImageViewMatrix(Matrix matrix)
        {
            var imageView = GetImageView();
            if (null != imageView)
            {
                checkImageViewScaleType();
                imageView.ImageMatrix = (matrix);

                // Call MatrixChangedListener if needed
                if (null != _matrixChangeListener)
                {
                    RectF displayRect = getDisplayRect(matrix);
                    if (null != displayRect) _matrixChangeListener.OnMatrixChanged(displayRect);
                }
            }
        }

        public Matrix GetDrawMatrix()
        {
            _drawMatrix.Set(_baseMatrix);
            _drawMatrix.PostConcat(_suppMatrix);
            return _drawMatrix;
        }

        public void Update()
        {
            var imageView = GetImageView();

            if (null != imageView)
            {
                if (_zoomEnabled)
                {
                    // Make sure we using MATRIX Scale Type
                    setImageViewScaleTypeMatrix(imageView);

                    // Update the base matrix using the current drawable
                    updateBaseMatrix(imageView.Drawable);
                }
                else
                {
                    // Reset the Matrix...
                    resetMatrix();
                }
            }
        }

        static void checkZoomLevels(float minZoom, float midZoom, float maxZoom)
        {
            if (minZoom >= midZoom) throw new Java.Lang.IllegalArgumentException("MinZoom has to be less than MidZoom");
            if (midZoom >= maxZoom) throw new Java.Lang.IllegalArgumentException("MidZoom has to be less than MaxZoom");
        }

        static bool hasDrawable(ImageView imageView) => null != imageView && null != imageView.Drawable;

        static bool isSupportedScaleType(ImageView.ScaleType scaleType)
        {
            if (null == scaleType) return false;

            if (scaleType.Name().Equals(ImageView.ScaleType.Matrix.Name())) throw new Java.Lang.IllegalArgumentException(scaleType.Name() + " is not supported in PhotoView");

            return true;
        }

        static void setImageViewScaleTypeMatrix(ImageView imageView)
        {
            if (null != imageView && !(imageView is ISpitzerImageView))
            {
                if (!ImageView.ScaleType.Matrix.Name().Equals(imageView.GetScaleType().Name()))
                {
                    imageView.SetScaleType(ImageView.ScaleType.Matrix);
                }
            }
        }

        void cancelFling()
        {
            if (null != _currentFlingRunnable)
            {
                _currentFlingRunnable.CancelFling();
                _currentFlingRunnable = null;
            }
        }

        bool checkMatrixBounds()
        {
            var imageView = GetImageView();
            if (null == imageView) return false;

            var rect = getDisplayRect(GetDrawMatrix());
            if (null == rect) return false;

            float height = rect.Height(), width = rect.Width();
            float deltaX = 0, deltaY = 0;

            int viewHeight = getImageViewHeight(imageView);

            if (height <= viewHeight)
            {
                if (_scaleType.Name().Equals(ImageView.ScaleType.FitStart.Name())) deltaY = -rect.Top;
                else if (ImageView.ScaleType.FitEnd.Equals(_scaleType.Name())) deltaY = viewHeight - height - rect.Top;
                else deltaY = (viewHeight - height) / 2 - rect.Top;
            }
            else if (rect.Top > 0) deltaY = -rect.Top;
            else if (rect.Bottom < viewHeight) deltaY = viewHeight - rect.Bottom;

            int viewWidth = getImageViewWidth(imageView);
            if (width <= viewWidth)
            {
                if (_scaleType.Name().Equals(ImageView.ScaleType.FitStart.Name())) deltaX = -rect.Left;
                else if (_scaleType.Name().Equals(ImageView.ScaleType.FitEnd.Name())) deltaX = viewWidth - width - rect.Left;
                else deltaX = (viewWidth - width) / 2 - rect.Left;

                _scrollEdge = EDGE_BOTH;
            }
            else if (rect.Left > 0)
            {
                _scrollEdge = EDGE_LEFT;
                deltaX = -rect.Left;
            }
            else if (rect.Right < viewWidth)
            {
                deltaX = viewWidth - rect.Right;
                _scrollEdge = EDGE_RIGHT;
            }
            else _scrollEdge = EDGE_NONE;

            // Finally actually translate the matrix
            _suppMatrix.PostTranslate(deltaX, deltaY);

            return true;
        }

        RectF getDisplayRect(Matrix matrix)
        {
            var imageView = GetImageView();

            if (null != imageView)
            {
                Android.Graphics.Drawables.Drawable d = imageView.Drawable;
                if (null != d)
                {
                    _displayRect.Set(0, 0, d.IntrinsicWidth, d.IntrinsicHeight);
                    matrix.MapRect(_displayRect);

                    return _displayRect;
                }
            }

            return null;
        }

        void checkAndDisplayMatrix()
        {
            if (checkMatrixBounds()) SetImageViewMatrix(GetDrawMatrix());
        }

        float getValue(Matrix matrix, int whichValue)
        {
            matrix.GetValues(_matrixValues);

            return _matrixValues[whichValue];
        }

        int getImageViewWidth(ImageView imageView) => null == imageView
            ? 0
            : imageView.Width - imageView.PaddingLeft - imageView.PaddingRight;

        int getImageViewHeight(ImageView imageView) => null == imageView
            ? 0
            : imageView.Height - imageView.PaddingTop - imageView.PaddingBottom;

        void updateBaseMatrix(Android.Graphics.Drawables.Drawable d)
        {
            var imageView = GetImageView();
            if (null == imageView || null == d) return;

            float viewWidth = getImageViewWidth(imageView);
            float viewHeight = getImageViewHeight(imageView);
            int drawableWidth = d.IntrinsicWidth;
            int drawableHeight = d.IntrinsicHeight;

            _baseMatrix.Reset();

            float widthScale = viewWidth / drawableWidth;
            float heightScale = viewHeight / drawableHeight;

            if (_scaleType == ImageView.ScaleType.Center)
            {
                _baseMatrix.PostTranslate((viewWidth - drawableWidth) / 2F, (viewHeight - drawableHeight) / 2F);
            }
            else if (_scaleType == ImageView.ScaleType.CenterCrop)
            {
                float scale = Math.Max(widthScale, heightScale);

                _baseMatrix.PostScale(scale, scale);
                _baseMatrix.PostTranslate((viewWidth - drawableWidth * scale) / 2F, (viewHeight - drawableHeight * scale) / 2F);
            }
            else if (_scaleType == ImageView.ScaleType.CenterInside)
            {
                float scale = Math.Min(1.0f, Math.Min(widthScale, heightScale));

                _baseMatrix.PostScale(scale, scale);
                _baseMatrix.PostTranslate((viewWidth - drawableWidth * scale) / 2F, (viewHeight - drawableHeight * scale) / 2F);
            }
            else
            {
                var tempSrc = new RectF(0, 0, drawableWidth, drawableHeight);
                var tempDst = new RectF(0, 0, viewWidth, viewHeight);

                if (_scaleType.Name().Equals(ImageView.ScaleType.FitCenter.Name())) _baseMatrix.SetRectToRect(tempSrc, tempDst, Matrix.ScaleToFit.Center);
                else if (_scaleType.Name().Equals(ImageView.ScaleType.FitStart.Name())) _baseMatrix.SetRectToRect(tempSrc, tempDst, Matrix.ScaleToFit.Start);
                else if (_scaleType.Name().Equals(ImageView.ScaleType.FitEnd.Name())) _baseMatrix.SetRectToRect(tempSrc, tempDst, Matrix.ScaleToFit.End);
                else if (_scaleType.Name().Equals(ImageView.ScaleType.FitXy.Name())) _baseMatrix.SetRectToRect(tempSrc, tempDst, Matrix.ScaleToFit.Fill);
            }

            resetMatrix();
        }

        void resetMatrix()
        {
            _suppMatrix.Reset();
            SetImageViewMatrix(GetDrawMatrix());
            checkMatrixBounds();
        }

        void checkImageViewScaleType()
        {
            var imageView = GetImageView();

            // PhotoView's getScaleType() will just divert to this.getScaleType() so only call if we're not attached to a PhotoView.
            if (null != imageView && !(imageView is ISpitzerImageView))
            {
                if (!ImageView.ScaleType.Matrix.Name().Equals(imageView.GetScaleType().Name()))
                {
                    throw new Java.Lang.IllegalStateException("The ImageView's ScaleType has been changed since attaching a SpitzerAttacher");
                }
            }
        }

        #region ISpitzerImageView

        public bool CanZoom() => _zoomEnabled;

        public RectF GetDisplayRect()
        {
            checkMatrixBounds();
            return getDisplayRect(GetDrawMatrix());
        }

        public bool SetDisplayMatrix(Matrix finalMatrix)
        {
            if (finalMatrix == null) throw new Java.Lang.IllegalArgumentException("Matrix cannot be null");

            var imageView = GetImageView();
            if (null == imageView) return false;

            if (null == imageView.Drawable) return false;

            _suppMatrix.Set(finalMatrix);
            SetImageViewMatrix(GetDrawMatrix());
            checkMatrixBounds();

            return true;
        }

        public Matrix GetDisplayMatrix() => new Matrix(GetDrawMatrix());

        public float GetMinScale() => GetMinimumScale();

        public float GetMinimumScale() => _minScale;

        public float GetMidScale() => GetMediumScale();

        public float GetMediumScale() => _midScale;

        public float GetMaxScale() => GetMaximumScale();

        public float GetMaximumScale() => _maxScale;

        public float GetScale()
            => FloatMath.Sqrt((float)Math.Pow(getValue(_suppMatrix, Matrix.MscaleX), 2) + (float)Math.Pow(getValue(_suppMatrix, Matrix.MskewY), 2));

        public ImageView.ScaleType GetScaleType() => _scaleType;

        public void SetAllowParentInterceptOnEdge(bool allow) => _allowParentInterceptOnEdge = allow;

        public void SetMinScale(float minScale) => SetMinimumScale(minScale);

        public void SetMinimumScale(float minimumScale)
        {
            checkZoomLevels(minimumScale, _midScale, _maxScale);
            _minScale = minimumScale;
        }

        public void SetMidScale(float midScale) => SetMediumScale(midScale);

        public void SetMediumScale(float mediumScale)
        {
            checkZoomLevels(_minScale, mediumScale, _maxScale);
            _midScale = mediumScale;
        }

        public void SetMaxScale(float maxScale) => SetMaximumScale(maxScale);

        public void SetMaximumScale(float maximumScale)
        {
            checkZoomLevels(_minScale, _midScale, maximumScale);
            _maxScale = maximumScale;
        }

        public void SetOnLongClickListener(View.IOnLongClickListener listener) => _longClickListener = listener;

        public void SetRotationTo(float degrees)
        {
            _suppMatrix.SetRotate(degrees % 360);
            checkAndDisplayMatrix();
        }

        public void SetRotationBy(float degrees)
        {
            _suppMatrix.PostRotate(degrees % 360);
            checkAndDisplayMatrix();
        }

        public void SetScale(float scale) => SetScale(scale, false);

        public void SetScale(float scale, bool animate)
        {
            var imageView = GetImageView();

            if (null != imageView) SetScale(scale, (imageView.Right) / 2, (imageView.Bottom) / 2, animate);
        }

        public void SetScale(float scale, float focalX, float focalY, bool animate)
        {
            var imageView = GetImageView();

            if (null != imageView)
            {
                // check to see if the scale is within bounds
                if (scale < _minScale || scale > _maxScale)
                {
                    Log.Info(LOG_TAG, "Scale must be within the range of minScale and maxScale");

                    return;
                }

                if (animate) imageView.Post(new AnimatedZoomRunnable(this, GetScale(), scale, focalX, focalY));
                else
                {
                    _suppMatrix.SetScale(scale, scale, focalX, focalY);
                    checkAndDisplayMatrix();
                }
            }
        }

        public void SetScaleType(ImageView.ScaleType scaleType)
        {
            if (isSupportedScaleType(scaleType) && scaleType != _scaleType)
            {
                _scaleType = scaleType;

                // Finally update
                Update();
            }
        }

        public void SetZoomable(bool zoomable)
        {
            _zoomEnabled = zoomable;
            Update();
        }

        public void SetPhotoViewRotation(float degrees)
        {
            _suppMatrix.SetRotate(degrees % 360);
            checkAndDisplayMatrix();
        }

        public Bitmap GetVisibleRectangleBitmap()
        {
            ImageView imageView = GetImageView();
            return imageView == null ? null : imageView.DrawingCache;
        }

        public void SetZoomTransitionDuration(int milliseconds)
        {
            if (milliseconds < 0) milliseconds = Constants.DEFAULT_ZOOM_DURATION;

            this.ZOOM_DURATION = milliseconds;
        }

        public ISpitzerImageView GetIPhotoViewImplementation() => this;

        public void SetOnMatrixChangeListener(IOnMatrixChangedListener listener) => _matrixChangeListener = listener;

        public void SetOnPhotoTapListener(IOnPhotoTapListener listener) => _photoTapListener = listener;

        public IOnPhotoTapListener GetOnPhotoTapListener() => _photoTapListener;

        public void SetOnViewTapListener(IOnViewTapListener listener) => _viewTapListener = listener;

        public IOnViewTapListener GetOnViewTapListener() => _viewTapListener;

        public void SetOnDoubleTapListener(GestureDetector.IOnDoubleTapListener newOnDoubleTapListener)
        {
            if (newOnDoubleTapListener != null) this._gestureDetector.SetOnDoubleTapListener(newOnDoubleTapListener);
            else this._gestureDetector.SetOnDoubleTapListener(new DoubleTapListener(this));
        }

        #endregion ISpitzerImageView

        #region IOnGestureListener

        public void OnDrag(float dx, float dy)
        {
            if (_scaleDragDetector.IsScaling()) return; // Do not drag if we are already scaling

            if (DEBUG) Log.Info(LOG_TAG, Java.Lang.String.Format("onDrag: dx: %.2f. dy: %.2f", dx, dy));

            var imageView = GetImageView();
            _suppMatrix.PostTranslate(dx, dy);
            checkAndDisplayMatrix();

            IViewParent parent = imageView.Parent;
            if (_allowParentInterceptOnEdge && !_scaleDragDetector.IsScaling())
            {
                if (_scrollEdge == EDGE_BOTH || (_scrollEdge == EDGE_LEFT && dx >= 1f) || (_scrollEdge == EDGE_RIGHT && dx <= -1f))
                {
                    if (null != parent) parent.RequestDisallowInterceptTouchEvent(false);
                }
            }
            else
            {
                if (null != parent) parent.RequestDisallowInterceptTouchEvent(true);
            }
        }

        public void OnFling(float startX, float startY, float velocityX, float velocityY)
        {
            if (DEBUG) Log.Info(LOG_TAG, "onFling. sX: " + startX + " sY: " + startY + " Vx: " + velocityX + " Vy: " + velocityY);

            var imageView = GetImageView();

            _currentFlingRunnable = new FlingRunnable(this, imageView.Context);
            _currentFlingRunnable.Fling(getImageViewWidth(imageView), getImageViewHeight(imageView), (int)velocityX, (int)velocityY);

            imageView.Post(_currentFlingRunnable);
        }

        public void OnScale(float scaleFactor, float focusX, float focusY)
        {
            if (DEBUG) Log.Info(LOG_TAG, Java.Lang.String.Format("onScale: scale: %.2f. fX: %.2f. fY: %.2f", scaleFactor, focusX, focusY));

            if (GetScale() < _maxScale || scaleFactor < 1f)
            {
                _suppMatrix.PostScale(scaleFactor, scaleFactor, focusX, focusY);
                checkAndDisplayMatrix();
            }
        }

        #endregion IOnGestureListener

        #region IOnGlobalLayoutListener

        public void OnGlobalLayout()
        {
            var imageView = GetImageView();

            if (null != imageView)
            {
                if (_zoomEnabled)
                {
                    int top = imageView.Top;
                    int right = imageView.Right;
                    int bottom = imageView.Bottom;
                    int left = imageView.Left;

                    // We need to check whether the ImageView's bounds have changed.
                    // This would be easier if we targeted API 11+ as we could just use
                    // View.OnLayoutChangeListener. Instead we have to replicate the
                    // work, keeping track of the ImageView's bounds and then checking
                    //if the values change.
                    if (top != _ivTop || bottom != _ivBottom || left != _ivLeft || right != _ivRight)
                    {
                        // Update our base matrix, as the bounds have changed
                        updateBaseMatrix(imageView.Drawable);

                        // Update values as something has changed
                        _ivTop = top;
                        _ivRight = right;
                        _ivBottom = bottom;
                        _ivLeft = left;
                    }
                }
                else
                {
                    updateBaseMatrix(imageView.Drawable);
                }
            }
        }

        #endregion IOnGlobalLayoutListener

        class FlingRunnable : Java.Lang.Object, Java.Lang.IRunnable
        {
            readonly Scrollers.Scroller _scroller;
            int _currentX, _currentY;
            SpitzerAttacher _attacher;

            public FlingRunnable(SpitzerAttacher SpitzerAttacher, Context context)
            {
                _scroller = Scrollers.Scroller.GetScroller(context);
                this._attacher = SpitzerAttacher;
            }

            public void CancelFling()
            {
                if (_attacher.DEBUG) Log.Info("SpitzerAttacher", "Cancel Fling");

                _scroller.ForceFinished(true);
            }

            public void Fling(int viewWidth, int viewHeight, int velocityX, int velocityY)
            {
                var rect = _attacher.GetDisplayRect();
                if (null == rect) return;

                int startX = (int)Math.Round(-rect.Left);
                int minX, maxX, minY, maxY;

                if (viewWidth < rect.Width())
                {
                    minX = 0;
                    maxX = (int)Math.Round(rect.Width() - viewWidth);
                }
                else minX = maxX = startX;

                int startY = (int)Math.Round(-rect.Top);
                if (viewHeight < rect.Height())
                {
                    minY = 0;
                    maxY = (int)Math.Round(rect.Height() - viewHeight);
                }
                else minY = maxY = startY;

                _currentX = startX;
                _currentY = startY;

                if (_attacher.DEBUG) Log.Info("SpitzerAttacher", "fling. StartX:" + startX + " StartY:" + startY + " MaxX:" + maxX + " MaxY:" + maxY);

                // If we actually can move, fling the scroller
                if (startX != maxX || startY != maxY) _scroller.Fling(startX, startY, velocityX, velocityY, minX, maxX, minY, maxY, 0, 0);
            }

            public void Run()
            {
                if (_scroller.IsFinished()) return; // remaining post that should not be handled

                var imageView = _attacher.GetImageView();

                if (null != imageView && _scroller.ComputeScrollOffset())
                {
                    int newX = _scroller.GetCurrX();
                    int newY = _scroller.GetCurrY();

                    if (_attacher.DEBUG) Log.Info(LOG_TAG, "fling run(). CurrentX:" + _currentX + " CurrentY:" + _currentY + " NewX:" + newX + " NewY:" + newY);

                    _attacher._suppMatrix.PostTranslate(_currentX - newX, _currentY - newY);
                    _attacher.SetImageViewMatrix(_attacher.GetDrawMatrix());
                    _currentX = newX;
                    _currentY = newY;

                    Compat.PostOnAnimation(imageView, this);
                }
            }
        }

        class MSimpleOnGestureListener : GestureDetector.SimpleOnGestureListener
        {
            SpitzerAttacher _attacher;

            public MSimpleOnGestureListener(SpitzerAttacher SpitzerAttacher)
            {
                this._attacher = SpitzerAttacher;
            }

            public override void OnLongPress(MotionEvent e)
            {
                if (null != _attacher._longClickListener) _attacher._longClickListener.OnLongClick(_attacher.GetImageView());
            }
        }

        class AnimatedZoomRunnable : Java.Lang.Object, Java.Lang.IRunnable
        {
            readonly float _focalX, _focalY;
            readonly long _startTime;
            readonly float _zoomStart, _zoomEnd;
            SpitzerAttacher _attacher;

            public AnimatedZoomRunnable(SpitzerAttacher SpitzerAttacher, float currentZoom, float targetZoom, float focalX, float focalY)
            {
                this._attacher = SpitzerAttacher;
                _focalX = focalX;
                _focalY = focalY;
                _startTime = Java.Lang.JavaSystem.CurrentTimeMillis();
                _zoomStart = currentZoom;
                _zoomEnd = targetZoom;
            }

            #region IRunnable

            public void Run()
            {
                var imageView = _attacher.GetImageView();
                if (imageView == null) return;

                float t = Interpolate();
                float scale = _zoomStart + t * (_zoomEnd - _zoomStart);
                float deltaScale = scale / _attacher.GetScale();

                _attacher._suppMatrix.PostScale(deltaScale, deltaScale, _focalX, _focalY);
                _attacher.checkAndDisplayMatrix();

                // We haven't hit our target scale yet, so post ourselves again
                if (t < 1f) Compat.PostOnAnimation(imageView, this);
            }

            #endregion IRunnable

            float Interpolate()
            {
                float t = 1f * (Java.Lang.JavaSystem.CurrentTimeMillis() - _startTime) / _attacher.ZOOM_DURATION;
                t = Math.Min(1f, t);
                t = sInterpolator.GetInterpolation(t);
                return t;
            }
        }

        public interface IOnMatrixChangedListener
        {
            // Callback for when the Matrix displaying the Drawable has changed. This could be because
            // the View's bounds have changed, or the user has zoomed.

            // rect: Rectangle displaying the Drawable's new bounds.

            void OnMatrixChanged(RectF rect);
        }

        public interface IOnPhotoTapListener
        {
            // A callback to receive where the user taps on a photo. You will only receive a callback if
            // the user taps on the actual photo, tapping on 'whitespace' will be ignored.

            // view: View the user tapped.
            // x: where the user tapped from the of the Drawable, as percentage of the Drawable width.
            // y: where the user tapped from the top of the Drawable, as percentage of the Drawable height.

            void OnPhotoTap(View view, float x, float y);
        }

        public interface IOnViewTapListener
        {
            // A callback to receive where the user taps on a ImageView. You will receive a callback if
            // the user taps anywhere on the view, tapping on 'whitespace' will not be ignored.

            // view: View the user tapped.
            // x: where the user tapped from the left of the View.
            // y: where the user tapped from the top of the View.

            void OnViewTap(View view, float x, float y);
        }
    }
}