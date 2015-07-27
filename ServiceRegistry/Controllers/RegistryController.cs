using Common.Registry;
using ServiceRegistry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace ServiceRegistry.Controllers
{
    [RoutePrefix("registry")]
    public class RegistryController : ApiController
    {
        [Route("ping")]
        [HttpGet]
        public HttpResponseMessage Ping()
        {
            var stringResponse = DateTime.UtcNow.ToString();

            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            resp.Content = new StringContent(stringResponse, Encoding.UTF8, "text/plain");
            return resp;
        }

        [Route("status")]
        [HttpGet]
        public ServiceInfo Status()
        {
            return ServiceInfoFactory.CreateServiceDefinition(new ServiceInfo { Port = Convert.ToInt32(ServiceConfig.Reader.Port) });
        }
    }
}
