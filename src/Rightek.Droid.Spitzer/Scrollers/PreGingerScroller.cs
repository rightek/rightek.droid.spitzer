using Android.Content;
using Android.Widget;

namespace Rightek.Droid.Spitzer.Scrollers
{
    public class PreGingerScroller : Scroller
    {
        readonly Android.Widget.Scroller _scroller;

        public PreGingerScroller(Context context)
        {
            _scroller = new Android.Widget.Scroller(context);
        }

        #region ScrollerProxy

        public override bool ComputeScrollOffset() => _scroller.ComputeScrollOffset();

        public override void Fling(int startX, int startY, int velocityX, int velocityY, int minX, int maxX, int minY, int maxY, int overX, int overY)
            => _scroller.Fling(startX, startY, velocityX, velocityY, minX, maxX, minY, maxY);

        public override void ForceFinished(bool finished) => _scroller.ForceFinished(finished);

        public override bool IsFinished() => _scroller.IsFinished;

        public override int GetCurrX() => _scroller.CurrX;

        public override int GetCurrY() => _scroller.CurrY;

        #endregion ScrollerProxy
    }
}