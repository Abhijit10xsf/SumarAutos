using Newtonsoft.Json;
using RestSharp;
using SumarAuto.Client.Helper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;

namespace SumarAuto.Client
{
        public class SAPRestServiceLayer
        {
            public string baseUrl;
            private LoginRequest login;   // <-- correct variable name
            private string sessionId;
            private string routeId;

            public SAPRestServiceLayer()
            {
                baseUrl = ConfigurationManager.AppSettings["CurrentServiceURL"];

                // default (can be overridden)
                login = new LoginRequest()
                {
                    CompanyDB = ConfigurationManager.AppSettings["CompanyDB"],
                    UserName = ConfigurationManager.AppSettings["UserName"],
                    Password = ConfigurationManager.AppSettings["Password"]
                };
            }




            // 🔥 ALLOW LOGIN USING USER ENTERED CREDENTIALS
            public void SetCredentials(string userName, string password)
            {
                login.UserName = userName;
                login.Password = password;
            }

            // 🔥 SERVICE LAYER LOGIN
            public bool LoginRequest()
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback +=
                        (sender, cert, chain, sslPolicyErrors) => true;

                    var client = new RestClient(baseUrl + "/Login");
                    client.Timeout = -1;

                    var request = new RestRequest(Method.POST);
                    request.AddHeader("Content-Type", "application/json");

                    var body = JsonConvert.SerializeObject(login);
                    request.AddParameter("application/json", body, ParameterType.RequestBody);

                    var response = client.Execute(request);

                    if (!response.IsSuccessful)
                        throw new Exception("Service Layer Login Failed: " + response.Content);

                    var resp = JsonConvert.DeserializeObject<LoginResponse>(response.Content);

                    // Extract ROUTEID cookie
                    resp.RouteId = response.Cookies
                                    .Where(x => x.Name == "ROUTEID")
                                    .FirstOrDefault()?.Value;

                    // Save in fields
                    sessionId = resp.SessionId;
                    routeId = resp.RouteId;

                    // Save in MVC session
                    HttpContext.Current.Session["sessionId"] = sessionId;
                    HttpContext.Current.Session["routeId"] = routeId;

                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Utilities.SetResultMessage("Service Layer Login Error: " + ex.Message);
                    return false;
                }

