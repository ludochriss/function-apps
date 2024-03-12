using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Data.Tables;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using Services;
using Services.Functions;

namespace CryptoFunctions
{
    public class CreateStrategyOrder : BaseFunction<CreateStrategyOrder>
    {
        private readonly CryptoService _cryptoService;

        public CreateStrategyOrder(ILogger<CreateStrategyOrder> logger, TableService tableService, RestApiService restApiService, CryptoService cryptoService) : base(logger, tableService, restApiService)
        {
            _cryptoService = cryptoService;
        }

        [Function("CreateStrategyOrder")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {

            string name = "CreateStrategyOrder";
            logger.LogInformation("Started the CreateStrategyOrder function.");
            string message = string.Empty;
            HttpResponseData jsonResponse = null;
            JsonObject jsonResponseData = null;
            long orderId = _cryptoService.GenerateNewBinanceOrderId();
            try
            {
                JsonObject requestJson = await JsonSerializer.DeserializeAsync<JsonObject>(req.Body);
                //TODO: post the limit order to binance and retrieve the orderId of the limit order to add as the rowkey of the stoploss in the same order as the strategy is attached
                //var limitOrder = requestJson.TryGetPropertyValue("limitOrder",out var limitOrderValue) ? limitOrderValue as JsonObject : null;

                // var limitResult =  await _cryptoService.PostLimitOrderAsync(requestJson,orderId);

                //TODO: determine and validate the strategy type (fail if not correct) will propbably need a switch case as the
                //number of strategys grow
                var ocoOrderDetails = requestJson.TryGetPropertyValue("orderDetails", out var ocoOrderDetailsValue) ? ocoOrderDetailsValue as JsonObject : null;
                var orderTrigger = requestJson.TryGetPropertyValue("orderTrigger", out var orderTriggerValue) ? orderTriggerValue as JsonObject : null;
                if (ocoOrderDetails == null || orderTrigger == null) throw new ArgumentException("Invalid request body.  orderDetails, orderTrigger not found in payload.");

                var subType = requestJson.TryGetPropertyValue("orderSubType", out var subTypeValue) ? subTypeValue.ToString().ToLower() : null;
                var clientOrderId = orderTrigger.TryGetPropertyValue("clientOrderId", out var clientOrderIdValue) ? clientOrderIdValue.ToString() : null;
                var symbol = ocoOrderDetails.TryGetPropertyValue("symbol", out var symbolValue) ? symbolValue.ToString() : null;

                if (subType == null || clientOrderId == null || symbol == null || ocoOrderDetails == null || orderTrigger == null) throw new ArgumentException("Invalid request body. Order subType, symbol, orderDetails, orderTrigger or clientOrderId not found in payload.");
                //consider sending the order to testnet for validation?
                string pk, rk;
                pk = symbol;
                rk = symbol + clientOrderId;
                TableEntity e = new TableEntity(pk, rk);
                foreach (var jData in ocoOrderDetails)
                {
                    if (jData.Value != null)
                    {

                        e.Add(jData.Key.ToString(), jData.Value.ToString());
                    }
                }
                foreach (var prop in orderTrigger)
                {
                    if (prop.Value != null)
                    {

                        e.Add(prop.Key.ToString(), prop.Value.ToString());
                    }
                }
                //TODO: hit table service to upload the order and the strategy
                var tableResult = await _tableService._tableClient.AddEntityAsync(e, CancellationToken.None);







            }
            catch (InvalidJsonException jex)
            {
                logger.LogError($"{name}: Json Exception: {jex.Message}");
                jsonResponse = await _restApiService.HandleHttpResponseAsync(req, HttpStatusCode.BadRequest, "Invalid request body. Could not parse JSON.");
            }
            catch (ArgumentException anex)
            {

                logger.LogError($"{name}: Argument Exception: {anex.Message}");
                jsonResponse = await _restApiService.HandleHttpResponseAsync(req, HttpStatusCode.BadRequest, anex);
            }
            catch (JsonException jex)
            {
                logger.LogError($"{name}: Json Exception: {jex.Message}");
                jsonResponse = await _restApiService.HandleHttpResponseAsync(req, HttpStatusCode.BadRequest, "Invalid request body. Could not parse JSON.");
            }
            catch (Exception ex)
            {
                // Handle exception
                logger.LogError($"{name}: Exception: {ex.Message}");
            }
            finally
            {
                logger.LogInformation($"{name}: Complete");
            }


            return await _restApiService.HandleHttpResponseAsync(req, HttpStatusCode.OK, jsonResponse);
        }

    }
}
