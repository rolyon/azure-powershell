﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using Microsoft.Azure.Commands.Common.Authentication;
using Microsoft.Azure.Commands.Common.Authentication.Abstractions;
using Microsoft.Azure.Commands.Profile.Common;
using Microsoft.Azure.Commands.ResourceManager.Common;
using Microsoft.WindowsAzure.Commands.Common;
using Newtonsoft.Json;
using System.IO;
using System.Management.Automation;
//using Microsoft.Identity.Client;
using Microsoft.Azure.Commands.Common.Authentication.Authentication.Clients;
using Azure.Identity;

namespace Microsoft.Azure.Commands.Profile.Context
{
    [Cmdlet("Disable", ResourceManager.Common.AzureRMConstants.AzureRMPrefix + "ContextAutosave", SupportsShouldProcess = true)]
    [OutputType(typeof(ContextAutosaveSettings))]
    public class DisableAzureRmContextAutosave : AzureContextModificationCmdlet
    {
        protected override bool RequireDefaultContext() { return false; }

        public override void ExecuteCmdlet()
        {
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Scope)) && Scope == ContextModificationScope.Process)
            {
                ConfirmAction("Do not autosave the context in the current session", "Current session", () =>
                {
                    ModifyContext((profile, client) =>
                    {
                        ContextAutosaveSettings settings = null;
                        AzureSession.Modify((session) => DisableAutosave(session, false, out settings));
                        ResourceManagerProfileProvider.InitializeResourceManagerProfile(true);
                        AzureRmProfileProvider.Instance.Profile = profile;
                        WriteObject(settings);
                    });
                });
            }
            else
            {
                ConfirmAction("Never autosave the context for the current user", "Current user",
                    () =>
                    {
                        ModifyContext((profile, client) =>
                        {
                            ContextAutosaveSettings settings = null;
                            AzureSession.Modify((session) => DisableAutosave(session, true, out settings));
                            ResourceManagerProfileProvider.InitializeResourceManagerProfile(true);
                            AzureRmProfileProvider.Instance.Profile = profile;
                            WriteObject(settings);
                        });
                    });
            }
        }

        void DisableAutosave(IAzureSession session, bool writeAutoSaveFile, out ContextAutosaveSettings result)
        {
            string tokenPath = Path.Combine(session.TokenCacheDirectory, session.TokenCacheFile);
            result = new ContextAutosaveSettings
            {
                Mode = ContextSaveMode.Process
            };

            FileUtilities.DataStore = session.DataStore;
            session.ARMContextSaveMode = ContextSaveMode.Process;

            MemoryStream memoryStream = null;
            var cacheProvider = new InMemoryTokenCacheProvider();
            if (AzureSession.Instance.TryGetComponent(
                    PowerShellTokenCacheProvider.PowerShellTokenCacheProviderKey,
                    out PowerShellTokenCacheProvider originalAuthenticationClientFactory))
            {
                var token = originalAuthenticationClientFactory.ReadTokenData();
                if (token != null && token.Length > 0)
                {
                    memoryStream = new MemoryStream(token);
                }
                cacheProvider.UpdateTokenDataWithoutFlush(token);
                cacheProvider.FlushTokenData();
            }
            AzureSession.Instance.UnregisterComponent<AuthenticationClientFactory>(PowerShellTokenCacheProvider.PowerShellTokenCacheProviderKey);
            AzureSession.Instance.RegisterComponent(PowerShellTokenCacheProvider.PowerShellTokenCacheProviderKey, () => cacheProvider);

            TokenCache newTokenCache = null;
            if(AzureSession.Instance.TryGetComponent(nameof(TokenCache), out TokenCache tokenCache))
            {
                if(tokenCache.GetType() == typeof(TokenCache))
                {
                    newTokenCache = tokenCache;
                }
                else
                {
                    //TODO: read token data from cache file directly
                    newTokenCache = memoryStream == null ? null : TokenCache.Deserialize(memoryStream);
                }
            }

            if(newTokenCache == null)
            {
                newTokenCache = new TokenCache();
            }
            AzureSession.Instance.RegisterComponent(nameof(TokenCache), () => newTokenCache, true);
            if(AzureSession.Instance.TryGetComponent(AuthenticatorBuilder.AuthenticatorBuilderKey, out IAuthenticatorBuilder builder))
            {
                builder.Reset();
            }

            if (writeAutoSaveFile)
            {
                FileUtilities.EnsureDirectoryExists(session.ProfileDirectory);
                string autoSavePath = Path.Combine(session.ProfileDirectory, ContextAutosaveSettings.AutoSaveSettingsFile);
                session.DataStore.WriteFile(autoSavePath, JsonConvert.SerializeObject(result));
            }
        }
    }
}
