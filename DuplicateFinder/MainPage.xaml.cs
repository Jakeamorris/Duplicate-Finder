using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace DuplicateFinder
{
    public sealed partial class MainPage : Page
    {
        public DuplicateLogic Logic;

        public MainPage()
        {
            this.InitializeComponent();
            this.Logic = new DuplicateLogic();
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            Pick_Folder();
        }

        private async void Pick_Folder()
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                FutureAccessList.AddOrReplace("PickedFolderToken", folder);

                //Begin spinner fadein
                StatusFadeOutStoryboard.Begin();
                Progress.IsActive = true;
                ProgressNumberFadeInStoryboard.Begin();

                //Stopwatch for measuring time taken
                var watch = System.Diagnostics.Stopwatch.StartNew();                                            //remove

                //Wait for duplicate logic to complete
                await Logic.AssignTasks(folder);

                //Begin spinner fadeout
                ProgressFadeOutStoryboard.Begin();
                ProgressFadeOutStoryboard.Completed += ProgressFadeOutStoryboard_Completed;

                //Display time taken
                watch.Stop();                                                                                   //remove
                test.Text = watch.ElapsedMilliseconds.ToString();                                               //
            }
            else
            {
                StatusBlock.Text = "Something went wrong, please try again.";
                StatusFadeInStoryboard.Begin();
            }
        }

        private void SettingsIcon_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (SettingsTappedStoryboard.GetCurrentState() != ClockState.Active)
            {
                SettingsTappedStoryboard.Stop();
                SettingsHoverStoryboard.Stop();
                
                SettingsHoverStoryboard.Begin();
            }
        }

        private void SettingsIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SettingsTappedStoryboard.Stop();
            SettingsTappedStoryboard.Begin();

            //SettingsOverlay.Visibility = Visibility.Visible;
        }


        private void ProgressFadeOutStoryboard_Completed(object sender, object e)
        {
            Progress.IsActive = false;
            Progress.Opacity = 1.0;

            ProgressNumber.Text = "0%";
        }
    }
}
