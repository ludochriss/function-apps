using System.Dynamic;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Services;

namespace DataFunctions
{
    public class GetTableEntity
    {
        private readonly ILogger _logger;
        public TableService _tableService { get; set; }
        public RestApiService _apiService { get; set; }

        public GetTableEntity(ILoggerFactory loggerFactory, TableService tableService, RestApiService apiService)
        {
            _logger = loggerFactory.CreateLogger<GetTableEntity>();
            _tableService = tableService;
            _apiService = apiService;
        }
        [Function("GetTableEntity")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            string className = MethodBase.GetCurrentMethod()?.DeclaringType?.Name ?? string.Empty;
            _logger.LogInformation($"{className}: Started");
            string message = string.Empty;
            try
            {
                string bodyString = req.ReadAsStringAsync().Result ?? string.Empty;
                JsonNode body = JsonNode.Parse(bodyString);
                string partitionKey = body["partitionKey"]?.ToString();
                string rowKey = body["rowKey"]?.ToString();
                TableEntity? result = null;
                if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                {
                    message = "Row key or partition key is missing or null";
                    _logger.LogInformation(message);
                    return await _apiService.HandleHttpResponseAsync(req, HttpStatusCode.BadRequest, message);
                }
                result = await _tableService.GetTableEntityAsync(partitionKey, rowKey);
                _logger.LogInformation($"{className}: C# HTTP trigger function processed a request.");
                var tableValue = result.FirstOrDefault(x => x.Key == "message");
                var responseObject = new Dictionary<string, object> { { tableValue.Key, tableValue.Value } };
                return await _apiService.HandleHttpResponseAsync(req, HttpStatusCode.OK, responseObject);
            }
            catch (Exception ex)
            {
                message = "Exception occurred processing request";
                _logger.LogError($"{message}: {ex.Message}");
                return await _apiService.HandleHttpResponseAsync(req, HttpStatusCode.BadRequest, message, ex.Message);
            }
        }
    }
}
