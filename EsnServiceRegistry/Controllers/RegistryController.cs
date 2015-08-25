using EsnCore.Registry;
using EsnServiceRegistry.Attributes;
using EsnServiceRegistry.Models;
using EsnServiceRegistry.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace EsnServiceRegistry.Controllers
{
    [NoCache]
    [RoutePrefix("registry")]
    public class RegistryController : ApiController
    {
        [Route("dashboard")]
        [HttpGet]
        public DashboardModel GetDashboard()
        {
            using (var registryRepo = new RegistryRepository(new RegistryDatabaseFactory()))
            {
                var dash = new DashboardModel
                {
                    Registry = ServiceInfoFactory.CreateServiceDefinition(new ServiceInfo
                    {
                        Port = Convert.ToInt32(ServiceConfig.Reader.Port),
                        Name = ServiceConfig.Reader.ServiceName,
                    })
                };
                dash.AmqpAdmin = ServiceConfig.Reader.AmqpAdmin;
                dash.RethinkAdmin = ServiceConfig.Reader.RethinkAdmin;
                int services, hosts, issues;
                registryRepo.GetTotals(out services, out hosts, out issues);
                dash.ServicesCount = services;
                dash.HostsCount = hosts;
                dash.IssuesCount = issues;
                return dash;
            }
        }

        [Route("dashboard/clusters")]
        [HttpGet]
        public List<ServiceCluster> GetServiceClusters()
        {
            using (var registryRepo = new RegistryRepository(new RegistryDatabaseFactory()))
            {
                return registryRepo.GetServiceClusters();
            }
        }

        [Route("ping")]
        [HttpGet]
        public HttpResponseMessage Ping()
        {
            var stringResponse = DateTime.UtcNow.ToString();

            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            resp.Content = new StringContent(stringResponse, Encoding.UTF8, "text/plain");
            return resp;
        }
    }
}
