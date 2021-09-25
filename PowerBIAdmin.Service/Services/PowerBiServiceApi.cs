using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.PowerBI.Api.Models.Credentials;
using Microsoft.Rest;
using Microsoft.Identity.Web;
using PowerBIAdmin.Repository.Models;
using PowerBIAdmin.Service.DataTable;
using Microsoft.Identity.Client;

namespace PowerBIAdmin.Service 
{

    public class EmbeddedReportViewModel
    {
        public string ReportId;
        public string Name;
        public string EmbedUrl;
        public string Token;
        public string TenantName;
    }

    public class PowerBiTenantDetails : PowerBiWorkspace
    {
        public IList<Report> Reports { get; set; }
        public IList<Dataset> Datasets { get; set; }
        public IList<GroupUser> Members { get; set; }
    }

    public class PowerBiServiceApi
    {
        private readonly AppOwnsDataDBService AppOwnsDataDBService;
        private readonly IConfiguration Configuration;
        private readonly IWebHostEnvironment Env;

        private ITokenAcquisition tokenAcquisition { get; }
        private string urlPowerBiServiceApiRoot { get; }

        public PowerBiServiceApi(IConfiguration configuration, ITokenAcquisition tokenAcquisition, AppOwnsDataDBService AppOwnsDataDBService, IWebHostEnvironment env)
        {
            this.Configuration = configuration;
            this.urlPowerBiServiceApiRoot = configuration["PowerBi:ServiceRootUrl"];
            this.tokenAcquisition = tokenAcquisition;
            this.AppOwnsDataDBService = AppOwnsDataDBService;
            this.Env = env;
        }

        public const string powerbiApiDefaultScope = "https://analysis.windows.net/powerbi/api/.default";

        public string GetAccessToken()
        {
            string accessToken = "";
            try
            {
                accessToken = tokenAcquisition.GetAccessTokenForAppAsync(powerbiApiDefaultScope).Result;
            }
            catch(Exception ex)
            {
                accessToken = GetAccessTokenWithServicePrincipal();
            }
            return accessToken;
        }

        public string GetAccessTokenWithServicePrincipal()
        {
            AuthenticationResult authenticationResult = null;
            var m_authorityUrl = Configuration["AzureAd:AuthorityUrl"];
            var tenantId = Configuration["AzureAd:TenantId"];
            var applicationId = Configuration["AzureAd:ClientId"];
            var applicationSecret = Configuration["AzureAd:ClientSecret"];
            var m_scope = Configuration["PowerBi:Scope"].Split(';');
            // For app only authentication, we need the specific tenant id in the authority url
            var tenantSpecificURL = m_authorityUrl.Replace("organizations", tenantId);

            IConfidentialClientApplication clientApp = ConfidentialClientApplicationBuilder
                                                                            .Create(applicationId)
                                                                            .WithClientSecret(applicationSecret)
                                                                            .WithAuthority(tenantSpecificURL)
                                                                            .Build();

            authenticationResult = clientApp.AcquireTokenForClient(m_scope).ExecuteAsync().Result;

            return authenticationResult.AccessToken;
        }

        public PowerBIClient GetPowerBiClient()
        {
            string accesstoken = GetAccessToken();
            var tokenCredentials = new TokenCredentials(accesstoken, "Bearer");
            return new PowerBIClient(new Uri(urlPowerBiServiceApiRoot), tokenCredentials);
        }

        public async Task<EmbeddedReportViewModel> GetReport(Guid WorkspaceId, Guid ReportId)
        {
            using (PowerBIClient pbiClient = GetPowerBiClient())
            {
                // call to Power BI Service API to get embedding data
                var report = await pbiClient.Reports.GetReportInGroupAsync(WorkspaceId, ReportId);

                // generate read-only embed token for the report
                var datasetId = report.DatasetId;
                var tokenRequest = new GenerateTokenRequest(TokenAccessLevel.View, datasetId);
                var embedTokenResponse = await pbiClient.Reports.GenerateTokenAsync(WorkspaceId, ReportId, tokenRequest);
                var embedToken = embedTokenResponse.Token;

                // return report embedding data to caller
                return new EmbeddedReportViewModel
                {
                    ReportId = report.Id.ToString(),
                    EmbedUrl = report.EmbedUrl,
                    Name = report.Name,
                    Token = embedToken
                };
            }
        }

