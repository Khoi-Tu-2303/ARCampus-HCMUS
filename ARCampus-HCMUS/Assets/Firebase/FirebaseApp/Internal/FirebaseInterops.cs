

using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading.Tasks;

namespace Firebase.Internal
{
  
  internal static class FirebaseInterops
  {
    
    private static PropertyInfo _dataCollectionProperty = null;

    
    private static Type _appCheckType;
    private static MethodInfo _appCheckGetInstanceMethod;
    private static MethodInfo _appCheckGetTokenMethod;
    private static PropertyInfo _appCheckTokenResultProperty;
    private static PropertyInfo _appCheckTokenTokenProperty;
    
    private static bool _appCheckReflectionInitialized = false;
    
    private const string appCheckHeader = "X-Firebase-AppCheck";

    
    private static Type _authType;
    private static MethodInfo _authGetAuthMethod;
    private static PropertyInfo _authCurrentUserProperty;
    private static MethodInfo _userTokenAsyncMethod;
    private static PropertyInfo _userTokenTaskResultProperty;
    
    private static bool _authReflectionInitialized = false;
    
    private const string authHeader = "Authorization";

    static FirebaseInterops()
    {
      InitializeAppReflection();
      InitializeAppCheckReflection();
      InitializeAuthReflection();
    }

