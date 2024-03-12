using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Services;
using Services.Functions;


namespace CryptoFunctions.PriceCheck
{
    public class PriceCheck : BaseFunction<PriceCheck>
    {
        public CryptoService _cryptoService { get; set; }
        public PriceCheck(ILogger<PriceCheck> _logger, TableService tableService, RestApiService apiService, CryptoService cryptoService) : base(_logger, tableService, apiService)
        {
            _cryptoService = cryptoService;
        }

        [Function("PriceCheck")]
        public async Task Run([TimerTrigger("5 * * * * *", RunOnStartup = true)] TimerInfo myTimer)
        {
            Guid guid = new Guid();
            string name = "PriceCheck";

            var cancelResult =  await _cryptoService.SpotAccountTrade.CancelAllOpenOrdersOnASymbol("BTCUSDT");
            //get orderBinding AND orderSubmittedSuccessfully = false
            string filter = "orderSubmitted eq false";
            var triggers = await _tableService.QueryAsync(filter);
            foreach (var trigger in triggers)
            {
                var containsOrderId = trigger.TryGetValue("bindingOrderId", out object bindingOrderId);
                var containsSymbol = trigger.TryGetValue("symbol", out object symbol);
                if (containsOrderId && containsSymbol)
                {
                    var result = await _cryptoService.QueryOpenOrdersForSymbolAsync(symbol.ToString().ToUpper(), long.Parse(bindingOrderId.ToString()));
                    if (result == null)
                    {
                        var orderResult = await _cryptoService.PostOneCancelsOtherOrderAsync(trigger);
                        var orderListId = orderResult.FirstOrDefault(x => x.Key == "orderListId").Value;
                        var ocoOrders = orderResult.FirstOrDefault(x => x.Key == "orders").Value as JsonArray;
                        if (orderResult != null && ocoOrders != null)
                        {
                            logger.LogInformation($"{name}: OCO order submitted successfully. OrderListId: {orderListId}");
                            TableEntity te = new TableEntity(trigger);
                            te.Remove("orderSubmitted");
                            te.Add("orderSubmitted", true);
                            var upsertResult =await _tableService.UpsertAsync(te);
                            //TODO:send update to SPA if the upsert fails
                        }                      
                    }
                }
            }
        }
    }
}