        public Dataset GetDataset(PowerBIClient pbiClient, Guid WorkspaceId, string DatasetName)
        {
            var datasets = pbiClient.Datasets.GetDatasetsInGroup(WorkspaceId).Value;
            foreach (var dataset in datasets)
            {
                if (dataset.Name.Equals(DatasetName))
                {
                    return dataset;
                }
            }
            return null;
        }

        public async Task<IList<Group>> GetTenantWorkspaces(PowerBIClient pbiClient)
        {
            var workspaces = (await pbiClient.Groups.GetGroupsAsync()).Value;
            return workspaces;
        }
       
        public PowerBiWorkspace OnboardNewTenant(PowerBiWorkspace tenant)
        {
            using (PowerBIClient pbiClient = this.GetPowerBiClient())
            {               
                // create new app workspace
                GroupCreationRequest request = new GroupCreationRequest(tenant.Name);
                Group workspace = pbiClient.Groups.CreateGroup(request);
                
                tenant.WorkspaceId = workspace.Id.ToString();
                tenant.WorkspaceUrl = "https://app.powerbi.com/groups/" + workspace.Id.ToString() + "/";

                // add user as new workspace admin
                try
                {
                    string adminUser = Configuration["DemoSettings:AdminUser"];
                    if (!string.IsNullOrEmpty(adminUser))
                    {
                        pbiClient.Groups.AddGroupUser(workspace.Id, new GroupUser
                        {
                            EmailAddress = adminUser,
                            GroupUserAccessRight = "Admin"
                        });
                        pbiClient.Groups.AddGroupUser(workspace.Id, new GroupUser
                        {
                            EmailAddress = "pal@elfo.no",
                            GroupUserAccessRight = "Admin"
                        });
                        pbiClient.Groups.AddGroupUser(workspace.Id, new GroupUser
                        {
                            EmailAddress = "kjetil@elfo.no",
                            GroupUserAccessRight = "Admin"
                        });
                        pbiClient.Groups.AddGroupUser(workspace.Id, new GroupUser
                        {
                            EmailAddress = "hoa@elfo.no",
                            GroupUserAccessRight = "Admin"
                        });
                    }
                }
                catch (Exception ex)
                {
                }               
            }
            return tenant;
        }

        public PowerBiWorkspace ImportReport(PowerBiWorkspace tenant)
        {
            using (PowerBIClient pbiClient = this.GetPowerBiClient())
            {
                string pbixPath = this.Env.WebRootPath + @"/PBIX/SaleStatisticVPTemplate.pbix";
                string importName = "SaleAnalyticsStatistic";
                Guid workspaceId = new Guid(tenant.WorkspaceId);
                bool isExists = isExistsDatasetName(pbiClient, workspaceId, importName);
                if(isExists)
                {
                    importName = importName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                }
                PublishPBIX(pbiClient, workspaceId, pbixPath, importName);


                Dataset dataset = GetDataset(pbiClient, workspaceId, importName);

                UpdateMashupParametersRequest req =
                    new UpdateMashupParametersRequest(new List<UpdateMashupParameterDetails>() {
                      new UpdateMashupParameterDetails { Name = "DatabaseServer", NewValue = "sql01" },
                      new UpdateMashupParameterDetails { Name = "DatabaseName", NewValue = "8446-hyperlink-settlement" }
                  });

                pbiClient.Datasets.UpdateParametersInGroup(workspaceId, dataset.Id, req);

                // pbiClient.Datasets.RefreshDatasetInGroup(workspaceId, dataset.Id);
            }
            return tenant;
        }

