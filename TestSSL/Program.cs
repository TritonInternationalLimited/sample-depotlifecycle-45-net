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

    static void Main(string[] args)
    {
        // Set the TLS version, (VS 2013 will default to TSL 1.0/1.1)
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        // Run async main manually (since VS2013 / .NET 4.5 doesn't support async Main)
        RunAsync(args).Wait();
    }

    static async Task RunAsync(string[] args)
    {
        // Set custom global certificate validation callback
        ServicePointManager.ServerCertificateValidationCallback = CertificateDebug;

        // Validate the arguments are provided
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Please provide arguments");
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
     */
    static async Task GetCurrentGateStatus(string unitNumber, string token)
    {
        // build the url
        string url = string.Format("{0}/{1}", _url, unitNumber);

        using (HttpClient client = new HttpClient())
        {
            // set request method to get
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            // add authorization header
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            // add the correct content header
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await client.SendAsync(request);

            string responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine(responseContent);
            }
            else
            {
                Console.WriteLine(string.Format("Error: {0}", response.StatusCode));
                Console.WriteLine(string.Format("Error Content: {0}", responseContent));
            }
        }
    }

    /**
     * Generates a gate entry for the given unit
     */
    static async Task PostGateStatus(string token, string redeliveryNumber, string unit)
    {
        string url = _url;

        using (HttpClient client = new HttpClient())
        {
            // set request type to post
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            // set authorization header
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            // set content type to json
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Define the request body
            var requestBody = new
            {
                // redelivery number
                adviceNumber = redeliveryNumber,
                // depot information (can be retrieved using get)
                depot = new
                {
                    companyId = "CNXIAFTRI",
                    name = "Xiamen Sanlly Container Services, Co., Ltd.",
                    code = "XIAF"
                },
                // unitNumber of the container
                unitNumber = unit,
                // status of A - undamaged, D - damaged, S - sold
                status = "A",
                // timestamp of activity
                activityTime = DateTime.UtcNow.ToString("o"),
                // type of activity IN - Gate In, OUT - Gate Out
                type = "IN",
                // replace with actual object array
                photos = new string[0]
            };

            // Serialize the request body to JSON
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
                Console.WriteLine(string.Format("Error: {0}", response.StatusCode));
                Console.WriteLine(string.Format("Error Content: {0}", responseContent));
            }
        }
    }

    /**
     * This method provides additional debugging for the certificate.
     */
    private static bool CertificateDebug(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        Console.WriteLine("SSL Certificate Validation:");
        Console.WriteLine("Subject: " + certificate.Subject);
        Console.WriteLine("Issuer: " + certificate.Issuer);
        Console.WriteLine("Effective Date: " + certificate.GetEffectiveDateString());
        Console.WriteLine("Expiration Date: " + certificate.GetExpirationDateString());
        Console.WriteLine("Policy Errors: " + sslPolicyErrors);

        // uncomment this line to turn off SSL validation
        // return true;
        return sslPolicyErrors == SslPolicyErrors.None;
    }
}
