﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TeamCoding.Documents;
using TeamCoding.IdentityManagement;
using TeamCoding.Extensions;

namespace TeamCoding.VisualStudio
{
    /// <summary>
    /// Maintains a cache of images of users
    /// </summary>
    public class UserImageCache // TODO: Maybe make redis also a user image caching option
    {
        private static readonly Brush BorderBrush = new SolidColorBrush(new Color() { ScA = 0.65f, ScR = 1.0f, ScG = 1.0f, ScB = 1.0f });
        private readonly Dictionary<string, ImageSource> UrlImages = new Dictionary<string, ImageSource>();
        private readonly IDEWrapper IdeWrapper;

        public UserImageCache(IDEWrapper ideWrapper)
        {
            IdeWrapper = ideWrapper;
        }
        public Panel CreateUserIdentityControl(UserIdentity userIdentity)
        {
            var firstLetter = userIdentity.Id[0];
            var grid = new Grid();
            Image imageControl;
            grid.Children.Add(new Viewbox()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.DownOnly,
                Child = new TextBlock()
                {
                    LineStackingStrategy = LineStackingStrategy.MaxHeight,
                    TextAlignment = TextAlignment.Center,
                    TextTrimming = TextTrimming.None,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            });
            grid.Children.Add(imageControl = new Image()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            });
            grid.Children.Add(new Border()
            {
                BorderBrush = BorderBrush,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            });

            if (userIdentity.ImageUrl != null)
            {
                IdeWrapper.InvokeAsync(async () =>
                {
                    try
                    {
                        var request = await TeamCodingPackage.Current.HttpClient.GetAsync(userIdentity.ImageUrl);
                        if (!request.IsSuccessStatusCode) return;
                        var imageStream = await request.Content.ReadAsStreamAsync();
                        imageControl.Source = UrlImages[userIdentity.ImageUrl] = BitmapFrame.Create(imageStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    }
                    catch (Exception ex) when (!System.Diagnostics.Debugger.IsAttached)
                    {
                        TeamCodingPackage.Current.Logger.WriteError(ex);
                    }
                });
            }
            return grid;
        }
        internal void SetUserControlProperties(Panel parentControl, SourceControlledDocumentData matchedRemoteDoc)
        {
            var textBlockControl = parentControl.FindChild<TextBlock>();
            var firstLetter = (matchedRemoteDoc.IdeUserIdentity.Id)[0];
            textBlockControl.Text = firstLetter.ToString();
            parentControl.Background = VisuallyDistinctColours.GetRandomColour(firstLetter);

            var textBlockFormattedText = textBlockControl.GetBoundingRect();
            if (textBlockFormattedText.Top >= 5)
            { // If we have a lot of blank space at the top of the up-most pixel of the rendered character (for lower case letters for example), move the text up
                textBlockControl.Margin = new Thickness(0, (-textBlockFormattedText.Top) / 2, 0, 0);
            }
            else
            {
                textBlockControl.Margin = new Thickness(0);
            }
            // TODO: Think of a better way of indicating the user is editing a document
            parentControl.ToolTip = (matchedRemoteDoc.IdeUserIdentity.DisplayName ?? matchedRemoteDoc.IdeUserIdentity.Id) + (matchedRemoteDoc.BeingEdited ? " [edited]" : string.Empty);

            if (matchedRemoteDoc.HasFocus)
            {
                parentControl.Children.OfType<Border>().Single().Visibility = Visibility.Visible;
            }
            else
            {
                parentControl.Children.OfType<Border>().Single().Visibility = Visibility.Hidden;
            }

            SetImageSource(parentControl, matchedRemoteDoc);
        }

        private void SetImageSource(Panel parentControl, SourceControlledDocumentData matchedRemoteDoc)
        {
            ImageSource imageSource = null;
            if (matchedRemoteDoc.IdeUserIdentity.ImageBytes != null)
            {
                using (var MS = new MemoryStream(matchedRemoteDoc.IdeUserIdentity.ImageBytes))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = MS;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    imageSource = bitmap;
                }
            }
            else if (matchedRemoteDoc.IdeUserIdentity.ImageUrl != null)
            {
                if (UrlImages.ContainsKey(matchedRemoteDoc.IdeUserIdentity.ImageUrl))
                {
                    imageSource = UrlImages[matchedRemoteDoc.IdeUserIdentity.ImageUrl];
                }
            }

            parentControl.Children.OfType<Image>().Single().Source = imageSource;
        }

        /// <summary>
        /// Load a resource WPF-BitmapImage (png, bmp, ...) from embedded resource defined as 'Resource' not as 'Embedded resource'.
        /// </summary>
        /// <param name="pathInApplication">Path without starting slash</param>
        /// <param name="assembly">Usually 'Assembly.GetExecutingAssembly()'. If not mentionned, I will use the calling assembly</param>
        /// <returns></returns>
        private static BitmapImage LoadBitmapFromResource(string pathInApplication)
        { // http://stackoverflow.com/a/9737958
            if (pathInApplication[0] == '/')
            {
                pathInApplication = pathInApplication.Substring(1);
            }
            return new BitmapImage(new Uri(@"pack://application:,,,/" + System.Reflection.Assembly.GetCallingAssembly().GetName().Name + ";component/" + pathInApplication, UriKind.Absolute));
        }
    }
}
