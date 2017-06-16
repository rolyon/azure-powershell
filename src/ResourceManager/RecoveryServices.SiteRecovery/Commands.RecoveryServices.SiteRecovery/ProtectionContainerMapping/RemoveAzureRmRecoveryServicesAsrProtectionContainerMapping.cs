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

using System.Management.Automation;
using Microsoft.Azure.Management.RecoveryServices.SiteRecovery.Models;

namespace Microsoft.Azure.Commands.RecoveryServices.SiteRecovery
{
    /// <summary>
    ///     Adds Azure Site Recovery Policy settings to a Protection Container.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove,
        "AzureRmRecoveryServicesAsrProtectionContainerMapping",
        DefaultParameterSetName = ASRParameterSets.ByObject,
        SupportsShouldProcess = true)]
    [Alias("Remove-ASRProtectionContainerMapping")]
    [OutputType(typeof(ASRJob))]
    public class RemoveAzureRmRecoveryServicesAsrProtectionContainerMapping : SiteRecoveryCmdletBase
    {
        /// <summary>
        ///     Gets or sets Protection Container Mapping
        /// </summary>
        [Parameter(ParameterSetName = ASRParameterSets.ByObject,
            Mandatory = true,
            ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public ASRProtectionContainerMapping ProtectionContainerMapping { get; set; }

        /// <summary>
        ///     Gets or sets switch parameter. On passing, command does not ask for confirmation.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        ///     ProcessRecord of the command.
        /// </summary>
        public override void ExecuteSiteRecoveryCmdlet()
        {
            base.ExecuteSiteRecoveryCmdlet();

            if (ShouldProcess(ProtectionContainerMapping.Name,
                VerbsCommon.Remove))
            {
                PSSiteRecoveryLongRunningOperation response = null;

                if (!Force.IsPresent)
                {
                    var inputProperties = new RemoveProtectionContainerMappingInputProperties
                    {
                        ProviderSpecificInput = new ReplicationProviderContainerUnmappingInput()
                    };

                    var input =
                        new RemoveProtectionContainerMappingInput {Properties = inputProperties};

                    response = RecoveryServicesClient.UnConfigureProtection(
                        Utilities.GetValueFromArmId(ProtectionContainerMapping.ID,
                            ARMResourceTypeConstants.ReplicationFabrics),
                        Utilities.GetValueFromArmId(ProtectionContainerMapping.ID,
                            ARMResourceTypeConstants.ReplicationProtectionContainers),
                        ProtectionContainerMapping.Name,
                        input);
                }
                else
                {
                    response = RecoveryServicesClient.PurgeCloudMapping(Utilities.GetValueFromArmId(
                            ProtectionContainerMapping.ID,
                            ARMResourceTypeConstants.ReplicationFabrics),
                        Utilities.GetValueFromArmId(ProtectionContainerMapping.ID,
                            ARMResourceTypeConstants.ReplicationProtectionContainers),
                        ProtectionContainerMapping.Name);
                }

                var jobResponse =
                    RecoveryServicesClient.GetAzureSiteRecoveryJobDetails(PSRecoveryServicesClient
                        .GetJobIdFromReponseLocation(response.Location));

                WriteObject(new ASRJob(jobResponse));
            }
        }
    }
}