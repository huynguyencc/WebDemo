using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PowerBIAdmin.Repository.Models;

namespace PowerBIAdmin.Service
{
    /// <summary>
    /// AppOwnsDataDBService
    /// </summary>
    public class AppOwnsDataDBService
    {
        private readonly AppOwnsDataDB dbContext;

        public AppOwnsDataDBService(AppOwnsDataDB context)
        {
            dbContext = context;
        }

        #region Workspace
        public string GetNextWorkspaceName()
        {
            var appNames = dbContext.Workspaces.Select(tenant => tenant.Name).ToList();
            string baseName = "Workspace";
            string nextName;
            int counter = 0;
            do
            {
                counter += 1;
                nextName = baseName + "_" + counter.ToString("00");
            }
            while (appNames.Contains(nextName));
            return nextName;
        }

        public IList<PowerBiWorkspace> GetWorkspaces()
        {
            return dbContext.Workspaces
                   .Select(tenant => tenant)
                   .OrderBy(tenant => tenant.Name)
                   .ToList();
        }

        public void OnboardNewWorkspace(PowerBiWorkspace workspace)
        {
            dbContext.Workspaces.Add(workspace);
            dbContext.SaveChanges();
        }

        public PowerBiWorkspace GetWorkspace(string name)
        {
            var tenant = dbContext.Workspaces.Where(tenant => tenant.Name == name).FirstOrDefault();
            return tenant;
        }

        public void DeleteWorkspace(PowerBiWorkspace tenant)
        {
            // unassign any users in the tenant
            var tenantUsers = dbContext.Users.Where(user => user.WorkspaceName == tenant.Name);
            foreach (var user in tenantUsers)
            {
                user.WorkspaceName = "";
                dbContext.Users.Update(user);                
            }
            dbContext.SaveChanges();

            // delete the tenant
            dbContext.Workspaces.Remove(tenant);
            dbContext.SaveChanges();
            return;
        }

        #endregion

        #region User
        public IList<User> GetUsers()
        {
            return dbContext.Users
                   .Select(user => user)
                   .OrderByDescending(user => user.Created)
                   .ToList();
        }

        public User GetUser(string loginId)
        {
            var user = dbContext.Users.Where(user => user.LoginId == loginId).First();
            return user;
        }

        public IList<User> GetUsersByWorkspaceName(string workspaceName)
        {
            var users = dbContext.Users.Where(user => user.WorkspaceName == workspaceName).ToList();
            return users;
        }

        public User UpdateUser(User currentUser)
        {
            var users = dbContext.Users.Where(user => user.LoginId == currentUser.LoginId);
            User user;
            if (users.Count() > 0)
            {
                user = users.First();
            }
            else
            {
                user = new User();
            }
            user.CanEdit = currentUser.CanEdit;
            user.CanCreate = currentUser.CanCreate;
            user.WorkspaceName = currentUser.WorkspaceName;
            user.PointApiUrl = currentUser.PointApiUrl;
            dbContext.SaveChanges();
            return user;
        }

        public User CreateUser(User newUser)
        {
            var users = dbContext.Users.Where(user => user.LoginId == newUser.LoginId);
            User user;
            if (users.Count() > 0)
            {
                user = users.First();
            }
            else
            {
                user = new User();
                user.Created = DateTime.Now;
            }
            user.LoginId = newUser.LoginId;
            user.WorkspaceName = !string.IsNullOrEmpty(newUser.WorkspaceName) ? newUser.WorkspaceName : user.WorkspaceName;
            user.CanEdit = newUser.CanEdit;
            user.CanCreate = newUser.CanCreate;
            user.PointApiUrl = newUser.PointApiUrl;
            dbContext.Users.Add(user);
            dbContext.SaveChanges();
            return user;
        }

        public void DeleteUser(User user)
        {
            dbContext.Users.Remove(user);
            dbContext.SaveChanges();
            return;
        }

        #endregion

    }

}
