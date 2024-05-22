using Newtonsoft.Json;

namespace Annoy_o_Bot.GitHub.Callbacks
{
    public class RequestParser
    {
        public static CallbackModel ParseJson(string requestBody)
        {
            var requestObject = JsonConvert.DeserializeObject<CallbackModel>(requestBody);
            return requestObject;
        }
    }
}