﻿using IWorkspaceUpdateManager = Microsoft.VisualStudio.Alm.Roslyn.Client.IVisualStudioIntegrationService;
using Microsoft.VisualStudio.CodeSense.Roslyn;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamCoding.VisualStudio.CodeLens
{
    [Export(typeof(ICodeLensDataPointProvider)), Name(CodeLensName)]
    public class CurrentUsersDataPointV15Provider : ICodeLensDataPointProvider
    {
        public const string CodeLensName = "Team Coding";
        [Import]
        private readonly CurrentUsersDataPointV15Updater DataPointUpdater = null;
        [Import]
        private readonly IWorkspaceUpdateManager WorkspaceUpdateManager = null;
        public bool CanCreateDataPoint(ICodeLensDescriptor descriptor)
        {
            return descriptor is ICodeElementDescriptor;
        }
        public ICodeLensDataPoint CreateDataPoint(ICodeLensDescriptor codeLensDescriptor)
        {
            return new CurrentUsersDataPointV15(DataPointUpdater, WorkspaceUpdateManager, (ICodeElementDescriptor)codeLensDescriptor);
        }
    }
}
