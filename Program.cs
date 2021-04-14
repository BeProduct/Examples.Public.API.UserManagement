using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OAuth2.Helpers;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OAuth2
{
    class Program
    {
        static readonly string authUrl = "https://id.winks.io/ids";
        static readonly string clientId = "";
        static readonly string clientSecret = "";
        static readonly string companyName = "";
        static readonly string callbackUrl = "http://127.0.0.1:8888/";
        static RestClient client = new RestClient("https://developers.beproduct.com/");

        static (bool exists, string user_id, bool active) UserExists(string email, string token)
        {

            var request = new RestRequest("/api/" + companyName + "/users/getbyemail", Method.GET);
            request.AddHeader("Authorization", "Bearer " + token);
            request.AddParameter("email", email);
            var response = client.Execute<dynamic>(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                return (false, null, false);

            var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
            return (true, (string)result["id"], (bool)result["active"]);
        }

        static IEnumerable<(string id, string role)> GetRoles(string token)
        {
            var request = new RestRequest("/api/" + companyName + "/users/roles", Method.GET);
            request.AddHeader("Authorization", "Bearer " + token);
            var response = client.Execute<dynamic>(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("Could not retrieve a role list");

            var roles = JsonConvert.DeserializeObject<JArray>(response.Content);
            foreach (var r in roles)
            {
                yield return (r["id"].ToString(), r["roleName"].ToString());
            }
        }


        static string AddUser(dynamic requestBody, string token)
        {

            var request = new RestRequest("/api/" + companyName + "/users/create", Method.POST);
            request.AddHeader("Authorization", "Bearer " + token);
            request.AddJsonBody(requestBody);
            var response = client.Execute<dynamic>(request);


            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine("Coudlnt add new user:");
                Console.WriteLine(response.Content);
                throw new Exception(response.Content);
            }

            return response.Content;

        }

        static string SetUserActiveFlag(string userId, bool activeFlag, string token)
        {
            var request = new RestRequest("/api/" + companyName + "/users/" + userId + "/update", Method.POST);
            request.AddHeader("Authorization", "Bearer " + token);
            request.AddJsonBody(new
            {
                active = activeFlag
            });
            var response = client.Execute<dynamic>(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine("Coudln't update a user:");
                Console.WriteLine(response.Content);
                throw new Exception(response.Content);
            }

            return response.Content;

        }


        static void Main(string[] args)
        {
            var accessToken = Connect.Token(authUrl, clientId, clientSecret, callbackUrl);
            Console.WriteLine("Yay! You got an access token ;-)");
            Console.ReadLine();


            var email = ProvideNonEmpty("Enter email");

            var status = UserExists(email, accessToken);
            if (status.exists)
            {

                Console.WriteLine($"User with email {email} already exists.");
                if (status.active)
                {
                    if (Ask("User is active. Do you want to deactivate?"))
                    {
                        SetUserActiveFlag(status.user_id, false, accessToken);
                    }
                }
                else
                {
                    if (Ask("User is not active. Do you want to activate?"))
                    {
                        SetUserActiveFlag(status.user_id, true, accessToken);
                    }
                }

            }
            else if (Ask("User doesn't exist. Do you want to add a new user?"))
            {

                var roles = GetRoles(accessToken);
                var user = new
                {
                    username = ProvideNonEmpty("Enter username"),
                    email,
                    firstName = ProvideNonEmpty("Enter Firstname"),
                    lastName = ProvideNonEmpty("Enter Lastname"),
                    culture = "En-Us",
                    title = "",
                    roleId = roles.First(r => r.role.Equals(ProvideNonEmpty("Enter user role", roles.Select(_ => _.role)), StringComparison.OrdinalIgnoreCase)).id
                };

                var new_user = AddUser(user, accessToken);
                Console.WriteLine("User was added");

            }

            Console.WriteLine("Have a nice day!");
            Console.ReadLine();


        }




        #region HELPER FUNCTIONS
        static string ProvideNonEmpty(string question, IEnumerable<string> possibleValues = null)
        {
            Console.Write(question + ": ");
            string answer;
            while (true)
            {
                answer = Console.ReadLine().Trim();
                if (string.IsNullOrEmpty(answer))
                {
                    Console.WriteLine("Reuired field!");
                    continue;
                }

                if (possibleValues != null && possibleValues.Any() && !possibleValues.Contains(answer, StringComparer.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Field must be one of the next values:" + string.Join(",", possibleValues));
                    continue;
                }

                break;

            }
            return answer;
        }
        static bool Ask(string question)
        {
            Console.WriteLine(question + " y/n");
            string answer;
            do
            {
                answer = Console.ReadLine();
                if (answer.ToLower() == "y")
                    return true;
                if (answer.ToLower() == "n")
                    return false;

                Console.WriteLine("Type 'y' or 'n'");

            } while (!new[] { "y", "n" }.Contains(answer.ToLower()));

            return false;
        }
        #endregion

    }
}