                return true;
            }



            public bool ValidateUserCredentials()
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback +=
                        (sender, cert, chain, sslPolicyErrors) => true;

                    var client = new RestClient(baseUrl + "/Login");
                    client.Timeout = -1;

                    var loginBody = new LoginRequest()
                    {
                        CompanyDB = ConfigurationManager.AppSettings["CompanyDB"],
                        UserName = ConfigurationManager.AppSettings["UserName"], //loginData.UserName,
                        Password = ConfigurationManager.AppSettings["Password"] //loginData.Password
                    };

                    var request = new RestRequest(Method.POST);
                    request.AddHeader("Content-Type", "application/json");

                    var body = JsonConvert.SerializeObject(loginBody);
                    request.AddParameter("application/json", body, ParameterType.RequestBody);

                    var response = client.Execute(request);

                    // Success means the credentials are valid and a new temporary session was created.
                    if (!response.IsSuccessful)
                        throw new Exception("Invalid Credentials: " + response.Content);
                    var resp = JsonConvert.DeserializeObject<LoginResponse>(response.Content);
                    // Extract ROUTEID cookie
                    resp.RouteId = response.Cookies
                                    .Where(x => x.Name == "ROUTEID")
                                    .FirstOrDefault()?.Value;
                    HttpContext.Current.Session["USER_SESSION"] = resp.SessionId;
                    HttpContext.Current.Session["USER_ROUTE"] = resp.RouteId;

                    LoginRequest();

                    HttpContext.Current.Session["LoggedUser"] = loginBody.UserName;//loginData.UserName;
                    HttpContext.Current.Session["LoggedPW"] = loginBody.Password; //loginData.Password;
                    int loggedUserId = GetUserIdFromServiceLayer(loginBody.UserName);
                    HttpContext.Current.Session["LoggedUserId"] = loggedUserId;

                    ////Approvals
                    // HttpContext.Current.Session["LoggedUserId"] = 323;
                    ////Reports
                    //HttpContext.Current.Session["LoggedUserId"] = 61;


                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    //Utilities.SetResultMessage("Service Layer Login Error: " + ex.Message);
                    //return false;
                    Utilities.SetResultMessage(ex.Message.ToString());
                    return false;

                }

                return true; // Credentials validated successfully
            }
            public int GetUserIdFromServiceLayer(string userCode)
            {
                try
                {
                    var client = new RestClient(
                        baseUrl + $"/Users?$select=InternalKey&$filter=UserCode eq '{userCode}'");

                    var request = new RestRequest(Method.GET);
                    request.AddHeader("Content-Type", "application/json");

                    // 🔥 READ SESSION VALUES CORRECTLY
                    string sessionId = HttpContext.Current.Session["sessionId"]?.ToString();
                    string routeId = HttpContext.Current.Session["routeId"]?.ToString();

                    if (string.IsNullOrEmpty(sessionId))
                        throw new Exception("SAP session not found. Please login again.");

                    request.AddCookie("B1SESSION", sessionId);
                    request.AddCookie("ROUTEID", routeId);

                    var response = client.Execute(request);
                    if (!response.IsSuccessful)
                        throw new Exception("Failed to fetch UserID: " + response.Content);

                    dynamic result = JsonConvert.DeserializeObject(response.Content);

                    if (result.value == null || result.value.Count == 0)
                        throw new Exception("User not found in SAP: " + userCode);

                    return (int)result.value[0].InternalKey;
                }
                catch (Exception ex)
                {
                    throw new Exception("GetUserIdFromServiceLayer Error: " + ex.Message);
                }
            }

            public string Post(string jsonBody)
            {
                var endpoint = baseUrl + "/ApprovalRequests";

                var client = new RestClient(endpoint);
                var request = new RestRequest(Method.POST);

                request.AddHeader("Content-Type", "application/json");

                request.AddCookie("B1SESSION", HttpContext.Current.Session["sessionId"].ToString());
                request.AddCookie("ROUTEID", HttpContext.Current.Session["routeId"].ToString());

                request.AddParameter("application/json", jsonBody, ParameterType.RequestBody);

                var response = client.Execute(request);
                if (!response.IsSuccessful)
                    throw new Exception("POST Failed: " + response.Content);

                return response.Content;
            }
            public DataTable GetUserDetails(string userCode)
            {
                DataHelper dh = new DataHelper();
                string query = $@"
        SELECT ""E_Mail"", ""PortNum""
        FROM ""OUSR""
        WHERE ""USER_CODE"" = '{userCode}'";

                return dh.ExecuteDataTable(query); // your existing method
            }
            public bool ApproveRejectRequest(int wddCode, string decision, string remarks, string userSession, string userRoute)
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback +=
                        (sender, cert, chain, sslPolicyErrors) => true;

                    var endpoint = baseUrl + $"/ApprovalRequests({wddCode})";
                    //var endpoint = baseUrl + $"/ApprovalRequests?$filter=DraftEntry eq {DraftEntry}";
                    var client = new RestClient(endpoint);

                    //var userSession = HttpContext.Current.Session["USER_SESSION"]?.ToString();
                    //var userRoute = HttpContext.Current.Session["USER_ROUTE"]?.ToString();

                    if (string.IsNullOrEmpty(userSession) || string.IsNullOrEmpty(userRoute))
                        throw new Exception("User session not found. Please login first.");

                    var getReq = new RestRequest(Method.GET);
                    getReq.AddHeader("Content-Type", "application/json");
                    getReq.AddCookie("B1SESSION", userSession);
                    getReq.AddCookie("ROUTEID", userRoute);

                    var getResp = client.Execute(getReq);
                    if (!getResp.IsSuccessful)
                        throw new Exception("Failed to fetch workflow: " + getResp.Content);


                    string status;

                    if (decision == "Y")
                        status = "ardApproved";
                    else if (decision == "N")
                        status = "ardNotApproved";
                    else
                        throw new Exception("Invalid decision value. Use 'Y' or 'N'.");

                    var patchReq = new RestRequest(Method.PATCH);
                    patchReq.AddHeader("Content-Type", "application/json");
                    patchReq.AddCookie("B1SESSION", userSession);
                    patchReq.AddCookie("ROUTEID", userRoute);

                    var payload = new
                    {
                        ApprovalRequestDecisions = new[]
                     {
            new
            {
                Status = status,
                Remarks = remarks
            }
        }
                    };
                    patchReq.AddParameter(
                        "application/json",
                        JsonConvert.SerializeObject(payload),
                        ParameterType.RequestBody
                    );

                    var patchResp = client.Execute(patchReq);

                    if (patchResp.StatusCode == HttpStatusCode.NoContent)
                        return true;

                    throw new Exception("Approval failed: " + patchResp.Content);
                }
                catch (Exception ex)
                {
                    Utilities.SetResultMessage("Approval Error: " + ex.Message);
                    return false;
                }
            }


            //--------------------Create SO-----------------------
            public SalesOrderCreationResult CreateSalesOrder(object requestData)
            {
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, cert, chain, sslPolicyErrors) => true;

                var client = new RestClient($"{baseUrl}/Orders");

                var request = new RestRequest(Method.POST);

                request.AddHeader("Content-Type", "application/json");

                string sess = HttpContext.Current?.Session?["sessionId"]?.ToString() ?? sessionId;
                string rout = HttpContext.Current?.Session?["routeId"]?.ToString() ?? routeId;

                if (string.IsNullOrEmpty(sess))
                {
                    LoginRequest();
                    sess = HttpContext.Current?.Session?["sessionId"]?.ToString() ?? sessionId;
                    rout = HttpContext.Current?.Session?["routeId"]?.ToString() ?? routeId;
                }

                request.AddCookie("B1SESSION", sess);
                if (!string.IsNullOrEmpty(rout))
                {
                    request.AddCookie("ROUTEID", rout);
                }

                string jsonBody =
                    JsonConvert.SerializeObject(requestData);

                request.AddParameter(
                    "application/json",
                    jsonBody,
                    ParameterType.RequestBody
                );

                IRestResponse response = client.Execute(request);

                if (!response.IsSuccessful)
                {
                    throw new Exception(
                        $"Failed to create Sales Order. " +
                        $"Status: {response.StatusCode}, " +
                        $"Error: {response.Content}"
                    );
                }

                dynamic result =
                    JsonConvert.DeserializeObject(response.Content);

                return new SalesOrderCreationResult
                {
                    DocEntry = (int)result.DocEntry,
                    DocNum = (int)result.DocNum
                };
            }

        }

        public class SalesOrderCreationResult
        {
            public int DocEntry { get; set; }
            public int DocNum { get; set; }
        }
        // MODELS
        public class LoginRequest
        {
            public string CompanyDB { get; set; }
            public string Password { get; set; }
            public string UserName { get; set; }
        }

        public class LoginResponse : Response
        {
            public string SessionId { get; set; }
            public string Version { get; set; }
            public string RouteId { get; set; }
        }

        public class Response
        {
            public Error error { get; set; }
        }

        public class Error
        {
            public int code { get; set; }
            public string message { get; set; }
        }
}