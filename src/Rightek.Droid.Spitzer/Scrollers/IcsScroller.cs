using Android.Content;

namespace Rightek.Droid.Spitzer.Scrollers
{
    public class IcsScroller : GingerScroller
    {
        public IcsScroller(Context context) : base(context) { }

        public override bool ComputeScrollOffset() => OverScroller.ComputeScrollOffset();
    }
}