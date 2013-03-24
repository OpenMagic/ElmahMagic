using System;
using System.Collections.Generic;

namespace Elmah 
{
    public class ErrorDocument 
    {
        public string Id { get; set; }
        public string ApplicationName { get; set; }
        public IList<KeyValuePair<string, string>> Cookies { get; set; }
        public string Detail { get; set; }
        public Exception Exception { get; set; }
        public IList<KeyValuePair<string, string>> Form { get; set; }
        public string HostName { get; set; }
        public string Message { get; set; }
        public IList<KeyValuePair<string, string>> QueryString { get; set; }
        public IList<KeyValuePair<string, string>> ServerVariables { get; set; }
        public string Source { get; set; }
        public int StatusCode { get; set; }
        public DateTime Time { get; set; }
        public string Type { get; set; }
        public string User { get; set; }
        public string WebHostHtmlMessage { get; set; }

        public DateTime? Reviewed { get; set; }
        public DateTime? Resolved { get; set; }

        public string ErrorXml { get; set; }
    }
}