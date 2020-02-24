# Rightek.Droid.Spitzer
Simple pinch-to-zoom ImageView library for Xamarin.Android

## Basic Usage

```cs
using Rightek.Droid.Spitzer;

public class MyAdapter : PagerAdapter
{
    SpitzerAttacher _attacher;
    
    // ...
    
    public override Java.Lang.Object InstantiateItem(ViewGroup container, int position)
    {
        var view = _layoutInflater.Inflate(Resource.Layout.row_zoom, container, false);
        var myImageView = view.FindViewById<ImageView>(Resource.Id.myImageView);
        
        // black magic
        _attacher = new SpitzerAttacher(myImageView);
    }
}
```

## Nuget [![nuget](https://img.shields.io/nuget/v/Rightek.Droid.Spitzer.svg?color=%23268bd2&style=flat-square)](https://www.nuget.org/packages/Rightek.Droid.Spitzer) [![stats](https://img.shields.io/nuget/dt/Rightek.Droid.Spitzer.svg?color=%2382b414&style=flat-square)](https://www.nuget.org/stats/packages/Rightek.Droid.Spitzer?groupby=Version)

`PM> Install-Package Rightek.Droid.Spitzer`

## License
MIT

---
Made with ♥ by people @ [Rightek](http://rightek.ir)
