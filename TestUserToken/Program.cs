using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.ServiceModel.Security.Tokens;
using NLog;
using TestUserToken.ActivityService;
using TestUserToken.AuthenticationService;
using TestUserToken.ProjectService;


namespace TestUserToken
{
    public class Program
    {
        public String WcfRequest { get; set; }

        public String WcfResponse { get; set; }

        public static Logger Nlogger = LogManager.GetCurrentClassLogger();


        public static void Main(string[] args)
        {
            var userToken = new Program
            {
                WcfRequest = String.Empty,
                WcfResponse = String.Empty
            };

            userToken.RunTest();
        }

        /// <summary>
        /// Runs the test.
        /// </summary>
        public void RunTest()
        {
            try
            {
                //
                // P6 testing variables
                //
                const string projectId = "EC00515";
                const string activityId = "EC1230";

                //
                // By pass my self signed cert.
                //
                ServicePointManager.DefaultConnectionLimit = 9999;
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };


                //<DatabaseInstanceId xmlns="http://xmlns.oracle.com/Primavera/P6/V7/WS/Authentication">1</DatabaseInstanceId>
                // P6 project service endpoint
                //
                const string projectServiceEndpoint = "https://oyster.gnet:8209/p6ws/services/ProjectService";
                const string activiyServiceEndpoint = "https://oyster.gnet:8209/p6ws/services/ActivityService";
                const string authenticationServiceEndpoint = "https://oyster.gnet:8209/p6ws/services/AuthenticationService";

                const string userName = "admin";
                const string password = "eneG6242";

                var authenticationService = CreateAuthenticationServiceClient(authenticationServiceEndpoint, userName, password);
                var readDatabaseInstances = authenticationService.ReadDatabaseInstances(new object());

                var databaseInstanceId = 0;
                foreach (var readDatabaseInstance in readDatabaseInstances)
                {
                    Console.WriteLine("DatabaseName {0} with DatabaseInstanceId = {1}", readDatabaseInstance.DatabaseName, readDatabaseInstance.DatabaseInstanceId);
                    if (readDatabaseInstance.DatabaseName.StartsWith("P6Demo")) databaseInstanceId = readDatabaseInstance.DatabaseInstanceId;
                }

                //var login = new Login {UserName = userName, Password = password, DatabaseInstanceId = databaseInstanceId, DatabaseInstanceIdSpecified = true};
                //var loginResponse = authenticationService.Login(login);


                //var readSessions = authenticationService.ReadSessionProperties(new object());

                //
                // Create a client proxy for ProjectService
                //
                var projectService = CreateProjectServiceClient(projectServiceEndpoint, userName, password);
                var activityService = CreateActivityServiceClient(activiyServiceEndpoint, userName, password);

                var eab = new EndpointAddressBuilder(projectService.Endpoint.Address);
                var addressHeader = AddressHeader.CreateAddressHeader("DatabaseInstanceId", "http://xmlns.oracle.com/Primavera/P6/WS/Authentication/V1", databaseInstanceId.ToString(CultureInfo.InvariantCulture));
                eab.Headers.Add(addressHeader);
                projectService.Endpoint.Address = eab.ToEndpointAddress();



                var readProjects = DefaultProjectFields();
                readProjects.Filter = String.Format("Id = '{0}'", projectId);


                //
                // Read our projects
                //
                var readProject = projectService.ReadProjects(readProjects);
                if (readProject.Length > 0)
                Console.WriteLine("readProject has {0} project with code {1} last one named named {2}", readProject.Length, readProject[readProject.Length-1].Id, readProject[readProject.Length-1].Name);


                var readActivities = DefaultActivityFields();
                readActivities.Filter = String.Format("Project Code = {0} and Id ='{1}'", readProject[0].Id, activityId);

                var readActivityTest = activityService.ReadActivities(readActivities);

                foreach (var activity in readActivityTest)
                {
                    Console.WriteLine(activity.Name);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Defaults the fields used for our ReadProject.
        /// </summary>
        /// <returns>ReadProjects.</returns>
        public ReadProjects DefaultProjectFields()
        {
            var fields = new List<ProjectFieldType>
                             {
                                 ProjectFieldType.ObjectId,
                                 ProjectFieldType.Id,
                                 ProjectFieldType.Name,
                                 ProjectFieldType.Status,
                                 ProjectFieldType.StartDate,
                                 ProjectFieldType.FinishDate,
                                 ProjectFieldType.DataDate,
                             };
            var defaultFields = new ReadProjects { Field = fields.ToArray() };
            return defaultFields;
        }

        /// <summary>
        /// Defaults the activity fields.
        /// </summary>
        /// <returns>ReadActivities.</returns>
        public ReadActivities DefaultActivityFields()
        {
            var defaultFields = new ReadActivities();
            var fields = new List<ActivityFieldType>
                             {
                                 ActivityFieldType.ObjectId,
                                 ActivityFieldType.Id,
                                 ActivityFieldType.Name,
                                 ActivityFieldType.Status,
                                 ActivityFieldType.ActualDuration,
                                 ActivityFieldType.StartDate,
                                 ActivityFieldType.FinishDate,
                                 ActivityFieldType.BaselineStartDate,
                                 ActivityFieldType.BaselineFinishDate,
                                 ActivityFieldType.DataDate,
                                 ActivityFieldType.ProjectObjectId
                             };
            defaultFields.Field = fields.ToArray();
            return defaultFields;
        }

        /// <summary>
        /// Creates the project service client proxy.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>ProjectPortTypeClient.</returns>
        public ProjectPortTypeClient CreateProjectServiceClient(String url, String username, String password)
        {
            if (String.IsNullOrEmpty(url)) url = "https://localhost:8209/p6ws/services/ProjectService";

            var p6BindingCredentials = new P6BindingCredentials(username, password);

            //
            // Define our ProjectService client with P6 credentials
            //
            var projectServiceClient = new ProjectPortTypeClient(p6BindingCredentials.Binding, new EndpointAddress(url));
            projectServiceClient.ChannelFactory.Endpoint.Behaviors.Remove<System.ServiceModel.Description.ClientCredentials>();
            projectServiceClient.ChannelFactory.Endpoint.Behaviors.Add(p6BindingCredentials.Credentials);

            var cb = new MessageInspectorBehavior();

            // Add the custom behaviour to the list of service behaviours.
            projectServiceClient.Endpoint.Behaviors.Add(cb);

            // Subscribe to message inpection events and provess the event invokation.
            cb.OnMessageInspected += (src, e) =>
            {
                if (e.MessageInspectionType == EMessageInspectionType.Request) WcfRequest = e.Message;
                else WcfResponse = e.Message;
            };


            return projectServiceClient;
        }


        /// <summary>
        /// Creates the activity service client.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>ActivityPortTypeClient.</returns>
        public static ActivityPortTypeClient CreateActivityServiceClient(String url, String username, String password)
        {
            if (String.IsNullOrEmpty(url)) url = "https://localhost:8209/p6ws/services/ActivityService";

            var p6BindingCredentials = new P6BindingCredentials(username, password);

            //
            // Define our ProjectService client with P6 credentials
            //
            var activityServiceClient = new ActivityPortTypeClient(p6BindingCredentials.Binding, new EndpointAddress(url));
            activityServiceClient.ChannelFactory.Endpoint.Behaviors.Remove<System.ServiceModel.Description.ClientCredentials>();
            activityServiceClient.ChannelFactory.Endpoint.Behaviors.Add(p6BindingCredentials.Credentials);

            return activityServiceClient;
        }


        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static AuthenticationServicePortTypeClient CreateAuthenticationServiceClient(String url, String username, String password)
        {
            if (String.IsNullOrEmpty(url)) url = "https://localhost:8209/p6ws/services/AuthenticationService";

            var oysterGnetCertificate = GetX509Certificate("oyster.gnet");
            var p6BindingCredentials = new P6BindingCredentials(username, password);
            p6BindingCredentials.Credentials.TransportCertificate = oysterGnetCertificate;

            //
            // Define our ProjectService client with P6 credentials
            //  

            var authenticationServiceClient = new AuthenticationServicePortTypeClient(p6BindingCredentials.Binding, new EndpointAddress(url));
            authenticationServiceClient.ChannelFactory.Endpoint.Behaviors.Remove<ClientCredentials>();
            authenticationServiceClient.ChannelFactory.Endpoint.Behaviors.Add(p6BindingCredentials.Credentials);


            authenticationServiceClient.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.PeerOrChainTrust;
            authenticationServiceClient.ClientCredentials.ServiceCertificate.Authentication.TrustedStoreLocation = StoreLocation.LocalMachine;

            authenticationServiceClient.ClientCredentials.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, X509FindType.FindBySubjectName, "oyster.gnet");
            authenticationServiceClient.ClientCredentials.ServiceCertificate.SetDefaultCertificate(StoreLocation.LocalMachine, StoreName.My, X509FindType.FindBySubjectName, "oyster.gnet");


            return authenticationServiceClient;
        }

        private static X509Certificate2 GetX509Certificate(String certificateName)
        {

            var storex = new X509Store(StoreLocation.LocalMachine);
            storex.Open(OpenFlags.ReadOnly);
            var certificatesx = storex.Certificates.Find(X509FindType.FindBySubjectName, certificateName, true);
            
            var retVal = new X509Certificate2();
            if (certificatesx.Count > 0) retVal = certificatesx[0];
            
            storex.Close();
            return retVal;
        }
    }
}
