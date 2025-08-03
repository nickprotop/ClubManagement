using System.Net.Http.Json;
using ClubManagement.Shared.DTOs;

namespace ClubManagement.Client.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<T>?> GetAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            return await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.ErrorResult($"Request failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<T>?> PostAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            var response = data != null 
                ? await _httpClient.PostAsJsonAsync(endpoint, data)
                : await _httpClient.PostAsync(endpoint, null);
            return await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.ErrorResult($"Request failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<T>?> PutAsync<T>(string endpoint, object data)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, data);
            return await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.ErrorResult($"Request failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<T>?> DeleteAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            return await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.ErrorResult($"Request failed: {ex.Message}");
        }
    }
}