    private static void LogError(string message)
    {
#if FIREBASEAI_DEBUG_LOGGING
      UnityEngine.Debug.LogError(message);
#endif
    }

    
    private static void InitializeAppReflection()
    {
      try
      {
        _dataCollectionProperty = typeof(FirebaseApp).GetProperty(
            "IsDataCollectionDefaultEnabled",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (_dataCollectionProperty == null)
        {
          LogError("Could not find FirebaseApp.IsDataCollectionDefaultEnabled property via reflection.");
          return;
        }
        if (_dataCollectionProperty.PropertyType != typeof(bool))
        {
          LogError("FirebaseApp.IsDataCollectionDefaultEnabled is not a bool, " +
                   $"but is {_dataCollectionProperty.PropertyType}");
          return;
        }
      }
      catch (Exception e)
      {
        LogError($"Failed to initialize FirebaseApp reflection: {e}");
      }
    }

    
    public static bool GetIsDataCollectionDefaultEnabled(FirebaseApp firebaseApp)
    {
      if (firebaseApp == null || _dataCollectionProperty == null)
      {
        return false;
      }

      try
      {
        return (bool)_dataCollectionProperty.GetValue(firebaseApp);
      }
      catch (Exception e)
      {
        LogError($"Error accessing 'IsDataCollectionDefaultEnabled': {e}");
        return false;
      }
    }

    
    private const string _unknownSdkVersion = "unknown";
    private static readonly Lazy<string> _sdkVersionFetcher = new(() =>
    {
      try
      {
        
        Type versionInfoType = typeof(FirebaseApp).Assembly.GetType("Firebase.VersionInfo");
        if (versionInfoType == null)
        {
          LogError("Firebase.VersionInfo type not found via reflection");
          return _unknownSdkVersion;
        }

        
        PropertyInfo sdkVersionProperty = versionInfoType.GetProperty(
                "SdkVersion",
                BindingFlags.Static | BindingFlags.NonPublic);
        if (sdkVersionProperty == null)
        {
          LogError("Firebase.VersionInfo.SdkVersion property not found via reflection.");
          return _unknownSdkVersion;
        }

        return sdkVersionProperty.GetValue(null) as string ?? _unknownSdkVersion;
      }
      catch (Exception e)
      {
        LogError($"Error accessing SdkVersion via reflection: {e}");
        return _unknownSdkVersion;
      }
    });

    
    internal static string GetVersionInfoSdkVersion()
    {
      return _sdkVersionFetcher.Value;
    }

    
    private static void InitializeAppCheckReflection()
    {
      const string firebaseAppCheckTypeName = "Firebase.AppCheck.FirebaseAppCheck, Firebase.AppCheck";
      const string getAppCheckTokenMethodName = "GetAppCheckTokenAsync";

      try
      {
        
        _appCheckReflectionInitialized = false;

        _appCheckType = Type.GetType(firebaseAppCheckTypeName);
        if (_appCheckType == null)
        {
          return;
        }

        
        _appCheckGetInstanceMethod = _appCheckType.GetMethod(
            "GetInstance", BindingFlags.Static | BindingFlags.Public, null,
            new Type[] { typeof(FirebaseApp) }, null);
        if (_appCheckGetInstanceMethod == null)
        {
          LogError("Could not find FirebaseAppCheck.GetInstance method via reflection.");
          return;
        }

        
        _appCheckGetTokenMethod = _appCheckType.GetMethod(
            getAppCheckTokenMethodName, BindingFlags.Instance | BindingFlags.Public, null,
            new Type[] { typeof(bool) }, null);
        if (_appCheckGetTokenMethod == null)
        {
          LogError($"Could not find {getAppCheckTokenMethodName} method via reflection.");
          return;
        }

        
        Type appCheckTokenTaskType = _appCheckGetTokenMethod.ReturnType;

        
        _appCheckTokenResultProperty = appCheckTokenTaskType.GetProperty("Result");
        if (_appCheckTokenResultProperty == null)
        {
          LogError("Could not find Result property on App Check token Task.");
          return;
        }

        
        Type appCheckTokenType = _appCheckTokenResultProperty.PropertyType;

        _appCheckTokenTokenProperty = appCheckTokenType.GetProperty("Token");
        if (_appCheckTokenTokenProperty == null)
        {
          LogError($"Could not find Token property on AppCheckToken.");
          return;
        }

        _appCheckReflectionInitialized = true;
      }
      catch (Exception e)
      {
        LogError($"Exception during static initialization of FirebaseInterops: {e}");
      }
    }

    
    internal static async Task<string> GetAppCheckTokenAsync(FirebaseApp firebaseApp)
    {
      
      if (!_appCheckReflectionInitialized)
      {
        return null;
      }

      try
      {
        
        object appCheckInstance = _appCheckGetInstanceMethod.Invoke(null, new object[] { firebaseApp });
        if (appCheckInstance == null)
        {
          LogError("Failed to get FirebaseAppCheck instance via reflection.");
          return null;
        }

        
        object taskObject = _appCheckGetTokenMethod.Invoke(appCheckInstance, new object[] { false });
        if (taskObject is not Task appCheckTokenTask)
        {
          LogError($"Invoking GetToken did not return a Task.");
          return null;
        }

        
        await appCheckTokenTask;

        
        if (appCheckTokenTask.IsFaulted)
        {
          LogError($"Error getting App Check token: {appCheckTokenTask.Exception}");
          return null;
        }

        
        object tokenResult = _appCheckTokenResultProperty.GetValue(appCheckTokenTask); 
        if (tokenResult == null)
        {
          LogError("App Check token result was null.");
          return null;
        }

        
        return _appCheckTokenTokenProperty.GetValue(tokenResult) as string;
      }
      catch (Exception e)
      {
        
        LogError($"An error occurred while trying to fetch App Check token: {e}");
      }
      return null;
    }

    
    private static void InitializeAuthReflection()
    {
      const string firebaseAuthTypeName = "Firebase.Auth.FirebaseAuth, Firebase.Auth";
      const string getTokenMethodName = "TokenAsync";

      try
      {
        
        _authReflectionInitialized = false;

        _authType = Type.GetType(firebaseAuthTypeName);
        if (_authType == null)
        {
          
          return;
        }

        
        _authGetAuthMethod = _authType.GetMethod(
            "GetAuth", BindingFlags.Static | BindingFlags.Public, null,
            new Type[] { typeof(FirebaseApp) }, null);
        if (_authGetAuthMethod == null)
        {
          LogError("Could not find FirebaseAuth.GetAuth method via reflection.");
          return;
        }

        
        _authCurrentUserProperty = _authType.GetProperty("CurrentUser", BindingFlags.Instance | BindingFlags.Public);
        if (_authCurrentUserProperty == null)
        {
          LogError("Could not find FirebaseAuth.CurrentUser property via reflection.");
          return;
        }

        
        Type userType = _authCurrentUserProperty.PropertyType;

        
        _userTokenAsyncMethod = userType.GetMethod(
            getTokenMethodName, BindingFlags.Instance | BindingFlags.Public, null,
            new Type[] { typeof(bool) }, null);
        if (_userTokenAsyncMethod == null)
        {
          LogError($"Could not find FirebaseUser.{getTokenMethodName}(bool) method via reflection.");
          return;
        }

        
        Type tokenTaskType = _userTokenAsyncMethod.ReturnType;

        
        _userTokenTaskResultProperty = tokenTaskType.GetProperty("Result");
        if (_userTokenTaskResultProperty == null)
        {
          LogError("Could not find Result property on Auth token Task.");
          return;
        }

        
        if (_userTokenTaskResultProperty.PropertyType != typeof(string))
        {
          LogError("Auth token Task's Result property is not a string, " +
              $"but is {_userTokenTaskResultProperty.PropertyType}");
          return;
        }

        _authReflectionInitialized = true;
      }
      catch (Exception e)
      {
        LogError($"Exception during static initialization of Auth reflection in FirebaseInterops: {e}");
        _authReflectionInitialized = false;
      }
    }

    
    internal static async Task<string> GetAuthTokenAsync(FirebaseApp firebaseApp)
    {
      
      if (!_authReflectionInitialized)
      {
        return null;
      }

      try
      {
        
        object authInstance = _authGetAuthMethod.Invoke(null, new object[] { firebaseApp });
        if (authInstance == null)
        {
          LogError("Failed to get FirebaseAuth instance via reflection.");
          return null;
        }

        
        object currentUser = _authCurrentUserProperty.GetValue(authInstance);
        if (currentUser == null)
        {
          
          return null;
        }

        
        object taskObject = _userTokenAsyncMethod.Invoke(currentUser, new object[] { false });
        if (taskObject is not Task tokenTask)
        {
          LogError("Invoking TokenAsync did not return a Task.");
          return null;
        }

        
        await tokenTask;

        
        if (tokenTask.IsFaulted)
        {
          LogError($"Error getting Auth token: {tokenTask.Exception}");
          return null;
        }

        
        return _userTokenTaskResultProperty.GetValue(tokenTask) as string;
      }
      catch (Exception e)
      {
        
        LogError($"An error occurred while trying to fetch Auth token: {e}");
      }
      return null;
    }

    
    internal static async Task AddFirebaseTokensAsync(HttpRequestMessage request, FirebaseApp firebaseApp)
    {
      string appCheckToken = await GetAppCheckTokenAsync(firebaseApp);
      if (!string.IsNullOrEmpty(appCheckToken))
      {
        request.Headers.Add(appCheckHeader, appCheckToken);
      }

      string authToken = await GetAuthTokenAsync(firebaseApp);
      if (!string.IsNullOrEmpty(authToken))
      {
        request.Headers.Add(authHeader, $"Firebase {authToken}");
      }
    }

    
    internal static async Task AddFirebaseTokensAsync(ClientWebSocket socket, FirebaseApp firebaseApp)
    {
      string appCheckToken = await GetAppCheckTokenAsync(firebaseApp);
      if (!string.IsNullOrEmpty(appCheckToken))
      {
        socket.Options.SetRequestHeader(appCheckHeader, appCheckToken);
      }

      string authToken = await GetAuthTokenAsync(firebaseApp);
      if (!string.IsNullOrEmpty(authToken))
      {
        socket.Options.SetRequestHeader(authHeader, $"Firebase {authToken}");
      }
    }
  }

}
