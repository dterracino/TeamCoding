﻿using TeamCoding.CredentialManagement;
using TeamCoding.Extensions;
using TeamCoding.VisualStudio.Models;

namespace TeamCoding.IdentityManagement
{
    /// <summary>
    /// Gets crendials from the windows credential manager for a given set of credential targets (tried in turn)
    /// </summary>
    public class CredentialManagerIdentityProvider : IIdentityProvider
    {
        private readonly UserIdentity Identity;
        public bool ShouldCache => true;
        public UserIdentity GetIdentity() => Identity;
        public CredentialManagerIdentityProvider(string[] credentialTargets)
        {
            Credential credential = null;
            foreach (var credentialTarget in credentialTargets)
            {
                credential = new Credential { Target = credentialTarget };
                if(credential.Load() && credential.Username != null)
                {
                    break;
                }
            }
            
            if(credential?.Username == null)
            {
                Identity = null;
                return;
            }

            Identity = new UserIdentity()
            {
                Id = LocalIDEModel.Id.Value,
                DisplayName = credential.Username,
                ImageUrl = UserIdentity.GetGravatarUrlFromEmail(credential.Username)
            };

            TeamCodingPackage.Current.IDEWrapper.InvokeAsync(async () =>
            {
                var oldDisplayName = Identity.DisplayName;
                try
                {
                    Identity.DisplayName = await UserIdentity.GetGravatarDisplayNameFromEmailAsync(credential.Username).HandleException();
                }
                catch { } // Swallow failures here since they're dealt with above
                if (oldDisplayName != Identity.DisplayName)
                {
                    TeamCodingPackage.Current.LocalIdeModel.OnUserIdentityChanged();
                }
            });
        }

    }
}
