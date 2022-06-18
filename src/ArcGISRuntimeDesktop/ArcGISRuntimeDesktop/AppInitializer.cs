using Esri.ArcGISRuntime.Security;

namespace ArcGISRuntimeDesktop;

internal class AppInitializer
{
    public AppInitializer()
    {
    }

    public async Task Initialize(Window owner)
    {
        StatusText = "Initializing ArcGIS Runtime...";

        Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.Initialize();
        AuthenticationManager.Current.Persistence = await AppDataCredentialPersistence.CreateAsync();

        //Register server info for portal
        var settings = ApplicationViewModel.Instance.AppSettings;
        if (settings.OAuthClientId == "SET_CLIENT_ID" || settings.OAuthRedirectUrl == "SET_REDIRECT_URL")
        {
            // Application isn't configured. Please update the oauth settings by using the ArcGIS Developer Portal at
            // https://developers.arcgis.com/applications
            System.Diagnostics.Debugger.Break();
            throw new InvalidOperationException("Please configure your client id and redirect url in 'ApplicationConfiguration.xaml' to run this sample");
        }

        var redirectUrl = new Uri(settings.OAuthRedirectUrl);

        ServerInfo portalServerInfo = new ServerInfo(settings.PortalUrl)
        {
            TokenAuthenticationType = TokenAuthenticationType.OAuthAuthorizationCode,
            OAuthClientInfo = new OAuthClientInfo(settings.OAuthClientId, redirectUrl)
        };
        AuthenticationManager.Current.RegisterServer(portalServerInfo);

        Progress += 20;
        await Task.Delay(200);


        if (!string.IsNullOrEmpty(ApplicationViewModel.Instance.AppSettings.PortalUser)) // We have signed in before
        {
            StatusText = "Signing into ArcGIS Online...";
            try
            {
                // Do this without oauth handler - we want to fail if credential persistance was empty / or stored credentials no longer working
                ArcGISPortal arcgisPortal = await ArcGISPortal.CreateAsync(ApplicationViewModel.Instance.AppSettings.PortalUrl, true);
                ApplicationViewModel.Instance.PortalUser = arcgisPortal.User!;
            }
            catch (Exception)
            {
                AuthenticationManager.Current.RemoveAllCredentials(); // Start over
            }
        }

        AuthenticationManager.Current.ChallengeHandler = new DefaultChallengeHandler();
        AuthenticationManager.Current.OAuthAuthorizeHandler = OAuthAuthorizeHandler.Instance;

        StatusText = "Checking license...";
        var license = ApplicationViewModel.Instance.AppSettings.License;
        var licenseStatus = Esri.ArcGISRuntime.LicenseStatus.Invalid;
        if (license is not null)
        {
            try
            {
                var result = Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.SetLicense(license);
                licenseStatus = result.LicenseStatus;
            }
            catch { }
        }

        Progress += 20;
        await Task.Delay(200);

        if (ApplicationViewModel.Instance.PortalUser is null)
        {
            var signinWindow = new Windows.SignInWindow();
            signinWindow.Activate();
            WinUIEx.WindowExtensions.Hide(owner);
            try
            {
                var user = await signinWindow.SignInAsync();
                ApplicationViewModel.Instance.PortalUser = user;
                WinUIEx.WindowExtensions.Show(owner);
                WinUIEx.WindowExtensions.SetForegroundWindow(owner);
                signinWindow.Close();
            }
            catch (OperationCanceledException)
            {
                App.Current.Exit();
                return;
            }
        }

        if (licenseStatus != Esri.ArcGISRuntime.LicenseStatus.Valid)
        {
            // Refresh license from portal
            Progress += 20;
            StatusText = "Getting updated license...";
            license = await ApplicationViewModel.Instance.PortalUser!.Portal.GetLicenseInfoAsync();
            ApplicationViewModel.Instance.AppSettings.License = license;
            var result = Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.SetLicense(license);
            if (result.LicenseStatus != Esri.ArcGISRuntime.LicenseStatus.Valid)
                throw new NotSupportedException("Could not get a valid ArcGIS Runtime license from the portal. Portal returned: " + result.LicenseStatus);
        }
        StatusText = "Finishing up...";
        Progress = 80;
        await Task.Delay(200);
    }

    public event EventHandler<int>? ProgressChanged;

    public event EventHandler<string>? StatusTextChanged;

    private string m_text = "";

    public string StatusText
    {
        get { return m_text; }
        private set { m_text = value; StatusTextChanged?.Invoke(this, value); }
    }

    private int m_progress;

    public int Progress
    {
        get { return m_progress; }
        private set { m_progress = value; ProgressChanged?.Invoke(this, value); }
    }
}
