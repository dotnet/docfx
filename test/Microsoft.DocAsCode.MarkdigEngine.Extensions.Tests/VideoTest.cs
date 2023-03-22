// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.DocAsCode.MarkdigEngine.Tests;

public class VideoTest
{
    [Theory]
    [InlineData(@":::video source=""https://channel9.msdn.com/Shows/XamarinShow/Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin/player?nocookie=true"" title=""Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin"" max-width=""400"":::", @"<p><div class=""embeddedvideo"">
<iframe src=""https://channel9.msdn.com/Shows/XamarinShow/Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin/player?nocookie=true&amp;nocookie=true"" allowFullScreen=""true"" frameBorder=""0"" title=""Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin"" style=""max-width:400px;"">
</div></p>
")]
    [InlineData(@":::video source=""https://www.youtube.com/embed/wV11_nbT2XE"" title=""Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin"":::", @"<p><div class=""embeddedvideo"">
<iframe src=""https://www.youtube-nocookie.com/embed/wV11_nbT2XE"" allowFullScreen=""true"" frameBorder=""0"" title=""Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin"">
</div></p>
")]
    [InlineData(@":::video source=""https://www.youtube.com/embed/wV11_nbT2XE"" title=""Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin"":::

:::video source=""https://channel9.msdn.com/Shows/XamarinShow/Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin/player?nocookie=true"" title=""Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin"" max-width=""400"":::
", @"<p><div class=""embeddedvideo"">
<iframe src=""https://www.youtube-nocookie.com/embed/wV11_nbT2XE"" allowFullScreen=""true"" frameBorder=""0"" title=""Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin"">
</div></p>
<p><div class=""embeddedvideo"">
<iframe src=""https://channel9.msdn.com/Shows/XamarinShow/Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin/player?nocookie=true&amp;nocookie=true"" allowFullScreen=""true"" frameBorder=""0"" title=""Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin"" style=""max-width:400px;"">
</div></p>")]
    [InlineData(@":::video source=""https://www.microsoft.com/en-us/videoplayer/embed/wV11_nbT2XE"" title=""Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin"":::", @"<p><div class=""embeddedvideo"">
<iframe src=""https://www.microsoft.com/en-us/videoplayer/embed/wV11_nbT2XE"" allowFullScreen=""true"" frameBorder=""0"" title=""Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin"">
</div></p>")]
    public void VideoTestBlockGeneral(string source, string expected)
    {
        TestUtility.VerifyMarkup(source, expected);
    }
    
    [Theory]
    [InlineData(@":::video source="""" title=""My title"":::", @"<p>:::video source=&quot;&quot; title=&quot;My title&quot;:::</p>
")]
    [InlineData(@":::video title=""My title"":::", @"<p>:::video title=&quot;My title&quot;:::</p>
")]
    [InlineData(@":::video source=""https://videoland.msdn.com/Shows/XamarinShow/Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin/player?nocookie=true"" title=""Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin"" max-width=""400"":::", @"<p><div class=""embeddedvideo"">
<iframe src=""https://videoland.msdn.com/Shows/XamarinShow/Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin/player?nocookie=true"" allowFullScreen=""true"" frameBorder=""0"" title=""Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin"" style=""max-width:400px;"">
</div></p>
")]
    [InlineData(@":::video source=""https://channel9.msdn.com/Shows/XamarinShow/Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin/player?nocookie=true"" title=""Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin"" max-width=""what"":::", @"<p>:::video source=&quot;https://channel9.msdn.com/Shows/XamarinShow/Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin/player?nocookie=true&quot; title=&quot;Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin&quot; max-width=&quot;what&quot;:::</p>
")]
    [InlineData(@":::video blab=""blib"" source=""https://channel9.msdn.com/Shows/XamarinShow/Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin/player?nocookie=true"" title=""Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin"":::", @"<p>:::video blab=&quot;blib&quot; source=&quot;https://channel9.msdn.com/Shows/XamarinShow/Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin/player?nocookie=true&quot; title=&quot;Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin&quot;:::</p>
")]
    [InlineData(@":::video source=""https://channel9.msdn.com/Shows/XamarinShow/Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin/"" title=""Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin"":::", @"<p><div class=""embeddedvideo"">
<iframe src=""https://channel9.msdn.com/Shows/XamarinShow/Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin/?nocookie=true"" allowFullScreen=""true"" frameBorder=""0"" title=""Video: Build-Your-First-Android-App-with-Visual-Studio-2019-and-Xamarin"">
</div></p>
")]
    public void VideoTestBlock_InvalidVideo(string source, string expected)
    {
        TestUtility.VerifyMarkup(source, expected, errors: new[] { "invalid-video" });
    }

}
