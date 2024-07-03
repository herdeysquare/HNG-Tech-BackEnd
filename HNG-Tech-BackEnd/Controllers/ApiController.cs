using HNG_Tech_BackEnd.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace HNG_Tech_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ApiController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string visitor_name)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var location = "unknown";
            var temperature = "unknown";

            try
            {
                using (var client = _httpClientFactory.CreateClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/json"));

                    // Get location based on IP
                    var locationResponse = await client.GetAsync($"https://ipinfo.io?token=b4c50bc86dc3ab");
                    if (locationResponse.IsSuccessStatusCode)
                    {
                        var locationJson = JObject.Parse(await locationResponse.Content.ReadAsStringAsync());
                        location = locationJson["Lagos"]?.ToString() ?? "Lagos";
                    }
                    else
                    {
                        var errorContent = await locationResponse.Content.ReadAsStringAsync();
                        return StatusCode((int)locationResponse.StatusCode, $"Error fetching location: {errorContent}");
                    }

                    // Get weather based on location
                    var weatherResponse = await client.GetAsync($"https://api.openweathermap.org/data/2.5/weather?q=Lagos&units=metric&appid=97faabff35c9db05c915edf006837cd9");
                    if (weatherResponse.IsSuccessStatusCode)
                    {
                        var weatherJson = JObject.Parse(await weatherResponse.Content.ReadAsStringAsync());
                        temperature = weatherJson["main"]?["temp"]?.ToString() ?? "Unknown temperature";
                    }
                    else if (weatherResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return NotFound($"Weather data not found for {location}");
                    }
                    else
                    {
                        var errorContent = await weatherResponse.Content.ReadAsStringAsync();
                        return StatusCode((int)weatherResponse.StatusCode, $"Error fetching weather: {errorContent}");
                    }
                }

                var greeting = $"Hello, {visitor_name}!, the temperature is {temperature} degrees Celsius in {location} " +
                    $"stay warm and avoid the Island as it is flooded";

                var response = new ApiResponse
                {
                    ClientIp = clientIp,
                    Location = location,
                    Greeting = greeting
                };

                return Ok(response);
            }
            catch (HttpRequestException httpEx)
            {
                // Log and handle the HTTP request error
                return StatusCode(500, $"HTTP Request error: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                // Log and handle general errors
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }

    public class ApiResponse
    {
        public string ClientIp { get; set; }
        public string Location { get; set; }
        public string Greeting { get; set; }
    }
}
