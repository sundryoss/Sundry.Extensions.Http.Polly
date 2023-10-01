namespace Sundry.Extensions.Http.Polly.Console.Services;

public interface IWeatherService
{  
      ValueTask<bool> GetWeatherforecast();
}

public class WeatherService : IWeatherService
{
      private readonly HttpClient _httpClient;
      public WeatherService(HttpClient httpClient)
      {
            _httpClient = httpClient;     
      }
    public  async ValueTask<bool> GetWeatherforecast()
    {
      var responseMessage = await _httpClient.GetAsync("WeatherForecast");   
      return responseMessage.IsSuccessStatusCode;                            
    }

}
