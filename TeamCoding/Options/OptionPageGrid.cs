﻿using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TeamCoding.Options
{ // TODO: Figure out some way of getting (some) settings from a local file that could be synced from a repository. Maybe take them as defaults (and a way of resetting to default)
    [Guid(Guids.OptionPageGridGuidString)]
    public class OptionPageGrid : UIElementDialogPage
    {
        public string Username { get; set; } = UserSettings.DefaultUsername;
        public string UserImageUrl { get; set; } = UserSettings.DefaultImageUrl;
        public string FileBasedPersisterPath { get; set; } = SharedSettings.DefaultFileBasedPersisterPath;
        public string RedisServer { get; set; } = SharedSettings.DefaultRedisServer;
        private OptionsPage OptionsPage;
        protected override UIElement Child { get { return OptionsPage ?? (OptionsPage = new OptionsPage(this)); } }
        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);
            TeamCodingPackage.Current.Settings.Update(this);
        }
    }
}