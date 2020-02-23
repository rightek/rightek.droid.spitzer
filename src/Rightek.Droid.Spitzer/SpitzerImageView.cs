using Android.Content;
using Android.Util;
using Android.Widget;

using Rightek.Droid.Spitzer.Interfaces;

namespace Rightek.Droid.Spitzer
{
    public class SpitzerImageView : ImageView, ISpitzerImageView
    {
        SpitzerAttacher _attacher;
        ScaleType _pendingScaleType;

        public SpitzerImageView(Context context) : base(context, null, 0)
        {
            base.SetScaleType(ScaleType.Matrix);
            _attacher = new SpitzerAttacher(this);

            if (null != _pendingScaleType)
            {
                SetScaleType(_pendingScaleType);
                _pendingScaleType = null;
            }
        }

        public SpitzerImageView(Context context, IAttributeSet attr) : base(context, attr, 0)
        {
            base.SetScaleType(ScaleType.Matrix);
            _attacher = new SpitzerAttacher(this);

            if (null != _pendingScaleType)
            {
                SetScaleType(_pendingScaleType);
                _pendingScaleType = null;
            }
        }

        public SpitzerImageView(Context context, IAttributeSet attr, int defStyle) : base(context, attr, defStyle)
        {
            base.SetScaleType(ScaleType.Matrix);
            _attacher = new SpitzerAttacher(this);

            if (null != _pendingScaleType)
            {
                SetScaleType(_pendingScaleType);
                _pendingScaleType = null;
            }
        }

        #region IPhotoView

        public bool CanZoom() => _attacher.CanZoom();

        public Android.Graphics.RectF GetDisplayRect() => _attacher.GetDisplayRect();

        public bool SetDisplayMatrix(Android.Graphics.Matrix finalMatrix) => _attacher.SetDisplayMatrix(finalMatrix);

        public Android.Graphics.Matrix GetDisplayMatrix() => _attacher.GetDrawMatrix();

        public float GetMinScale() => GetMinimumScale();

        public float GetMinimumScale() => _attacher.GetMinimumScale();

        public float GetMidScale() => GetMediumScale();

        public float GetMediumScale() => _attacher.GetMediumScale();

        public float GetMaxScale() => GetMaximumScale();

        public float GetMaximumScale() => _attacher.GetMaximumScale();

        public float GetScale() => _attacher.GetScale();

        public override ImageView.ScaleType GetScaleType() => _attacher.GetScaleType();

        public void SetAllowParentInterceptOnEdge(bool allow) => _attacher.SetAllowParentInterceptOnEdge(allow);

        public void SetMinScale(float minScale) => SetMinimumScale(minScale);

        public void SetMinimumScale(float minimumScale) => _attacher.SetMinimumScale(minimumScale);

        public void SetMidScale(float midScale) => SetMediumScale(midScale);

        public void SetMediumScale(float mediumScale) => _attacher.SetMediumScale(mediumScale);

        public void SetMaxScale(float maxScale) => SetMaximumScale(maxScale);

        public void SetMaximumScale(float maximumScale) => _attacher.SetMaximumScale(maximumScale);

        public void SetOnMatrixChangeListener(SpitzerAttacher.IOnMatrixChangedListener listener) => _attacher.SetOnMatrixChangeListener(listener);

        public override void SetOnLongClickListener(Android.Views.View.IOnLongClickListener listener) => _attacher.SetOnLongClickListener(listener);

        public void SetOnPhotoTapListener(SpitzerAttacher.IOnPhotoTapListener listener) => _attacher.SetOnPhotoTapListener(listener);

        public SpitzerAttacher.IOnPhotoTapListener GetOnPhotoTapListener() => _attacher.GetOnPhotoTapListener();

        public void SetOnViewTapListener(SpitzerAttacher.IOnViewTapListener listener) => _attacher.SetOnViewTapListener(listener);

        public void SetRotationTo(float rotationDegree) => _attacher.SetRotationTo(rotationDegree);

        public void SetRotationBy(float rotationDegree) => _attacher.SetRotationBy(rotationDegree);

        public SpitzerAttacher.IOnViewTapListener GetOnViewTapListener() => _attacher.GetOnViewTapListener();

        public void SetScale(float scale) => _attacher.SetScale(scale);

        public void SetScale(float scale, bool animate) => _attacher.SetScale(scale, animate);

        public void SetScale(float scale, float focalX, float focalY, bool animate) => _attacher.SetScale(scale, focalX, focalY, animate);

        public override void SetScaleType(ScaleType scaleType)
        {
            if (null != _attacher) _attacher.SetScaleType(scaleType);
            else _pendingScaleType = scaleType;
        }

        public void SetZoomable(bool zoomable) => _attacher.SetZoomable(zoomable);

        public void SetPhotoViewRotation(float rotationDegree) => _attacher.SetRotationTo(rotationDegree);

        public Android.Graphics.Bitmap GetVisibleRectangleBitmap() => _attacher.GetVisibleRectangleBitmap();

        public void SetZoomTransitionDuration(int milliseconds) => _attacher.SetZoomTransitionDuration(milliseconds);

        public ISpitzerImageView GetIPhotoViewImplementation() => _attacher;

        public void SetOnDoubleTapListener(Android.Views.GestureDetector.IOnDoubleTapListener newOnDoubleTapListener)
            => _attacher.SetOnDoubleTapListener(newOnDoubleTapListener);

        #endregion IPhotoView

        #region ImageView

        public override void SetImageDrawable(Android.Graphics.Drawables.Drawable drawable)
        {
            base.SetImageDrawable(drawable);
            if (null != _attacher)
            {
                _attacher.Update();
            }
        }

        public override void SetImageResource(int resId)
        {
            base.SetImageResource(resId);
            if (null != _attacher)
            {
                _attacher.Update();
            }
        }

        public override void SetImageURI(Android.Net.Uri uri)
        {
            base.SetImageURI(uri);
            if (null != _attacher)
            {
                _attacher.Update();
            }
        }

        protected override void OnDetachedFromWindow()
        {
            _attacher.Cleanup();
            base.OnDetachedFromWindow();
        }

        #endregion ImageView
    }
}