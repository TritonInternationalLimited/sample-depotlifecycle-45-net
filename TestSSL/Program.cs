using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using System.Net.Security;
using System.Net;

/**
 * Takes in 2-3 arguments
 * Argument 1 mode - get or create
 * Argument 2 unit number - the unit number to get or create
 * Argument 3 (create only) redelivery number - the redelivery number to create
 *
 * Sample Get Input: get TCKU6034863
 * Sample Create Input: create TCKU6034863 AXIAF32029
 */
class TestSSL
{
    private static string _url = "https://testapi.trtn.com/triton/api/v2/gate";

    static async Task Main(string[] args)
    {
        // Set custom global sertificate validaiton callback
        ServicePointManager.ServerCertificateValidationCallback = CertificateDebug;

        // Validate the arguments are provided
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Please provide arugements");
            return;
        }

        string mode = args[0];
        string unitNumber = args[1];
        string token = Environment.GetEnvironmentVariable("TRITON_API_TOKEN");

        // validate the unit number exists
        if (string.IsNullOrEmpty(unitNumber))
        {
            Console.WriteLine("Error: Unit number cannot be null or empty.");
            return;
        }

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Error: TRITON_API_TOKEN environment variable is not set.");
            return;
        }

        switch (mode)
        {
            case "get":
                await GetCurrentGateStatus(unitNumber, token);
                break;

            case "create":
                if (args.Length < 3)
                {
                    Console.WriteLine("Error: Please provide advice number and unit number for post.");
                    return;
                }

                string adviceNumber = args[2];
                await PostGateStatus(token, adviceNumber, unitNumber);
                break;

            default:
                Console.WriteLine("Error: Invalid mode. Use 'get' or 'create'.");
                break;
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    /**
     * Gets the gate status for the given unit
     * Sample Response:
     *
        {
          "adviceNumber" : "AXIAF32007",
          "depot" : {
            "companyId" : "CNXIAFTRI",
            "name" : "Xiamen Sanlly Container Services, Co., Ltd.",
            "code" : "XIAF"
          },
          "status" : "A",
          "activityTime" : "2025-03-12T00:00:00-04:00",
          "currentExchangeRate" : 1.0,
          "currentInspectionCriteria" : "IICL",
          "type" : "IN"
        }
     */
    static async Task GetCurrentGateStatus(string unitNumber, string token)
    {
        string url = $"{_url}/{unitNumber}";

        using (HttpClient client = new HttpClient())
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await client.SendAsync(request);

            string responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine(responseContent);
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                Console.WriteLine($"Error Content: {responseContent}");
            }
        }
    }

    /**
     * Generates a gate entry for the given unit
     * Sample response:
        {
        "adviceNumber" : "AXIAF32029",
        "customerReference" : "CSCX57-400000",
        "transactionReference" : "AXIAF32029",
        "insuranceCoverage" : {
        "amountCovered" : 99999.00,
        "amountCurrency" : "USD",
        "allOrNothing" : false,
        "appliesToCTL" : false,
        "exclusions" : [ "Standard Exclusions Apply" ]
        },
        "currentExchangeRate" : 1.0,
        "comments" : [ "Please use each unit's Inspection Category to determine what type of primary estimate to send." ],
        "currentInspectionCriteria" : "IICL"
        }
     */
    static async Task PostGateStatus(string token, string redeliveryNumber, string unit)
    {
        string url = _url;

        using (HttpClient client = new HttpClient())
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var requestBody = new
            {
                adviceNumber = redeliveryNumber,
                depot = new
                {
                    companyId = "CNXIAFTRI",
                    name = "Xiamen Sanlly Container Services, Co., Ltd.",
                    code = "XIAF"
                },
                unitNumber = unit,
                status = "A",
                activityTime = DateTime.UtcNow.ToString("o"),
                type = "IN",
                photos = new string[0]
            };

            string jsonBody = JsonConvert.SerializeObject(requestBody);
            request.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.SendAsync(request);

            string responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Post successful.");
                Console.WriteLine(responseContent);
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                Console.WriteLine($"Error Content: {responseContent}");
            }
        }
    }

    private static bool CertificateDebug(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        Console.WriteLine("SSL Certificate Validation:");
        Console.WriteLine($"Subject: {certificate.Subject}");
        Console.WriteLine($"Issuer: {certificate.Issuer}");
        Console.WriteLine($"Effective Date: {certificate.GetEffectiveDateString()}");
        Console.WriteLine($"Expiration Date: {certificate.GetExpirationDateString()}");
        Console.WriteLine($"Policy Errors: {sslPolicyErrors}");

        // uncomment this line to turn off SSL validation
        // return true;
        return sslPolicyErrors == SslPolicyErrors.None;
    }
}
