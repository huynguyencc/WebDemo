using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PowerBIAdmin.Service
{
    public class WorkspaceDetail
    {
        public string Name { get; set; }
        public string WorkspaceId { get; set; }
        public IList<string> Datasets { get; set; }
        public bool CanEdit { get; set; }
        public bool CanCreate { get; set; }
    }

    public class PointApiService : ApiService
    {
        public async Task<bool> UpdatePowerBISetting(string apiUrl, WorkspaceDetail workspaceDetail)
        {
            Initialize(apiUrl);
            return await PostAsync<bool>($"powerpisettings", workspaceDetail);
        }
    }
}
