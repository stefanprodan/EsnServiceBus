﻿using EsnCore.Registry;
using EsnCore.ServiceBus;
using EsnServiceRegistry.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace EsnServiceRegistry.Controllers
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

            var topics = new TopicFactory(ConnectionConfig.GetFactoryDefault(), new JsonMessageSerializer(), new ConsoleLog(), status.Version);
            topics.PublishMessage(status, new string[] { Topics.Images, Topics.Text, Topics.Url, Topics.Video });
            return status;
        }


        [Route("pubsub")]
        [HttpGet]
        public ServiceInfo EmitPubSubMessage()
        {
            var status = ServiceInfoFactory.CreateServiceDefinition(new ServiceInfo { Port = Convert.ToInt32(ServiceConfig.Reader.Port) });

            var pubsub = new FanoutFactory(ConnectionConfig.GetFactoryDefault(), new JsonMessageSerializer(), new ConsoleLog(), status.Version);
            pubsub.PublishMessage(status);

            return status;
        }
    }
}
