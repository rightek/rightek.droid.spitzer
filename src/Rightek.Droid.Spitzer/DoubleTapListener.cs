namespace Rightek.Droid.Spitzer
{
    public class DoubleTapListener : Java.Lang.Object, Android.Views.GestureDetector.IOnDoubleTapListener
    {
        SpitzerAttacher _attacher;

        public DoubleTapListener(SpitzerAttacher attacher)
        {
            SetPhotoViewAttacher(attacher);
        }

        public void SetPhotoViewAttacher(SpitzerAttacher attacher)
        {
            _attacher = attacher;
        }

        #region IOnDoubleTapListener

        public bool OnDoubleTap(Android.Views.MotionEvent e)
        {
            if (_attacher == null) return false;

            try
            {
                float scale = _attacher.GetScale();
                float x = e.GetX();
                float y = e.GetY();

                if (scale < _attacher.GetMediumScale())
                {
                    _attacher.SetScale(_attacher.GetMediumScale(), x, y, true);
                }
                else if (scale >= _attacher.GetMediumScale() && scale < _attacher.GetMaximumScale())
                {
                    _attacher.SetScale(_attacher.GetMaximumScale(), x, y, true);
                }
                else
                {
                    _attacher.SetScale(_attacher.GetMinimumScale(), x, y, true);
                }
            }
            catch (Java.Lang.ArrayIndexOutOfBoundsException ex)
            {
                // Can sometimes happen when getX() and getY() is called
            }

            return true;
        }

        public bool OnDoubleTapEvent(Android.Views.MotionEvent e)
        {
            return false;
        }

        public bool OnSingleTapConfirmed(Android.Views.MotionEvent e)
        {
            if (this._attacher == null) return false;

            var imageView = _attacher.GetImageView();

            if (null != _attacher.GetOnPhotoTapListener())
            {
                var displayRect = _attacher.GetDisplayRect();

                if (null != displayRect)
                {
                    float x = e.GetX(), y = e.GetY();

                    if (displayRect.Contains(x, y))
                    {
                        float xResult = (x - displayRect.Left) / displayRect.Width();
                        float yResult = (y - displayRect.Top) / displayRect.Height();

                        _attacher.GetOnPhotoTapListener().OnPhotoTap(imageView, xResult, yResult);
                        return true;
                    }
                }
            }

            if (null != _attacher.GetOnViewTapListener())
            {
                _attacher.GetOnViewTapListener().OnViewTap(imageView, e.GetX(), e.GetY());
            }

            return false;
        }

        #endregion IOnDoubleTapListener
    }
}