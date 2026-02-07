using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Resources
{
    public partial class Loader
    {
        [FileEmbed.FileEmbed("game-background.png")]
        public static partial ReadOnlySpan<byte> GetBackgroundImage();
    }
}
