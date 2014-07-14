using System;
using System.Diagnostics;
using System.Web.Mvc;
using System.IO;
using HighAvailabilityLib;
using System.Net;
using System.Text;
using System.Collections.Generic;


namespace HighAvailabilityTestApp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            TextWriterTraceListener traceListener = new TextWriterTraceListener(AppDomain.CurrentDomain.BaseDirectory.ToString() + "HomeControllerTraceOutput.txt");
            Trace.Listeners.Add(traceListener);

            return View();
        }
       
        //Method called from javascript via AJAX in order to confirm username and password for site
        public JsonResult ConfirmSite(string site, string username, string password)
        {
            //ensure that scm endpoint url was not submitted
            if (site.Contains(".scm."))
            {
                return Json("It appears you have entered the URL of the scm endpoint. Please check the url again",JsonRequestBehavior.AllowGet);
            }

            var confirmationNum = getRandomNumber();
           
            //Call to change the target url to that of the site extension /hawebapi
            site = getSiteExtension(site);

            //Trace.WriteLine(site + "api/Confirmation/" + confirmationNum);
            var confirmationRequestUrl = Path.Combine(site , "api/Confirmation/" , confirmationNum.ToString());
            Trace.TraceInformation("HomeController::ConfirmSite(): site extention url:" + site);

            //Send Request to HA Web API for confirmation
            var confirmationResult = SendHAWebAPIRequest(confirmationRequestUrl, username, password);
            Trace.TraceInformation("HomeController::ConfirmSite(): The following should match: confirmation number: " + confirmationNum + " result: " + confirmationResult);

            if(int.Parse(confirmationResult.Replace("\"", ""))==confirmationNum){
                Trace.TraceInformation("HomeController::ConfirmSite(): Connection to Site Extension confirmed");
                return Json("VALID", JsonRequestBehavior.AllowGet);
            }
            else
            {
                Trace.TraceError("HomeController::ConfirmSite(): Connection to Site Extension Unsuccessful");
                //Will return this message and it will be displayed in the alert box
                return Json("Site was unable to be confirmed please retry entering publishing credentials", JsonRequestBehavior.AllowGet);
            }
            
        }

        //Method that is called by jquery AJAX in order to start CPU Test
        public JsonResult RunCPUTest( string[] sites, string username, string password)
        {
            Trace.TraceInformation("HomeController::RunCPUTest(): Sending Request to Start CPU Fault Test");
            return StartFaultTest(sites, username, password, TestResult.TestType.CPU);
        }

        //Method that is called by jquery AJAX in order to start CPU Test
        public JsonResult RunMemoryTest(string[] sites, string username, string password)
        {
            Trace.TraceInformation("HomeController::RunMemoryTest(): Sending Request to Start Memory Fault Test");
            return StartFaultTest(sites, username, password, TestResult.TestType.MEMORY);
        }


        public JsonResult StartFaultTest(string[] sites, string username, string password,TestResult.TestType type)
        {
            TimeSpan initdelay = new TimeSpan(0,0,15), 
                     delay =     new TimeSpan(0,0,20),
                     total =     new TimeSpan(0, 1, 0),
                     fault =     new TimeSpan(0,0,20);

            var responseText = "";

            //creates a seperate array containing the scm endpoint urls for each of the target web sites
            string[] scmsites = getSiteExtension(sites);

            try
            {
                responseText = RunFaultTest(delay.Add(initdelay), fault, scmsites, username, password, type);
            }
            catch (Exception ex)
            {
                Trace.TraceError("HomeController::StartFaultTest(): Exception encountered when calling RunFaultTest(): \n" + ex.ToString());
                return Json("FAIL", JsonRequestBehavior.AllowGet);
            }

            //Determine if the response is in the format that specifies a successful fault injection which is "Success;TESTTYPE"
            var split = responseText.Split(';');
            if (split.Length < 2)
            {
                Trace.TraceError("HomeController::StartFaultTest(): Incorrect response ['"+ responseText + "'] recieved from Web API");
                return Json("FAIL", JsonRequestBehavior.AllowGet);
            }
            else if (split.Length==2 && split[0].Equals("Success") && split[1].Equals(type.ToString()))
            {
                Trace.TraceInformation("HomeController::StartFaultTest(): Correct Response recieved: starting Load...");
                var filename = RunLoad(sites, total, initdelay, type);

                Trace.TraceInformation("HomeController::StartFaultTest(): Load Finished. Fetching Results...");
                var result = GetResults(filename, total, fault, delay, initdelay);

                if (result.invalidFormat == null)
                {
                    LogResults(result);
                }
                else
                {
                    return Json("FAIL", JsonRequestBehavior.AllowGet);
                }

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                Trace.TraceError("HomeController::StartFaultTest(): Incorrect response ['" + responseText + "'] recieved from Web API");
                return Json("FAIL", JsonRequestBehavior.AllowGet);
            }
        }


        public string RunFaultTest(TimeSpan delay, TimeSpan duration, string[] sites, string username, string password, TestResult.TestType type)
        {
            if (sites.Length > 0)
            {
                var testRequestUrl = Path.Combine(sites[0], "api/test/", ((int)type).ToString());
                var testRequestData = duration.TotalSeconds + ";" + delay.TotalSeconds;

                Trace.TraceInformation("HomeController::RunFaultTest(): Test Request Url: " + testRequestUrl);
                Trace.TraceInformation("HomeController::RunFaultTest(): Test Request Data: " + testRequestData);
                
                //Returns value that is recieved from the HA WebAPI
                return SendHAWebAPIRequest(testRequestUrl, testRequestData, username, password);
            }

            //return value that will trigger error in calling function that will relay the error to the user
            Trace.TraceError("HomeController::RunFaultTest(): There needs to be at least one site passed in");
            return "ERROR";

        }

        //Initializes new LoadManager and starts the HTTP Load
        public string RunLoad(string[] sites, TimeSpan total, TimeSpan initdelay, TestResult.TestType type)
        {
            Trace.TraceInformation("HomeController:RunLoad(): Sending Request to start HTTP Load");
            var loadManager = new LoadManager();
            return loadManager.HttpRequestLoad(sites, total, initdelay, type);
        }

        //Called when test run is complete and accepts filename of the file that is created by the LoadManager containing Load Statisitics
        //Returns a SimpleTestResult object that contains all necessary information for displaying results to user
        public SimpleTestResult GetResults(string filename, TimeSpan total, TimeSpan fault, TimeSpan delay, TimeSpan initdelay)
        {
            Trace.TraceInformation("HomeController:GetResults(): Sending Request to get results");
            var measurementManager = new MeasurementManager();
            return measurementManager.GetTestResult(filename, total, fault, delay, initdelay);
        }

        //Print outcome of load to log
        public void LogResults(SimpleTestResult result)
        {
            Trace.WriteLine("HomeController::LogResults(): The following is the results of the test:");
            for (var i = 0; i < result.latency.Length; i++)
            {
                Trace.WriteLine(i + "  Latency:" + result.latency[i] + "success: " + result.successRate[i]);
            }
        }

        //Method for sending a HA WebAPI request 
        public string SendHAWebAPIRequest(string site, string username, string password){
            return SendHAWebAPIRequest(site, "", username, password);
        }
        
        //Overloaded Method that allows a HA WebAPI request with data
        public string SendHAWebAPIRequest(string site, string data, string username, string password)
        {
            Trace.TraceInformation("HomeController::SendHAWebAPIRequest(): Entering...");
            var request = WebRequest.Create(site);

            //request parameters
            request.Method = "POST";
            var cred = Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
            request.Headers.Add("Authorization", "Basic " + cred);
            request.ContentType = "application/x-www-form-urlencoded";

            //Put data onto Request Stream 
            var byteArray = Encoding.UTF8.GetBytes(data);
            request.ContentLength = byteArray.Length;
            using (var dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }
            Trace.TraceInformation("HomeController::SendHAWebAPIRequest(): Data Written: '" + data + "'");

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var responseStreamReader = new StreamReader(response.GetResponseStream()))
                    {
                        var responseText = responseStreamReader.ReadToEnd();
                        Trace.TraceInformation("HomeController::SendHAWebAPIRequest(): Response Text: " + responseText);
                        return responseText;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("HomeController::SendHAWebAPIRequest(): Exception caught. Error when reading response to Web API request.\n" + ex.ToString());
                return ex.ToString();
            }
        }

        //converts an array of site url strings to an array containing their scm endpoint site extentions
        public string[] getSiteExtension(string[] sites)
        {
            if (sites.Length == 0)
            {
                return new string[sites.Length];
            }

            var newSiteList = new List<string>();

            foreach (var site in sites)
            {
                newSiteList.Add(getSiteExtension(site));
            }
            return newSiteList.ToArray();
        }

        //converts a url string to a url of the corresponding scm endpoint site extentions
        public string getSiteExtension(string site)
        {
            var newurl = "";

            site = NormalizeUrl(site);
            var split = new List<string>(site.Split('.'));
            split.Insert(1, "scm");
            newurl = String.Join(".", split);
            if (!newurl.EndsWith("/"))
            {
                newurl += "/";
            }
            newurl += "HighAvailabilityAPI/";
            return newurl;
        }

        //will normalize url string passed in to always start with https://
        public string NormalizeUrl(string url)
        {
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                return "https://" + url;
            if (url.StartsWith("http://"))
            {
                return "https" + url.Substring(url.IndexOf(':'));
            }
            return url;
        }

        private int getRandomNumber()
        {
            var rand = new Random();
            return (int)Math.Floor(rand.NextDouble() * 1000);
        }

        /*
        public void Killw3wp()
        {
            Trace.WriteLine("AjaxController:Killw3wp : Start");
            CloudQueue queue = SetupQueue();
            string[] arguments = new string[1];
            TestRequest request = new TestRequest(TestResult.TestType.W3WP, DateTime.UtcNow, arguments);
            CloudQueueMessage message = null;
            try
            {
                message = getSerializedMessage(request);
                queue.AddMessage(message);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }
        */
       

    }

}