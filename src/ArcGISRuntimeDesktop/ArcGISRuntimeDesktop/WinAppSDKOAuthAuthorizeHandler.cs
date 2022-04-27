using Windows.ApplicationModel.Activation;

namespace ArcGISRuntimeDesktop;

/// <summary>
/// Handles OAuth redirection to the system browser and re-activation.
/// </summary>
/// <remarks>
/// <para>
/// To use, in the App() constructor of your app add <c>OAuthAuthorizeHandler.OnAppCreation();</c> as the very first call.
/// This will ensure the correct instance gets reactivated after OAuth sign in has completed, and new instances won't
/// start running if the redirect was meant for a different instance.
/// </para>
/// <para>
/// Your app must be configured for OAuth. In you app package's <c>Package.appxmanifest</c> under Declartions, add a 
/// Protocol declaration and add the scheme you registered for your application's oauth redirect url under "Name".
/// </para>
/// <para>To use with ArcGIS Runtime's OAuth callback, assign the static instance to the handler:
/// <code lang="csharp">AuthenticationManager.Current.OAuthAuthorizeHandler = WinAppSDKOAuthAuthorizeHandler.Instance;</code>
/// </para>
/// </remarks>
public class WinAppSDKOAuthAuthorizeHandler : Esri.ArcGISRuntime.Security.IOAuthAuthorizeHandler
{
    public static WinAppSDKOAuthAuthorizeHandler Instance { get; } = new WinAppSDKOAuthAuthorizeHandler();

    private Dictionary<string, TaskCompletionSource<Uri>> tasks = new Dictionary<string, TaskCompletionSource<Uri>>();

    private WinAppSDKOAuthAuthorizeHandler()
    {
        Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().Activated += CurrentAppInstance_Activated;
    }

    private static System.Collections.Specialized.NameValueCollection? GetState(Microsoft.Windows.AppLifecycle.AppActivationArguments activatedEventArgs)
    {
        if (activatedEventArgs.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.Protocol &&
            activatedEventArgs.Data is IProtocolActivatedEventArgs protocolArgs)
        {
            var vals = System.Web.HttpUtility.ParseQueryString(protocolArgs.Uri.Query);
            if (vals["state"] is string state)
            {
                return System.Web.HttpUtility.ParseQueryString(state);
            }
        }
        return null;
    }

    /// <summary>
    /// Must be first thing called from App constructor
    /// </summary>
    /// <param name="app"></param>
    public static void OnAppCreation()
    {
        var activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
        var state = GetState(activatedEventArgs);
        if (state is not null && state["appInstanceId"] is string id)
        {
            var instance = Microsoft.Windows.AppLifecycle.AppInstance.GetInstances().Where(i => i.Key == id).FirstOrDefault();

            if (instance is not null && !instance.IsCurrent)
            {
                // Redirect to correct instance and close this one
                instance.RedirectActivationToAsync(activatedEventArgs).AsTask().Wait();
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
        }
        else
        {
            var thisInstance = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent();
            if (string.IsNullOrEmpty(thisInstance.Key))
            {
                Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey(Guid.NewGuid().ToString());
            }
        }
    }

    private void CurrentAppInstance_Activated(object? sender, Microsoft.Windows.AppLifecycle.AppActivationArguments e)
    {
        if (e.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.Protocol)
        {
            if (e.Data is IProtocolActivatedEventArgs protocolArgs)
            {
                var vals = System.Web.HttpUtility.ParseQueryString(protocolArgs.Uri.Query);
                if (vals["state"] is string state)
                {
                    vals = System.Web.HttpUtility.ParseQueryString(state);
                    if (vals["signinId"] is string signinId)
                    {
                        ResumeSignin(protocolArgs.Uri, signinId);
                    }
                }
            }
        }
    }

    private void ResumeSignin(Uri callbackUri, string signinId)
    {
        if (signinId != null && tasks.ContainsKey(signinId))
        {
            var task = tasks[signinId];
            tasks.Remove(signinId);
            task.TrySetResult(callbackUri);
        }
    }

    async Task<IDictionary<string, string>> Esri.ArcGISRuntime.Security.IOAuthAuthorizeHandler.AuthorizeAsync(Uri serviceUri, Uri authorizeUri, Uri callbackUri)
    {
        var g = Guid.NewGuid();

        UriBuilder b = new UriBuilder(authorizeUri);

        var query = System.Web.HttpUtility.ParseQueryString(authorizeUri.Query);
        query["state"] = $"appInstanceId={Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().Key}&signinId={g}";
        b.Query = query.ToString();
        authorizeUri = b.Uri;

        var tcs = new TaskCompletionSource<Uri>();
        var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = "rundll32.exe";
        process.StartInfo.Arguments = "url.dll,FileProtocolHandler " + authorizeUri.ToString();
        process.StartInfo.UseShellExecute = true;
        process.Start();
        tasks.Add(g.ToString(), tcs);
        var uri = await tcs.Task.ConfigureAwait(false);
        var str = string.Empty;
        if (!string.IsNullOrEmpty(uri.Fragment))
            str = uri.Fragment.Substring(1);
        else if (!string.IsNullOrEmpty(uri.Query))
            str = uri.Query;

        query = System.Web.HttpUtility.ParseQueryString(str); 
        Dictionary<string, string> values = new Dictionary<string, string>();
        foreach(string key in query.Keys)
        {
            values[key] = query[key] ?? String.Empty;
        }
        return values;
    }
}
