namespace RabbitMQ_Chat
{
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using System.Text;

    internal class Program
    {
        static List<string> messages = new();
        static Mutex display = new();

        static void Main(string[] args)
        {
            //initialize variables
            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            string room_code = "";
            string username = "Default";
            bool FINISHED = false;
            string input = "";

            //console shiz
            Console.WriteLine("What is your username?");
            username = Console.ReadLine();
            Console.Clear();
            Console.WriteLine("Enter Room id:");
            room_code = Console.ReadLine();
            Console.Clear();


            //connect and join
            ConnectionSetup(room_code, channel);
            Console.Clear(); //need to clear before we join
            JoinRoom(username, channel, room_code);


            //let user know how to leave
            Console.WriteLine("TYPE 'EXIT' to leave");

            //loop for sending and receiving messages

            while (!FINISHED)
            {
                //position for typing
                Console.SetCursorPosition(0, 13);
                input = Console.ReadLine();
                Console.Clear();
                if (input != "EXIT")
                {
                    SendMessage(input, username, channel, room_code);
                }
                else
                {
                    LeaveRoom(username, channel, room_code);
                    Console.Clear();
                    FINISHED = true;
                }
            }

        }

        static void ConnectionSetup(string exchangeCode, IModel channel)
        {

            channel.ExchangeDeclare(exchange: exchangeCode, type: ExchangeType.Fanout);

            // declare a server-named queue
            var queueName = channel.QueueDeclare().QueueName;

            channel.QueueBind(queue: queueName,
                exchange: exchangeCode,
                routingKey: string.Empty);

            //consuumer listener
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                messages.Add(message);
                DisplayMessages();

            };

            channel.BasicConsume(queue: queueName,
                                 autoAck: true,
                                 consumer: consumer);


        }

       
        static void SendMessage(string message, string username, IModel channel, string exchangeCode)
        {
            //Grabs username and message, packages together and sends
            var adjusted_message = $"{username}: {message}";

            var encoded_message = Encoding.UTF8.GetBytes(adjusted_message);

            channel.BasicPublish(exchange: exchangeCode,
                routingKey: string.Empty,
                basicProperties: null,
                body: encoded_message);
        }

        static void LeaveRoom(string username, IModel channel, string exchangeCode)
        {
            //same as send message, but lets people know they left
            var adjusted_message = $"{username} has left";

            var encoded_message = Encoding.UTF8.GetBytes(adjusted_message);

            channel.BasicPublish(exchange: exchangeCode,
                routingKey: string.Empty,
                basicProperties: null,
                body: encoded_message);
        }

        static void JoinRoom(string username, IModel channel, string exchangeCode)
        {
            //same as send message, but lets people know they joined
            var adjusted_message = $"{username} joined";

            var encoded_message = Encoding.UTF8.GetBytes(adjusted_message);

            channel.BasicPublish(exchange: exchangeCode,
                routingKey: string.Empty,
                basicProperties: null,
                body: encoded_message);
        }

        static void DisplayMessages()
        {
            //this creates the chat interface
            //due to the way multiple messages can be received at a time, we need to make it thread safe with a mutex
            display.WaitOne();
            //grab current position so when person is typing, they shouldnt loose track of where about the cursor is
            var currentPosition = Console.GetCursorPosition();
            Console.SetCursorPosition(0, 0);
            //clearing the screen
            for (int i = 0; i < 13; i++)
            {
                Console.WriteLine("\t\t\t\t\t\t\t\t\t");
            }
            //build top
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("TYPE 'EXIT' to leave");
            Console.WriteLine("------------------------");
            //trim messages tto 10
            if (messages.Count > 10)
            {
                do
                {
                    messages.RemoveAt(0);
                } while (messages.Count > 10);
            }
            //write out each message
            foreach (string message in messages)
            {
                Console.WriteLine(message);
            }
            //we might not always have 10 messages, so need to set cursor
            Console.SetCursorPosition(0, 12);
            Console.WriteLine("------------------------");
            //set cursor back to place so person can continue typing
            Console.SetCursorPosition(currentPosition.Left, currentPosition.Top);
            //release mutex for next time to be processed
            display.ReleaseMutex();
        }



    }
}