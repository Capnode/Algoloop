using Borsdata.Api.Dal.Infrastructure;
using Borsdata.Api.Dal.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;


/// <summary>
/// Sample class to call Borsdata API V1.
/// The ratelimit logic only work with single thread.
/// </summary>
namespace Borsdata.Api.Dal
{
    public class ApiClient : IDisposable
    {
        HttpClient _client; 
        string _authKey;                // Querystring authKey
        Stopwatch _timer;               // Check time from last Api call to check Ratelimit
        string _urlRoot;

        public ApiClient(string apiKey)
        {
            _authKey = "?authKey="+ apiKey;

            _timer = Stopwatch.StartNew();
            _urlRoot = "https://apiservice.borsdata.se/";
        }


        /// <summary> Return list of all instruments</summary>
        public InstrumentRespV1 GetInstruments()
        {
            string url = string.Format(_urlRoot+"/v1/instruments/");
            HttpResponseMessage response = WebbCall(url, _authKey);

            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                InstrumentRespV1 res = JsonConvert.DeserializeObject<InstrumentRespV1>(json);
                return res;
            }
            else
            {
                Console.WriteLine("GetInstruments {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            return null;
        }

        /// <summary> Return Full Year reports for one instrument (max 10 reports)</summary>
        public ReportsYearRespV1 GetReportsYear(long instrumentId)
        {
         
            string url = string.Format(_urlRoot+"/v1/instruments/{0}/reports/year", instrumentId);
            HttpResponseMessage response = WebbCall(url, _authKey);

            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                ReportsYearRespV1 res = JsonConvert.DeserializeObject<ReportsYearRespV1>(json);
                return res;
            }
            else
            {
                Console.WriteLine("GetReportsYear {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            return null;
        }

        /// <summary> Return R12 reports (Rolling 12Month => Sum of last four quarter reports) for one instrument (max 10 reports)</summary>
        public ReportsR12RespV1 GetReportsR12(long instrumentId)
        {

            string url = string.Format(_urlRoot+"/v1/instruments/{0}/reports/r12", instrumentId);
            HttpResponseMessage response = WebbCall(url, _authKey);

            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                ReportsR12RespV1 res = JsonConvert.DeserializeObject<ReportsR12RespV1>(json);
                return res;
            }
            else
            {
                Console.WriteLine("GetReportsR12 {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            return null;
        }

        /// <summary> Return Quarter reports (Normaly data for last 3 month) for one instrument (max 10 reports)</summary>
        public ReportsQuarterRespV1 GetReportsQuarter(long instrumentId)
        {

            string url = string.Format(_urlRoot+"/v1/instruments/{0}/reports/quarter", instrumentId);
            HttpResponseMessage response = WebbCall(url, _authKey);

            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                ReportsQuarterRespV1 res = JsonConvert.DeserializeObject<ReportsQuarterRespV1>(json);
                return res;
            }
            else
            {
                Console.WriteLine("GetReportsQuarter {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            return null;
        }

        /// <summary> Return EndDay stockprice for one Instrument (max 10 year history)</summary>
        public StockPricesRespV1 GetStockPrices(long instrumentId)
        {

            string url = string.Format(_urlRoot+"/v1/instruments/{0}/stockprices", instrumentId);
            HttpResponseMessage response = WebbCall(url, _authKey);

            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                StockPricesRespV1 res = JsonConvert.DeserializeObject<StockPricesRespV1>(json);
                return res;
            }
            else
            {
                Console.WriteLine("GetStockPrices {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            return null;
        }

        /// <summary> Return EndDay stockprice for one Instrument (max 10 year history)</summary>
        public StockPricesRespV1 GetStockPrices(long instrumentId, DateTime from, DateTime to)
        {
            string url = string.Format(_urlRoot + "/v1/instruments/{0}/stockprices", instrumentId);
            string urlPar = string.Format(_authKey + "&from={0}&to={1}", from.ToShortDateString(), to.ToShortDateString());
            HttpResponseMessage response = WebbCall(url, urlPar);

            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                StockPricesRespV1 res = JsonConvert.DeserializeObject<StockPricesRespV1>(json);
                return res;
            }
            else
            {
                Console.WriteLine("GetStockPrices time  {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            return null;
        }

        /// <summary>
        /// Some KPIs has history.
        /// See list of KPI and how to call on github
        /// https://github.com/Borsdata-Sweden/API/wiki/KPI-History
        /// </summary>
        /// <param name="instrumentId">Company Ericsson has instrumentId=77</param>
        /// <param name="KpiId">KPI id. P/E =2</param>
        /// <param name="rt"> What report is KPI calculated with? [year, r12, quarter]</param>
        /// <param name="pt">What stockprice is KPI calculated with? [mean, high, low]</param>
        /// <returns>List of historical KPI values</returns>
        public KpisHistoryRespV1 GetKpiHistory(long instrumentId, int KpiId, ReportType rt, PriceType pt)
        {

            string url = string.Format(_urlRoot + "/v1/Instruments/{0}/kpis/{1}/{2}/{3}/history", instrumentId, KpiId, rt.ToString(), pt.ToString());
            string urlPar = string.Format(_authKey);
            HttpResponseMessage response = WebbCall(url, urlPar);

            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                KpisHistoryRespV1 res = JsonConvert.DeserializeObject<KpisHistoryRespV1>(json);
                return res;
            }
            else
            {
                Console.WriteLine("GetStockPrices time  {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            return null;
        }

        /// <summary>
        /// Screener kpis. Return one datapoint for one Intrument.
        /// You can find exact API Url on Borsdata screener in the KPI window and [API URL] button.
        /// </summary>
        /// <param name="instrumentId">Company Ericsson has instrumentId=77</param>
        /// <param name="KpiId">KPI id</param>
        /// <param name="time">Time period for the KPI</param>
        /// <param name="calc">Calculation format.</param>
        /// <returns></returns>
        public KpisRespV1 GetKpiScreenerSingle(long instrumentId, int KpiId, string time, string calc)
        {
            string url = string.Format(_urlRoot + "/v1/Instruments/{0}/kpis/{1}/{2}/{3}", instrumentId, KpiId, time, calc);
            string urlPar = string.Format(_authKey);
            HttpResponseMessage response = WebbCall(url, urlPar);

            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                KpisRespV1 res = JsonConvert.DeserializeObject<KpisRespV1>(json);
                return res;
            }
            else
            {
                Console.WriteLine("GetStockPrices time  {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            return null;
        }



        /// <summary>
        /// Screener kpis. Return List of datapoints for all Intrument.
        /// You can find exact API Url on Borsdata screener in the KPI window and [API URL] button.
        /// </summary>
        /// <param name="KpiId">KPI id</param>
        /// <param name="time">Time period for the KPI</param>
        /// <param name="calc">Calculation format</param>
        /// <returns></returns>
        public KpisAllCompRespV1 GetKpiScreener(int KpiId, string time, string calc)
        {

            string url = string.Format(_urlRoot + "/v1/instruments/kpis/{0}/{1}/{2}", KpiId, time, calc);
            string urlPar = string.Format(_authKey);
            HttpResponseMessage response = WebbCall(url, urlPar);

            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                KpisAllCompRespV1 res = JsonConvert.DeserializeObject<KpisAllCompRespV1>(json);
                return res;
            }
            else
            {
                Console.WriteLine("GetStockPrices time  {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            return null;
        }


        public MarketsRespV1 GetMarkets()
        {
            string url = string.Format(_urlRoot + "/v1/markets");
            HttpResponseMessage response = WebbCall(url, _authKey);

            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                MarketsRespV1 res = JsonConvert.DeserializeObject<MarketsRespV1>(json);
                return res;
            }
            else
            {
                Console.WriteLine("GetMarkets {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            return null;
        }


        public SectorsRespV1 GetSectors()
        {
            string url = string.Format(_urlRoot + "/v1/sectors");
            HttpResponseMessage response = WebbCall(url, _authKey);

            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                SectorsRespV1 res = JsonConvert.DeserializeObject<SectorsRespV1>(json);
                return res;
            }
            else
            {
                Console.WriteLine("GetSectors {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            return null;
        }


        public CountriesRespV1 GetCountries()
        {
            string url = string.Format(_urlRoot + "/v1/countries");
            HttpResponseMessage response = WebbCall(url, _authKey);

            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                CountriesRespV1 res = JsonConvert.DeserializeObject<CountriesRespV1>(json);
                return res;
            }
            else
            {
                Console.WriteLine("GetCountries {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            return null;
        }


        public BranchesRespV1 GetBranches()
        {
            string url = string.Format(_urlRoot + "/v1/branches");
            HttpResponseMessage response = WebbCall(url, _authKey);

            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                BranchesRespV1 res = JsonConvert.DeserializeObject<BranchesRespV1>(json);
                return res;
            }
            else
            {
                Console.WriteLine("GetBranches {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            return null;
        }


        /// <summary>
        /// Return last 100 instruments with changed data or where reports is updated.
        /// </summary>
        /// <returns></returns>
        public InstrumentsUpdatedRespV1 GetInstrumentsUpdated()
        {
            string url = string.Format(_urlRoot + "/v1/instruments/updated");
            HttpResponseMessage response = WebbCall(url, _authKey);

            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                InstrumentsUpdatedRespV1 res = JsonConvert.DeserializeObject<InstrumentsUpdatedRespV1>(json);
                return res;
            }
            else
            {
                Console.WriteLine("GetInstrumentsUpdated {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            return null;
        }

        /// <summary>
        /// Return last time the Kpis was recalculated.
        /// Normaly this is at night after report and Stockprices is updated.
        /// But can also be during day when new reports is added.
        /// </summary>
        /// <returns></returns>
        public KpisCalcUpdatedRespV1 GetKpisCalcUpdated()
        {
            string url = string.Format(_urlRoot + "/v1/instruments/kpis/updated");
            HttpResponseMessage response = WebbCall(url, _authKey);

            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                KpisCalcUpdatedRespV1 res = JsonConvert.DeserializeObject<KpisCalcUpdatedRespV1>(json);
                return res;
            }
            else
            {
                Console.WriteLine("GetKpisCalcUpdated {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            return null;
        }


        // Return list of Instruments with StockSplit. 
        // StockSplit affects all historical stockprices, reportdata and Kpis for this instrument.
        public StockSplitRespV1 GetStockSplits()
        {
            string url = string.Format(_urlRoot + "/v1/instruments/StockSplits");
            HttpResponseMessage response = WebbCall(url, _authKey);

            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                StockSplitRespV1 res = JsonConvert.DeserializeObject<StockSplitRespV1>(json);
                return res;
            }
            else
            {
                Console.WriteLine("GetStockSplits {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            return null;
        }





        /// <summary>
        /// Combine URL and Querystring. Check if need to sleep (Ratelimit). Then call API.
        /// It try call API 2 times if Ratelimit is hit.
        /// </summary>
        /// <param name="url">API url</param>
        /// <param name="querystring">Querystring</param>
        /// <returns></returns>
        HttpResponseMessage WebbCall(string url, string querystring)
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri(url);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            SleepBeforeNewApiCall(); // Sleep if needed to avoid RateLimit
            HttpResponseMessage response = _client.GetAsync(querystring).Result; // Call API

            Console.WriteLine(url + " " + querystring);

            if ((int)response.StatusCode == 429) // We still get RateLimit error. Sleep more. 
            {
                //Console.WriteLine("StatusCode == 429.. Sleep more!!");
                System.Threading.Thread.Sleep(500);
                response = _client.GetAsync(querystring).Result; // Call API second time!
            }

            return response;
        }


        /// <summary>
        /// Ratelimit to API is 2 req/Sec.
        /// Check if the time sice last API call is less than 500ms. 
        /// Then sleep to avoid RateLimit 429.
        /// </summary>
        void SleepBeforeNewApiCall()
        {
            _timer.Stop();
            if (_timer.ElapsedMilliseconds < 500)
            {
                int sleepms = 550 - (int)_timer.ElapsedMilliseconds; //Add 50 extra ms.
                Console.WriteLine("Sleep Before New Api Call ms:" + sleepms);
                System.Threading.Thread.Sleep(sleepms);
            }
            _timer.Restart();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _client.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
