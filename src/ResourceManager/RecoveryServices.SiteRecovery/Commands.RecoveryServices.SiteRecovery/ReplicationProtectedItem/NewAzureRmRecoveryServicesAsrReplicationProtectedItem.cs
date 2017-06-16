// ----------------------------------------------------------------------------------
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

using System;
using System.Management.Automation;
using Microsoft.Azure.Commands.RecoveryServices.SiteRecovery.Properties;
using Microsoft.Azure.Management.RecoveryServices.SiteRecovery.Models;
using Job = Microsoft.Azure.Management.RecoveryServices.SiteRecovery.Models.Job;

namespace Microsoft.Azure.Commands.RecoveryServices.SiteRecovery
{
    /// <summary>
    ///     Set Protection Entity protection state.
    /// </summary>
    [Cmdlet(VerbsCommon.New,
        "AzureRmRecoveryServicesAsrReplicationProtectedItem",
        DefaultParameterSetName = ASRParameterSets.DisableDR,
        SupportsShouldProcess = true)]
    [Alias("New-ASRReplicationProtectedItem")]
    [OutputType(typeof(ASRJob))]
    public class NewAzureRmRecoveryServicesAsrReplicationProtectedItem : SiteRecoveryCmdletBase
    {
        /// <summary>
        ///     Gets or sets Replication Protected Item.
        /// </summary>
        [Parameter(ParameterSetName = ASRParameterSets.EnterpriseToEnterprise,
            Mandatory = true,
            ValueFromPipeline = true)]
        [Parameter(ParameterSetName = ASRParameterSets.EnterpriseToAzure,
            Mandatory = true,
            ValueFromPipeline = true)]
        [Parameter(ParameterSetName = ASRParameterSets.HyperVSiteToAzure,
            Mandatory = true,
            ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public ASRProtectableItem ProtectableItem { get; set; }

        /// <summary>
        ///     Gets or sets Replication Protected Item Name.
        /// </summary>
        [Parameter(ParameterSetName = ASRParameterSets.EnterpriseToEnterprise,
            Mandatory = true)]
        [Parameter(ParameterSetName = ASRParameterSets.EnterpriseToAzure,
            Mandatory = true)]
        [Parameter(ParameterSetName = ASRParameterSets.HyperVSiteToAzure,
            Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets Policy.
        /// </summary>
        [Parameter(ParameterSetName = ASRParameterSets.EnterpriseToEnterprise,
            Mandatory = true)]
        [Parameter(ParameterSetName = ASRParameterSets.EnterpriseToAzure,
            Mandatory = true)]
        [Parameter(ParameterSetName = ASRParameterSets.HyperVSiteToAzure,
            Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public ASRProtectionContainerMapping ProtectionContainerMapping { get; set; }

        /// <summary>
        ///     Gets or sets Recovery Azure Storage Account Name of the Policy for E2A and B2A scenarios.
        /// </summary>
        [Parameter(ParameterSetName = ASRParameterSets.EnterpriseToAzure,
            Mandatory = true)]
        [Parameter(ParameterSetName = ASRParameterSets.HyperVSiteToAzure,
            Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string RecoveryAzureStorageAccountId { get; set; }

        /// <summary>
        ///     Gets or sets OS disk name.
        /// </summary>
        [Parameter(ParameterSetName = ASRParameterSets.HyperVSiteToAzure,
            Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string OSDiskName { get; set; }

        /// <summary>
        ///     Gets or sets OS Type
        /// </summary>
        [Parameter(ParameterSetName = ASRParameterSets.HyperVSiteToAzure,
            Mandatory = true)]
        [ValidateNotNullOrEmpty]
        [ValidateSet(Constants.OSWindows,
            Constants.OSLinux)]
        public string OS { get; set; }

        /// <summary>
        ///     Gets or sets Recovery Resource Group Id.
        /// </summary>
        [Parameter(ParameterSetName = ASRParameterSets.EnterpriseToAzure,
            Mandatory = true)]
        [Parameter(ParameterSetName = ASRParameterSets.HyperVSiteToAzure,
            Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string RecoveryResourceGroupId { get; set; }

        /// <summary>
        ///     Gets or sets switch parameter. On passing, command waits till completion.
        /// </summary>
        [Parameter]
        public SwitchParameter WaitForCompletion { get; set; }

        private Job jobResponse;

        /// <summary>
        ///     Job response.
        /// </summary>
        private PSSiteRecoveryLongRunningOperation response;

        /// <summary>
        ///     ProcessRecord of the command.
        /// </summary>
        public override void ExecuteSiteRecoveryCmdlet()
        {
            base.ExecuteSiteRecoveryCmdlet();

            var policy = RecoveryServicesClient.GetAzureSiteRecoveryPolicy(
                Utilities.GetValueFromArmId(ProtectionContainerMapping.PolicyId,
                    ARMResourceTypeConstants.ReplicationPolicies));
            var policyInstanceType = policy.Properties.ProviderSpecificDetails;

            switch (ParameterSetName)
            {
                case ASRParameterSets.EnterpriseToEnterprise:
                    if (!(policyInstanceType is HyperVReplicaPolicyDetails) &&
                        !(policyInstanceType is HyperVReplicaBluePolicyDetails))
                    {
                        throw new PSArgumentException(string.Format(
                            Resources.ContainerMappingParameterSetMismatch,
                            ProtectionContainerMapping.Name,
                            policyInstanceType));
                    }

                    break;

                case ASRParameterSets.EnterpriseToAzure:
                case ASRParameterSets.HyperVSiteToAzure:
                    if (!(policyInstanceType is HyperVReplicaAzurePolicyDetails))
                    {
                        throw new PSArgumentException(string.Format(
                            Resources.ContainerMappingParameterSetMismatch,
                            ProtectionContainerMapping.Name,
                            policyInstanceType));
                    }

                    break;

                default:
                    break;
            }

            var enableProtectionProviderSpecificInput = new EnableProtectionProviderSpecificInput();
            var inputProperties = new EnableProtectionInputProperties
            {
                PolicyId = ProtectionContainerMapping.PolicyId,
                ProtectableItemId = ProtectableItem.ID,
                ProviderSpecificDetails = enableProtectionProviderSpecificInput
            };

            var input = new EnableProtectionInput {Properties = inputProperties};

            // E2A and B2A.
            if (0 ==
                string.Compare(ParameterSetName,
                    ASRParameterSets.EnterpriseToAzure,
                    StringComparison.OrdinalIgnoreCase) ||
                0 ==
                string.Compare(ParameterSetName,
                    ASRParameterSets.HyperVSiteToAzure,
                    StringComparison.OrdinalIgnoreCase))
            {
                var providerSettings = new HyperVReplicaAzureEnableProtectionInput();
                providerSettings.HvHostVmId = ProtectableItem.FabricObjectId;
                providerSettings.VmName = ProtectableItem.FriendlyName;
                providerSettings.TargetAzureVmName = ProtectableItem.FriendlyName;

                // Id disk details are missing in input PE object, get the latest PE.
                if (string.IsNullOrEmpty(ProtectableItem.OS))
                {
                    // Just checked for OS to see whether the disk details got filled up or not
                    var protectableItemResponse =
                        RecoveryServicesClient.GetAzureSiteRecoveryProtectableItem(
                            Utilities.GetValueFromArmId(ProtectableItem.ID,
                                ARMResourceTypeConstants.ReplicationFabrics),
                            ProtectableItem.ProtectionContainerId,
                            ProtectableItem.Name);

                    ProtectableItem = new ASRProtectableItem(protectableItemResponse);
                }

                if (string.IsNullOrWhiteSpace(OS))
                {
                    providerSettings.OsType = string.Compare(ProtectableItem.OS,
                                                  Constants.OSWindows,
                                                  StringComparison.OrdinalIgnoreCase) ==
                                              0 ||
                                              string.Compare(ProtectableItem.OS,
                                                  Constants.OSLinux) ==
                                              0 ? ProtectableItem.OS : Constants.OSWindows;
                }
                else
                {
                    providerSettings.OsType = OS;
                }

                if (string.IsNullOrWhiteSpace(OSDiskName))
                {
                    providerSettings.VhdId = ProtectableItem.OSDiskId;
                }
                else
                {
                    foreach (var disk in ProtectableItem.Disks)
                    {
                        if (0 ==
                            string.Compare(disk.Name,
                                OSDiskName,
                                true))
                        {
                            providerSettings.VhdId = disk.Id;
                            break;
                        }
                    }
                }

                if (RecoveryAzureStorageAccountId != null)
                {
                    providerSettings.TargetStorageAccountId = RecoveryAzureStorageAccountId;
                }

                var deploymentType = Utilities.GetValueFromArmId(RecoveryAzureStorageAccountId,
                    ARMResourceTypeConstants.Providers);
                if (deploymentType.ToLower()
                    .Contains(Constants.Classic.ToLower()))
                {
                    providerSettings.TargetAzureV1ResourceGroupId = RecoveryResourceGroupId;
                    providerSettings.TargetAzureV2ResourceGroupId = null;
                }
                else
                {
                    providerSettings.TargetAzureV1ResourceGroupId = null;
                    providerSettings.TargetAzureV2ResourceGroupId = RecoveryResourceGroupId;
                }

                input.Properties.ProviderSpecificDetails = providerSettings;
            }

            response = RecoveryServicesClient.EnableProtection(Utilities.GetValueFromArmId(
                    ProtectableItem.ID,
                    ARMResourceTypeConstants.ReplicationFabrics),
                Utilities.GetValueFromArmId(ProtectableItem.ID,
                    ARMResourceTypeConstants.ReplicationProtectionContainers),
                Name,
                input);

            jobResponse =
                RecoveryServicesClient.GetAzureSiteRecoveryJobDetails(PSRecoveryServicesClient
                    .GetJobIdFromReponseLocation(response.Location));

            WriteObject(new ASRJob(jobResponse));

            if (WaitForCompletion.IsPresent)
            {
                WaitForJobCompletion(jobResponse.Name);

                jobResponse =
                    RecoveryServicesClient.GetAzureSiteRecoveryJobDetails(PSRecoveryServicesClient
                        .GetJobIdFromReponseLocation(response.Location));

                WriteObject(new ASRJob(jobResponse));
            }
        }

        /// <summary>
        ///     Writes Job.
        /// </summary>
        /// <param name="job">JOB object</param>
        private void WriteJob(Job job)
        {
            WriteObject(new ASRJob(job));
        }
    }
}