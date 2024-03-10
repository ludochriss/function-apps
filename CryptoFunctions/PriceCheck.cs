using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Services;
using Services.Functions;


namespace CryptoFunctions.PriceCheck
{
    public class PriceCheck:BaseFunction<PriceCheck>
    {
        public CryptoService _cryptoService { get; set; }
        public PriceCheck(ILogger<PriceCheck> _logger, TableService tableService, RestApiService apiService, CryptoService cryptoService):base(_logger,tableService,apiService)
        {
            _cryptoService = cryptoService;
        }

        [Function("PriceCheck")]
        public async Task Run([TimerTrigger("5 * * * * *", RunOnStartup =true)] TimerInfo myTimer)
        {
            Guid guid = new Guid();

            //logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var result = await _cryptoService.GetSymbolPriceAsync("BTCUSDT");

            var test =  await _cryptoService.TestSpotAccountTrade();
           string accountInfo =await  _cryptoService.SpotAccountTrade.AccountInformation();
        //    if(string.IsNullOrEmpty(test)) logger.LogInformation("TestSpotAccountTrade: Failed");
            logger.LogInformation($"{result}");
            
            if (myTimer.ScheduleStatus is not null)
            {
                logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
