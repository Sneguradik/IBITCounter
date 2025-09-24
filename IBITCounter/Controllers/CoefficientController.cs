using Application.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IBITCounter.Controllers
{
    [Route("coefficients")]
    [ApiController]
    public class CoefficientController(CoefficientStorage coefficientStorage, ILogger<CoefficientController> logger) : ControllerBase
    {
        private readonly object _lock = new ();
        
        [HttpGet]
        public CoefficientStorage GetCoefficients() => coefficientStorage;

        [HttpPost]
        public void SetCoefficients([FromBody]CoefficientStorage coefficients)
        {
            lock (_lock)
            {
                coefficientStorage.StakingFee =  coefficients.StakingFee;
                coefficientStorage.Median = coefficients.Median;
                coefficients.Day = coefficients.Day;
            }
            logger
                .LogInformation("Set coefficients - Staking Fee: {CoefficientsStakingFee} | Median BTC: {CoefficientsMedian} | Day: {CoefficientsDay}", 
                    coefficients.StakingFee, coefficients.Median, coefficients.Day);
        }

        [HttpPost("staking_fee")]
        public void SetStakingFee(CoefficientValue value)
        {
            lock (_lock)
            {
                coefficientStorage.StakingFee = value.Value;
            }
            logger.LogInformation("Set coefficients - Staking Fee: {CoefficientsStakingFee}", value.Value);
        }
        
        [HttpPost("median_btc")]
        public void SetMedianBtc(CoefficientValue value)
        {
            lock (_lock)
            {
                coefficientStorage.Median = value.Value;
            }
            logger.LogInformation("Set coefficients - Median BTC: {CoefficientsMedian}", value.Value);
        }
        
        [HttpPost("day")]
        public void SetDay(CoefficientValue value)
        {
            lock (_lock)
            {
                coefficientStorage.Day = (int)value.Value;
            }
            logger.LogInformation("Set coefficients - Day: {CoefficientsDay}", value.Value);
        }
    }

    public record CoefficientValue(double Value);
}
