using System.Net.Http.Json;
using System.Net;
using ClubManagement.Shared.DTOs;

namespace ClubManagement.Client.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;

    public ApiService(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public async Task<ApiResponse<T>?> GetAsync<T>(string endpoint)
    {
        return await ExecuteWithAuthRetry(async () =>
        {
            var response = await _httpClient.GetAsync(endpoint);
            return await HandleResponse<T>(response);
        });
    }

    public async Task<ApiResponse<T>?> PostAsync<T>(string endpoint, object? data = null)
    {
        return await ExecuteWithAuthRetry(async () =>
        {
            var response = data != null 
                ? await _httpClient.PostAsJsonAsync(endpoint, data)
                : await _httpClient.PostAsync(endpoint, null);
            return await HandleResponse<T>(response);
        });
    }

    public async Task<ApiResponse<T>?> PutAsync<T>(string endpoint, object data)
    {
        return await ExecuteWithAuthRetry(async () =>
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, data);
            return await HandleResponse<T>(response);
        });
    }

    public async Task<ApiResponse<T>?> DeleteAsync<T>(string endpoint)
    {
        return await ExecuteWithAuthRetry(async () =>
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            return await HandleResponse<T>(response);
        });
    }

    private async Task<ApiResponse<T>?> ExecuteWithAuthRetry<T>(Func<Task<ApiResponse<T>?>> apiCall)
    {
        try
        {
            // Ensure we have a valid token before making the API call
            if (!await _authService.IsAuthenticatedAsync())
            {
                await _authService.HandleSessionExpiredAsync();
                return ApiResponse<T>.ErrorResult("Session expired. Please login again.");
            }
            
            return await apiCall();
        }
        catch (HttpRequestException ex) when (IsUnauthorized(ex))
        {
            // Token might be expired, try to refresh
            if (await _authService.RefreshTokenAsync())
            {
                // Retry the API call after successful token refresh
                return await apiCall();
            }
            else
            {
                // Refresh failed - session expired
                await _authService.HandleSessionExpiredAsync();
                return ApiResponse<T>.ErrorResult("Session expired. Please login again.");
            }
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.ErrorResult($"Request failed: {ex.Message}");
        }
    }

    private async Task<ApiResponse<T>?> HandleResponse<T>(HttpResponseMessage response)
    {
        // Handle 401 Unauthorized specifically
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (await _authService.RefreshTokenAsync())
            {
                // Don't retry here - let the outer ExecuteWithAuthRetry handle the retry
                throw new HttpRequestException("401 Unauthorized - token refresh needed");
            }
            else
            {
                // Refresh failed - session expired
                await _authService.HandleSessionExpiredAsync();
                return ApiResponse<T>.ErrorResult("Session expired. Please login again.");
            }
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
    }

    private static bool IsUnauthorized(HttpRequestException ex)
    {
        return ex.Message.Contains("401") || ex.Message.Contains("Unauthorized");
    }
}