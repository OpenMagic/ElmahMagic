using System;
using System.Collections;
using System.Configuration;
using System.Linq;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Extensions;
using Raven.Client.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Elmah
{
    public class RavenDbErrorLog : ErrorLog
    {
        private readonly string _connectionStringName;

        private IDocumentStore _documentStore;

        public RavenDbErrorLog(IDictionary config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            _connectionStringName = GetConnectionStringName(config);
            LoadApplicationName(config);
            InitDocumentStore();
        }

        public override string Name
        {
            get { return "RavenDB Error Log"; }
        }

        public override string Log(Error error)
        {
            if (error == null)
            {
                throw new ArgumentNullException("error");
            }

            error.Time = error.Time.ToUniversalTime();

            var errorDocument = error.MapToErrorDocument();
            
            using (var session = _documentStore.OpenSession())
            {
                session.Store(errorDocument);
                session.SaveChanges();
            }

            return errorDocument.Id;
        }
 
        public override ErrorLogEntry GetError(string id)
        {
            ErrorDocument document;

            using (var session = _documentStore.OpenSession())
            {
                document = session.Load<ErrorDocument>(id);
            }

            var error = document.MapToError();
            var logEntry = new ErrorLogEntry(this, id, error);

            return logEntry;
        }

        public override int GetErrors(int pageIndex, int pageSize, IList errorEntryList)
        {
            using (var session = _documentStore.OpenSession())
            {
                RavenQueryStatistics stats;

                IQueryable<ErrorDocument> result 
                           = session.Query<ErrorDocument>()
                                    .Statistics(out stats)
                                    .Skip(pageSize * pageIndex)
                                    .Take(pageSize)
                                    .OrderByDescending(c => c.Time);

                if (!string.IsNullOrWhiteSpace(ApplicationName))
                {
                    result = result.Where(x => x.ApplicationName == ApplicationName);
                }

                foreach (var errorDocument in result)
                {
                    var error = errorDocument.MapToError();
                    errorEntryList.Add(new ErrorLogEntry(this, errorDocument.Id, error));
                }

                return stats.TotalResults;
            }
        }

        private void LoadApplicationName(IDictionary config)
        {
            // Set the application name as this implementation provides
            // per-application isolation over a single store.
            var appName = string.Empty;
            if (config["applicationName"] != null)
            {
                appName = (string)config["applicationName"];
            }

            ApplicationName = appName;
        }

        private string GetConnectionStringName(IDictionary config)
        {
            var connectionString = LoadConnectionStringName(config);

            //
            // If there is no connection string to use then throw an 
            // exception to abort construction.
            //

            if (connectionString.Length == 0)
                throw new ApplicationException("Connection string is missing for the RavenDB error log.");

            return connectionString;
        }

        private void InitDocumentStore()
        {
            _documentStore = new DocumentStore
            {
                ConnectionStringName = _connectionStringName
            };

            _documentStore.Initialize();
        }

        private string LoadConnectionStringName(IDictionary config)
        {
            // From ELMAH source
            // First look for a connection string name that can be 
            // subsequently indexed into the <connectionStrings> section of 
            // the configuration to get the actual connection string.

            string connectionStringName = (string)config["connectionStringName"];

            if (!string.IsNullOrEmpty(connectionStringName))
            {
                var settings = ConfigurationManager.ConnectionStrings[connectionStringName];

                if (settings != null)
                    return connectionStringName;

                throw new ApplicationException(string.Format("Could not find a ConnectionString with the name '{0}'.", connectionStringName));
            }

            throw new ApplicationException("You must specifiy the 'connectionStringName' attribute on the <errorLog /> element.");
        }
    }
}