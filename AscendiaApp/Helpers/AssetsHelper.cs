using Microsoft.UI.Xaml.Media;
using System;

namespace AscendiaApp.Helpers;

public static class AssetsHelper
{
    public static ImageSource GetPositionImageSource(string? position)
    {
        string source = position switch
        {
            "1" => "ms-appx:///Assets/Ladder/pos_safelane.png",
            "2" => "ms-appx:///Assets/Ladder/pos_midlane.png",
            "3" => "ms-appx:///Assets/Ladder/pos_offlane.png",
            "4" => "ms-appx:///Assets/Ladder/pos_softsupport.png",
            "5" => "ms-appx:///Assets/Ladder/pos_hardsupport.png",
            _ => "ms-appx:///Assets/Ladder/Pos_unknown.png",
        };
        return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(source));
    }

    public static ImageSource GetRankImageSource(int? rankTier)
    {
        var source = rankTier switch
        {
            80 => "ms-appx:///Assets/Ladder/rank8.png",
            >= 70 and <= 79 => "ms-appx:///Assets/Ladder/rank7.png",
            >= 60 and <= 69 => "ms-appx:///Assets/Ladder/rank6.png",
            >= 50 and <= 59 => "ms-appx:///Assets/Ladder/rank5.png",
            >= 40 and <= 49 => "ms-appx:///Assets/Ladder/rank4.png",
            >= 30 and <= 39 => "ms-appx:///Assets/Ladder/rank3.png",
            >= 20 and <= 29 => "ms-appx:///Assets/Ladder/rank2.png",
            >= 10 and <= 19 => "ms-appx:///Assets/Ladder/rank1.png",
            _ => "ms-appx:///Assets/Ladder/rank0.png",
        };
        return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(source));
    }
}