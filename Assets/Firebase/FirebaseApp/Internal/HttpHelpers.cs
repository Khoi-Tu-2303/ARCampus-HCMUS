

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Firebase.Internal
{
  
  internal static class HttpHelpers
  {
    internal static async Task SetRequestHeaders(HttpRequestMessage request, FirebaseApp firebaseApp)
    {
      request.Headers.Add("x-goog-api-key", firebaseApp.Options.ApiKey);
      string version = FirebaseInterops.GetVersionInfoSdkVersion();
      request.Headers.Add("x-goog-api-client", $"gl-csharp/8.0 fire/{version}");
      if (FirebaseInterops.GetIsDataCollectionDefaultEnabled(firebaseApp))
      {
        request.Headers.Add("X-Firebase-AppId", firebaseApp.Options.AppId);
        request.Headers.Add("X-Firebase-AppVersion", UnityEngine.Application.version);
      }
      
      await FirebaseInterops.AddFirebaseTokensAsync(request, firebaseApp);
    }

    
    
    internal static async Task ValidateHttpResponse(HttpResponseMessage response)
    {
      if (response.IsSuccessStatusCode)
      {
        return;
      }

      
      string errorContent = "No error content available.";
      if (response.Content != null)
      {
        try
        {
          errorContent = await response.Content.ReadAsStringAsync();
        }
        catch (Exception readEx)
        {
          
          errorContent = $"Failed to read error content: {readEx.Message}";
        }
      }

      
      var ex = new HttpRequestException(
        $"HTTP request failed with status code: {(int)response.StatusCode} ({response.ReasonPhrase}).\n" +
        $"Error Content: {errorContent}",
        null
      );
      ex.Data["StatusCode"] = response.StatusCode;

      throw ex;
    }
  }

  
  internal static class HttpRequestExceptionExtensions
  {
    internal static HttpStatusCode? GetStatusCode(this HttpRequestException exception)
    {
      if (exception.Data.Contains("StatusCode"))
      {
        return (HttpStatusCode)exception.Data["StatusCode"];
      }
      return null;
    }
  }
}
