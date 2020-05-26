using Newtonsoft.Json;

namespace Annoy_o_Bot
{
    public class RequestParser
    {
        public static CallbackModel ParseJson(string requestBody)
        {
            CallbackModel requestObject = JsonConvert.DeserializeObject<CallbackModel>(requestBody);
            return requestObject;
        }
    }
}