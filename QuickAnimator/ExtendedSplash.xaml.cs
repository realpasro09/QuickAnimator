using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace QuickAnimator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ExtendedSplash : Page
    {
        public ExtendedSplash()
        {
            this.InitializeComponent();
        }

        public ExtendedSplash(SplashScreen splash)
         {
            InitializeComponent();
            extendedSplashImage.SetValue(Canvas.LeftProperty, splash.ImageLocation.X);
            extendedSplashImage.SetValue(Canvas.TopProperty, splash.ImageLocation.Y);
            extendedSplashImage.Height = splash.ImageLocation.Height;
            extendedSplashImage.Width = splash.ImageLocation.Width;
            // Position the extended splash screen’s progress ring.
            ProgressRing.SetValue(Canvas.TopProperty, splash.ImageLocation.Y + splash.ImageLocation.Height + 32);
            ProgressRing.SetValue(Canvas.LeftProperty,
            splash.ImageLocation.X + ((splash.ImageLocation.Width / 2) - 15));
         }

        internal void onSplashScreenDismissed(Windows.ApplicationModel.Activation.SplashScreen sender, object e)
         {
         // The splash screen has been dismissed and the extended splash screen is now in view.
         }

        

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }
    }
}
