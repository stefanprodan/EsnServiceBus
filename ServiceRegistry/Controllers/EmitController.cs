using Common.Registry;
using Common.ServiceBus;
using Newtonsoft.Json;
using RabbitMQ.Client;
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
    [RoutePrefix("emit")]
    public class EmitController : ApiController
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

        [Route("topic")]
        [HttpGet]
        public ServiceInfo EmitTopicMessage()
        {
            var status = ServiceInfoFactory.CreateServiceDefinition(new ServiceInfo { Port = Convert.ToInt32(ServiceConfig.Reader.Port) });

            var topics = new TopicFactory(ConnectionConfig.GetFactoryDefault());
            topics.PublishMessage(status, new string[] { Topics.Images, Topics.Text, Topics.Url, Topics.Video });
            return status;
        }


        [Route("pubsub")]
        [HttpGet]
        public ServiceInfo EmitPubSubMessage()
        {
            var status = ServiceInfoFactory.CreateServiceDefinition(new ServiceInfo { Port = Convert.ToInt32(ServiceConfig.Reader.Port) });

            var pubsub = new FanoutFactory(ConnectionConfig.GetFactoryDefault());
            pubsub.PublishMessage(status);

            return status;
        }
    }
}
