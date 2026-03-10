using System.Windows;
using System.Windows.Media.Animation;

namespace TankiLauncher
{
    public partial class App : Application
    {
        public static string CurrentUser;

        protected override void OnStartup(StartupEventArgs e)
        {
            Timeline.DesiredFrameRateProperty.OverrideMetadata(
                typeof(Timeline),
                new FrameworkPropertyMetadata
                {
                    DefaultValue = 120
                });

            base.OnStartup(e);
        }
    }
}