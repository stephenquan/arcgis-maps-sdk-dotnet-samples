// Copyright 2022 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using ArcGIS.Helpers;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;

namespace ArcGIS.Samples.OAuth
{
    [ArcGIS.Samples.Shared.Attributes.Sample(
        name: "Authenticate with OAuth",
        category: "Security",
        description: "Authenticate with ArcGIS Online (or your own portal) using OAuth2 to access secured resources (such as private web maps or layers).",
        instructions: "When you run the sample, the app will load a web map which contains premium content. You will be challenged for an ArcGIS Online login to view the private layers. Enter a user name and password for an ArcGIS Online named user account (such as your ArcGIS for Developers account). If you authenticate successfully, the traffic layer will display, otherwise the map will contain only the public basemap layer.",
        tags: new[] { "OAuth", "OAuth2", "authentication", "cloud", "credential", "portal", "security" })]
    [ArcGIS.Samples.Shared.Attributes.ClassFile("Helpers/ArcGISLoginPrompt.cs")]
    public partial class OAuth : ContentPage
    {
        // - The URL of the portal to authenticate with
        private const string ServerUrl = "https://www.arcgis.com/sharing/rest";

        // - The ID for a web map item hosted on the server (the ID below is for a traffic map of Paris).
        private const string WebMapId = "e5039444ef3c48b8a8fdc9227f9be7c1";

        public OAuth()
        {
            InitializeComponent();

            // Call a function to initialize the app and request a web map (with secured layers) to display.
            _ = Initialize();
        }

        private async Task Initialize()
        {
            try
            {
                // Set up the AuthenticationManager to use OAuth for secure ArcGIS Online requests.
                ArcGISLoginPrompt.SetChallengeHandler();

                // Connect to the portal (ArcGIS Online, for example).
                ArcGISPortal arcgisPortal = await ArcGISPortal.CreateAsync(new Uri(ServerUrl), loginRequired: true);

                // Issue: Exception raised when retrieving user entitlements via the multitenant www.arcgis.com URL
                //
                // After successful sign-in, calling GetLicenseInfoAsync() against https://www.arcgis.com/sharing/rest
                // raises the following ArcGISException:
                // Portal user with no license: Current user type does not include a Runtime license.
                //    at Esri.ArcGISRuntime.ArcGISException.HandleCoreError(CoreError error, Boolean throwException)
                //    at RuntimeCoreNet.GeneratedWrappers.Interop.CheckError(IntPtr errorHandle, Boolean throwOnFailure, GCHandle wrapperHandle)
                //    at RuntimeCoreNet.GeneratedWrappers.CoreTask.Get()
                //    at Esri.ArcGISRuntime.Internal.CoreTaskExtensions.TaskCompletedCallbackHandler`1.RuntimeCoreNet.GeneratedWrappers.ICoreCallback_Task_Completed.Completed()
                // --- End of stack trace from previous location ---
                //    at Esri.ArcGISRuntime.Portal.ArcGISPortal.< GetLicenseInfoAsync > d__41.MoveNext()
                //    at ArcGIS.Samples.OAuth.OAuth.< Initialize > d__3.MoveNext() in F:\Maui\arcgis - maps - sdk - dotnet - samples\src\MAUI\Maui.Samples\Samples\Security\OAuth\OAuth.xaml.cs:line 66
                //
                // Workaround:
                //
                // Use a single-tenant https://organization.maps.arcgis.com URL instead of https://www.arcgis.com.
                // The problem no longer occurs when using the organization-specific URL.
                var licenseInfo = await arcgisPortal.GetLicenseInfoAsync();

                // If successful, show the entitlements.
                await Application.Current.Windows[0].Page.DisplayAlertAsync("Entitlements check", licenseInfo.ToJson(), "OK");

                // Get a web map portal item using its ID.
                // If the item contains layers not shared publicly, the user will be challenged for credentials at this point.
                PortalItem portalItem = await PortalItem.CreateAsync(arcgisPortal, WebMapId);

                // Create a new map with the portal item and display it in the map view.
                // If authentication failed, only the public layers will be displayed.
                Map myMap = new Map(portalItem);
                MyMapView.Map = myMap;
            }
            catch (Exception e)
            {
                await Application.Current.Windows[0].Page.DisplayAlertAsync("Error", e.ToString(), "OK");
            }
        }
    }
}