        private bool isExistsDatasetName(PowerBIClient pbiClient, Guid workspaceId, string datasetName)
        {
            bool isExists = false;
            try
            {
                var datasets = pbiClient.Datasets.GetDatasetsInGroup(workspaceId);
                if (datasets != null && datasets.Value != null)
                {
                    isExists = datasets.Value.Any(p => p.Name.Equals(datasetName, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch(Exception e) { }
            return isExists;
        }
        
        public Stream ExportReport(PowerBiWorkspace tenant)
        {
            Stream stream;
            using (PowerBIClient pbiClient = this.GetPowerBiClient())
            {
                Guid workspaceId = new Guid(tenant.WorkspaceId);
                var report = pbiClient.Reports.GetReportsInGroup(workspaceId).Value.FirstOrDefault();
                stream = pbiClient.Reports.ExportReportInGroup(workspaceId, report.Id);                
            }
            return stream;
        }
        

        public PowerBiTenantDetails GetTenantDetails(PowerBiWorkspace tenant)
        {
            using (PowerBIClient pbiClient = this.GetPowerBiClient())
            {
                return new PowerBiTenantDetails
                {
                    Name = tenant.Name,
                    WorkspaceId = tenant.WorkspaceId,
                    WorkspaceUrl = tenant.WorkspaceUrl,
                    Members = pbiClient.Groups.GetGroupUsers(new Guid(tenant.WorkspaceId)).Value,
                    Datasets = pbiClient.Datasets.GetDatasetsInGroup(new Guid(tenant.WorkspaceId)).Value,
                    Reports = pbiClient.Reports.GetReportsInGroup(new Guid(tenant.WorkspaceId)).Value
                };
            }
        }

        public PowerBiWorkspace CreateAppWorkspace(PowerBIClient pbiClient, PowerBiWorkspace tenant)
        {
            // create new app workspace
            GroupCreationRequest request = new GroupCreationRequest(tenant.Name);
            Group workspace = pbiClient.Groups.CreateGroup(request);

            // add user as new workspace admin to make demoing easier
            string adminUser = Configuration["DemoSettings:AdminUser"];
            if (!string.IsNullOrEmpty(adminUser))
            {
                pbiClient.Groups.AddGroupUser(workspace.Id, new GroupUser
                {
                    EmailAddress = adminUser,
                    GroupUserAccessRight = "Admin"
                });
            }

            tenant.WorkspaceId = workspace.Id.ToString();

            return tenant;
        }

        public void DeleteWorkspace(PowerBiWorkspace tenant)
        {
            try
            {
                using (PowerBIClient pbiClient = this.GetPowerBiClient())
                {
                    Guid workspaceIdGuid = new Guid(tenant.WorkspaceId);
                    pbiClient.Groups.DeleteGroup(workspaceIdGuid);
                }
            }
            catch(Exception ex) { }
        }

        public void DeleteWorkspaceByName(string name)
        {
            try
            {
                using (PowerBIClient pbiClient = this.GetPowerBiClient())
                {
                    var groups = pbiClient.Groups.GetGroups();
                    var group = groups.Value.Where(p => p.Name == name).FirstOrDefault();
                    pbiClient.Groups.DeleteGroup(group.Id);
                }
            }
            catch (Exception ex) { }
        }

        public void PublishPBIX(PowerBIClient pbiClient, Guid WorkspaceId, string PbixFilePath, string ImportName)
        {
            FileStream stream = new FileStream(PbixFilePath, FileMode.Open, FileAccess.Read);
            var import = pbiClient.Imports.PostImportWithFileInGroup(WorkspaceId, stream, ImportName);
            while (import.ImportState != "Succeeded")
            {
                import = pbiClient.Imports.GetImportInGroup(WorkspaceId, import.Id);
            }
        }

        public void PatchSqlDatasourceCredentials(PowerBIClient pbiClient, Guid WorkspaceId, string DatasetId, string SqlUserName, string SqlUserPassword)
        {

            var datasources = (pbiClient.Datasets.GetDatasourcesInGroup(WorkspaceId, DatasetId)).Value;

            // find the target SQL datasource
            foreach (var datasource in datasources)
            {
                if (datasource.DatasourceType.ToLower() == "sql")
                {
                    // get the datasourceId and the gatewayId
                    var datasourceId = datasource.DatasourceId;
                    var gatewayId = datasource.GatewayId;
                    // Create UpdateDatasourceRequest to update Azure SQL datasource credentials
                    UpdateDatasourceRequest req = new UpdateDatasourceRequest
                    {
                        CredentialDetails = new CredentialDetails(
                        new BasicCredentials(SqlUserName, SqlUserPassword),
                        PrivacyLevel.None,
                        EncryptedConnection.NotEncrypted)
                    };
                    // Execute Patch command to update Azure SQL datasource credentials
                    pbiClient.Gateways.UpdateDatasource((Guid)gatewayId, (Guid)datasourceId, req);
                }
            };

        }

        public async Task<EmbeddedReportViewModel> GetReportEmbeddingData(string AppIdentity, string Tenant)
        {
            using (PowerBIClient pbiClient = GetPowerBiClient())
            {
                var tenant = this.AppOwnsDataDBService.GetWorkspace(Tenant);
                Guid workspaceId = new Guid(tenant.WorkspaceId);
                var reports = (await pbiClient.Reports.GetReportsInGroupAsync(workspaceId)).Value;

                var report = reports.First();  //reports.Where(report => report.Name.Equals("Sales")).First();

                GenerateTokenRequest generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "View");

                // call to Power BI Service API and pass GenerateTokenRequest object to generate embed token
                string embedToken = pbiClient.Reports.GenerateTokenInGroup(workspaceId, report.Id,
                                                                           generateTokenRequestParameters).Token;

                return new EmbeddedReportViewModel
                {
                    ReportId = report.Id.ToString(),
                    Name = report.Name,
                    EmbedUrl = report.EmbedUrl,
                    Token = embedToken,
                    TenantName = Tenant
                };
            }
        }

        public void CreateData<T>(string workspaceId, string datasetName, string tableName, IList<object> objects)
        {
            using (PowerBIClient pbiClient = this.GetPowerBiClient())
            {
                Guid workspaceGuid = new Guid(workspaceId);
                Dataset ds = GetDataset(pbiClient, workspaceGuid, datasetName);                
                pbiClient.Datasets.PostRowsInGroup(workspaceGuid, ds.Id, tableName, new PostRowsRequest()
                {
                    Rows = objects
                });
            }
        }

        public Table CreateTable<T>(string tableName)
        {
            Type elementType = typeof(T);
            Table table = new Table()
            {
                Name = tableName
            };

            table.Columns = new List<Column>();
            foreach (var propInfo in elementType.GetProperties())
            {
                Type ColType = Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;
                var colType = ColType.ToString().Replace("System.", "").ToLower();
                var colTypeBI = colType;
                switch (colType)
                {
                    case "datetime":
                        colTypeBI = "Datetime";
                        break;
                    case "int16":
                    case "int32":
                        colTypeBI = "Int64";
                        break;
                    case "decimal":
                        colTypeBI = "Double";
                        break;
                    case "boolean":
                        colTypeBI = "Boolean";
                        break;
                }

                table.Columns.Add(new Column()
                {
                    Name = propInfo.Name,
                    DataType = colTypeBI
                });
            }
            return table;
        }

        public CreateDatasetRequest CreateDataSet(string datasetName)
        {
            CreateDatasetRequest ds = new CreateDatasetRequest();
            ds.Name = datasetName;
            ds.DefaultMode = "Push";
            ds.Tables = new List<Table>();
            return ds;
        }

    }

}