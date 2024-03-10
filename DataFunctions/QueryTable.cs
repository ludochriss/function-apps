using System.Dynamic;
using System.Net;
using System.Reflection;
using System.Text.Json.Nodes;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Services;

namespace DataFunctions
{
    public class QueryTable
    {
        private readonly ILogger _logger;
  public TableService _tableService { get; set; }
        public RestApiService _apiService { get; set; }
        public QueryTable(ILoggerFactory loggerFactory,TableService tableService, RestApiService apiService)
        {
            _logger = loggerFactory.CreateLogger<QueryTable>();
              _tableService = tableService;
            _apiService = apiService;
        }

        [Function("QueryTable")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous,  "post")] HttpRequestData req)
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
                List<TableEntity>? result = null;
                if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                {
                    message = "Row key or partition key is missing or null";
                    _logger.LogInformation(message);
                    return await _apiService.HandleHttpResponseAsync(req, HttpStatusCode.BadRequest, message);
                }
                result = await _tableService.QueryPartitionKeyAsync(partitionKey);               
                
                _logger.LogInformation($"{className}: C# HTTP trigger function processed a request.");
                dynamic responseObject = new ExpandoObject();
                responseObject.result = result;


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
