using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PowerBIAdmin.Models;
using PowerBIAdmin.Repository.Models;
using PowerBIAdmin.Service;
using PowerBIAdmin.Service.DataTable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PowerBIAdmin.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private AppOwnsDataDBService _appOwnsDataDBService;
        private PowerBiServiceApi _powerBiServiceApi;
        private PointApiService _pointApiService;

        public HomeController(ILogger<HomeController> logger, 
            AppOwnsDataDBService appOwnsDataDBService, 
            PowerBiServiceApi powerBiServiceApi,
            PointApiService pointApiService)
        {
            _logger = logger;
            _appOwnsDataDBService = appOwnsDataDBService;
            _powerBiServiceApi = powerBiServiceApi;
            _pointApiService = pointApiService;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        #region Workspace
        public IActionResult Workspaces()
        {
            var model = _appOwnsDataDBService.GetWorkspaces();
            return View(model);
        }

        public IActionResult Workspace(string name)
        {
            var model = _appOwnsDataDBService.GetWorkspace(name);
            var modelWithDetails = _powerBiServiceApi.GetTenantDetails(model);
            return View(modelWithDetails);
        }

        [HttpPost]
        public IActionResult Workspace(PowerBiTenantDetails powerBiTenantDetail)
        {           
            _powerBiServiceApi.ImportReport(powerBiTenantDetail);
            return RedirectToAction("Workspace", new { name = powerBiTenantDetail.Name } );
        }

        public IActionResult OnboardWorkspace()
        {
            var model = new OnboardWorkspaceModel
            {
                WorkspaceName = this._appOwnsDataDBService.GetNextWorkspaceName()
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult OnboardWorkspace(string workspaceName)
        {
            var tenant = new PowerBiWorkspace
            {
                Name = workspaceName
            };
            tenant = _powerBiServiceApi.OnboardNewTenant(tenant);
            _appOwnsDataDBService.OnboardNewWorkspace(tenant);
            // Import Default Template
            try
            {
                _powerBiServiceApi.ImportReport(tenant);
            }
            catch (Exception) { }
            return RedirectToAction("Workspaces");
        }

        public async Task<IActionResult> DeleteWorkspace(string name)
        {
            var tenant = _appOwnsDataDBService.GetWorkspace(name);
            // Delete from Power BI
            _powerBiServiceApi.DeleteWorkspace(tenant);

            // Delete from Point
            var users = _appOwnsDataDBService.GetUsersByWorkspaceName(name);
            foreach (var user in users)
            {
                await _pointApiService.UpdatePowerBISetting(user.PointApiUrl, null);
            }
            // Delet from DB
            _appOwnsDataDBService.DeleteWorkspace(tenant);
            return RedirectToAction("Workspaces");
        }

        public IActionResult Embed(string AppIdentity, string Tenant)
        {
            var viewModel = _powerBiServiceApi.GetReportEmbeddingData(AppIdentity, Tenant).Result;
            return View(viewModel);
        }

        #endregion

        #region Users
        public IActionResult Users()
        {
            var model = _appOwnsDataDBService.GetUsers();
            return View(model);
        }

        public IActionResult CreateUser()
        {
            var model = new CreateUserModel
            {
                WorkspaceOptions = _appOwnsDataDBService.GetWorkspaces().Select(tenant => new SelectListItem
                {
                    Text = tenant.Name,
                    Value = tenant.Name
                }).ToList(),
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(User user)
        {
            var model = _appOwnsDataDBService.CreateUser(user);
            // Update to Point
            if (!string.IsNullOrWhiteSpace(model.PointApiUrl))
            {
                if (!string.IsNullOrWhiteSpace(model.WorkspaceName))
                {
                    var workspace = _appOwnsDataDBService.GetWorkspace(model.WorkspaceName);
                    if (workspace != null)
                    {
                        var modelWithDetail = _powerBiServiceApi.GetTenantDetails(workspace);
                        var dataset = modelWithDetail.Datasets.FirstOrDefault();
                        WorkspaceDetail workspaceDetail = new WorkspaceDetail()
                        {
                            WorkspaceId = modelWithDetail.WorkspaceId,
                            Name = modelWithDetail.Name,
                            Datasets = dataset != null ? new[] { dataset.Id } : null,
                            CanCreate = user.CanCreate,
                            CanEdit = user.CanEdit
                        };
                        // Update to point
                        await _pointApiService.UpdatePowerBISetting(model.PointApiUrl, workspaceDetail);
                    }
                }
            }
            return RedirectToAction("Users");
        }

        public IActionResult GetUser(string loginId)
        {
            var model = _appOwnsDataDBService.GetUser(loginId);
            return View(model);
        }

        public IActionResult EditUser(string loginId)
        {
            var model = new EditUserModel
            {
                User = _appOwnsDataDBService.GetUser(loginId),
                WorkspaceOptions = _appOwnsDataDBService.GetWorkspaces().Select(tenant => new SelectListItem
                {
                    Text = tenant.Name,
                    Value = tenant.Name
                }).ToList(),
            };
            var item = model.WorkspaceOptions.Where(p => p.Text == model.User.WorkspaceName).FirstOrDefault();
            if (item != null)
            {
                item.Selected = true;
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(User user)
        {
            // Update to DB
            var model = _appOwnsDataDBService.UpdateUser(user);

            // Update to Point
            if (!string.IsNullOrWhiteSpace(model.PointApiUrl))
            {
                if (!string.IsNullOrWhiteSpace(model.WorkspaceName))
                {
                    var workspace = _appOwnsDataDBService.GetWorkspace(model.WorkspaceName);
                    if (workspace != null)
                    {
                        var modelWithDetail = _powerBiServiceApi.GetTenantDetails(workspace);
                        var dataset = modelWithDetail.Datasets.FirstOrDefault();
                        WorkspaceDetail workspaceDetail = new WorkspaceDetail()
                        {
                            WorkspaceId = modelWithDetail.WorkspaceId,
                            Name = modelWithDetail.Name,
                            Datasets = dataset != null ? new[] { dataset.Id } : null,
                            CanCreate = user.CanCreate,
                            CanEdit = user.CanEdit
                        };
                        var json = JsonConvert.SerializeObject(workspaceDetail);
                        // Update to point
                        await _pointApiService.UpdatePowerBISetting(model.PointApiUrl, workspaceDetail);
                    }
                }
                else
                {
                    await _pointApiService.UpdatePowerBISetting(model.PointApiUrl, null);
                }
            }
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> DeleteUser(string loginId)
        {
            var user = _appOwnsDataDBService.GetUser(loginId);
            _appOwnsDataDBService.DeleteUser(user);
            await _pointApiService.UpdatePowerBISetting(user.PointApiUrl, null);
            return RedirectToAction("Users");
        }

        #endregion

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult CreateData()
        {
            return RedirectToAction("Workspaces");
        }

        
    }
}
