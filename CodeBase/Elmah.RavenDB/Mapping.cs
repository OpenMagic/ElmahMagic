using System.Collections.Generic;
using System.Collections.Specialized;
using Common.Logging;

namespace Elmah
{
    public static class Mapping
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public static void AddToNameValueCollection(this IList<KeyValuePair<string, string>> list, NameValueCollection collection)
        {
            Log.Trace(string.Format("AddToNameValueCollection(list, collection)"));

            foreach (var keyValuePair in list)
            {
                collection.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }

        public static Error MapToError(this ErrorDocument errorDocument)
        {
            Log.Trace("MapToError(errorDocument)");
            
            var error = new Error(errorDocument.Exception)
            {
                ApplicationName = errorDocument.ApplicationName,
                Detail = errorDocument.Detail,
                HostName = errorDocument.HostName,
                Message = errorDocument.Message,
                Source = errorDocument.Source,
                StatusCode = errorDocument.StatusCode,
                Time = errorDocument.Time,
                Type = errorDocument.Type,
                User = errorDocument.User,
                WebHostHtmlMessage = errorDocument.WebHostHtmlMessage
            };

            errorDocument.Cookies.AddToNameValueCollection(error.Cookies);
            errorDocument.Form.AddToNameValueCollection(error.Form);
            errorDocument.QueryString.AddToNameValueCollection(error.QueryString);
            errorDocument.ServerVariables.AddToNameValueCollection(error.ServerVariables);

            return error;
        }

        public static ErrorDocument MapToErrorDocument(this Error error)
        {
            Log.Trace("MapToErrorDocument");

            return new ErrorDocument
            {
                ApplicationName = error.ApplicationName,
                Cookies = error.Cookies.MapToListOfKeyValuePair(),
                Detail = error.Detail,
                Exception = error.Exception,
                Form = error.Form.MapToListOfKeyValuePair(),
                HostName = error.HostName,
                Message = error.Message,
                QueryString = error.QueryString.MapToListOfKeyValuePair(),
                ServerVariables = error.ServerVariables.MapToListOfKeyValuePair(),
                Source = error.Source,
                StatusCode = error.StatusCode,
                Time = error.Time,
                Type = error.Type,
                User = error.User,
                WebHostHtmlMessage = error.WebHostHtmlMessage
            };
        }

        public static IList<KeyValuePair<string, string>> MapToListOfKeyValuePair(this NameValueCollection collection)
        {
            Log.Trace("MapToListOfKeyValuePair(collection)");

            var list = new List<KeyValuePair<string, string>>();

            for (int i = 0; i < collection.Count; i++)
            {
                list.Add(new KeyValuePair<string, string>(collection.Keys[i], collection[i]));
            }

            return list;
        }

    }
}
