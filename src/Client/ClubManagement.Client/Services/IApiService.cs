using ClubManagement.Shared.DTOs;

namespace ClubManagement.Client.Services;

public interface IApiService
{
    Task<ApiResponse<T>?> GetAsync<T>(string endpoint);
    Task<ApiResponse<T>?> PostAsync<T>(string endpoint, object? data = null);
    Task<ApiResponse<T>?> PutAsync<T>(string endpoint, object data);
    Task<ApiResponse<T>?> DeleteAsync<T>(string endpoint);
}