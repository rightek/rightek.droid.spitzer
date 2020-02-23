using Android.Content;
using Android.Widget;

namespace Rightek.Droid.Spitzer.Scrollers
{
    public class GingerScroller : Scroller
    {
        protected readonly OverScroller OverScroller;
        bool _firstScroll = false;

        public GingerScroller(Context context)
        {
            OverScroller = new OverScroller(context);
        }

        #region ScrollerProxy

        public override bool ComputeScrollOffset()
        {
            if (_firstScroll)
            {
                OverScroller.ComputeScrollOffset();
                _firstScroll = false;
            }
            return OverScroller.ComputeScrollOffset();
        }

        public override void Fling(int startX, int startY, int velocityX, int velocityY, int minX, int maxX, int minY, int maxY, int overX, int overY)
            => OverScroller.Fling(startX, startY, velocityX, velocityY, minX, maxX, minY, maxY, overX, overY);

        public override void ForceFinished(bool finished) => OverScroller.ForceFinished(finished);

        public override bool IsFinished() => OverScroller.IsFinished;

        public override int GetCurrX() => OverScroller.CurrX;

        public override int GetCurrY() => OverScroller.CurrY;

        #endregion ScrollerProxy
    }
}