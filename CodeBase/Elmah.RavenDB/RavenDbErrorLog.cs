using System;
using System.Collections;
using System.Configuration;
using System.Linq;
using Common.Logging;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Linq;

namespace Elmah
{
    public class RavenDbErrorLog : ErrorLog
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private readonly string _connectionStringName;
        private IDocumentStore _documentStore;

        public RavenDbErrorLog(IDictionary config)
        {
            Logger.Trace("RavenDbErrorLog(config)");

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
            Logger.Trace(string.Format("error: {0}", error));

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
            Logger.Trace(string.Format("GetError(id: {0})", id));

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
            Logger.Trace(string.Format("GetErrors(pageIndex: {0}, pageSize: {0}, errorEntryList", pageIndex, pageSize));

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
            Logger.Trace("LoadApplicationName(config)");

            // Set the application name as this implementation provides
            // per-application isolation over a single store.
            var appName = string.Empty;
            if (config["applicationName"] != null)
            {
                appName = (string)config["applicationName"];
            }

            Logger.Debug(string.Format("ApplicationName: {0}", appName));
            ApplicationName = appName;
        }

        private string GetConnectionStringName(IDictionary config)
        {
            Logger.Trace(string.Format("GetConnectionStringName(config)"));

            var connectionString = LoadConnectionStringName(config);

            //
            // If there is no connection string to use then throw an 
            // exception to abort construction.
            //

            if (connectionString.Length == 0)
                throw new ApplicationException("Connection string is missing for the RavenDB error log.");

            Logger.Debug(string.Format("Connection string name: {0}", connectionString));
            return connectionString;
        }

        private void InitDocumentStore()
        {
            Logger.Trace("InitDocumentStore()");            
            
            _documentStore = new DocumentStore
            {
                ConnectionStringName = _connectionStringName
            };

            _documentStore.Initialize();
        }

        private string LoadConnectionStringName(IDictionary config)
        {
            Logger.Trace("LoadConnectionStringName(config)");

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