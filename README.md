# EsnServiceBus

Service Bus and Service Registy implementation based on RabbitMQ

### Prerequisites

1. Install [Erlang/OTP 18.0](http://www.erlang.org/download.html)
2. Install [RabbitMQ Server](https://www.rabbitmq.com/install-windows.html)
3. Enable Management Plugin `./rabbitmq-plugins.bat enable rabbitmq_management`
4. Restart service
5. Open web manager `http://localhost:15672` using user/pass `guest`
6. Connect from code URI `amqp://guest:guest@localhost:5672/%2f?heartbeat=60`
