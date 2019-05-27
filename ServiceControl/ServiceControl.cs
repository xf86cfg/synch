using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;

namespace synch
{
    public class ServiceControl : IServiceControl
    {
        private TcpListener _server = null;
        public event EventHandler<ServiceControlRequestEventArgs> ServiceControlRequest;
        private IOptions<ServiceControlConf> _options;

        public ServiceControl(IOptions<ServiceControlConf> options)
        {
            ServiceControlConf.Validate(options.Value);
            _options = options;
        }

        public async void Start()
        {
            var task = Task.Run(() => StartServer(_options.Value.Token));
            try
            {
                await task;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private void StartServer(string token)
        {
            _server = new TcpListener(IPAddress.Parse(_options.Value.ListenAddress), _options.Value.Port);
            _server.Start();
            while (true)
            {
                TcpClient client = _server.AcceptTcpClient();
                Task.Run(() => HandleClient(client, token));
            }
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        private string GetTokenFromInput(string input)
        {
            string authPattern = @"\([a-zA-Z0-9\-\.\:\\\/]*\)";
            return Regex.Match(input, authPattern).Value.TrimStart('(').TrimEnd(')');
        }

        private string GetActionFromInput(string input)
        {
            string commandPattern = @"^[a-zA-Z]*";
            return Regex.Match(input, commandPattern).Value;
        }

        private void AnswerClient(NetworkStream stream, string response)
        {
            byte[] msg = Encoding.ASCII.GetBytes(response + "\n");
            stream.Write(msg, 0, msg.Length);
        }

        private void HandleClient(TcpClient client, string token)
        {
            byte[] bytes = new byte[256];
            string data = null;
            NetworkStream stream = client.GetStream();
            int i;
            while (client.Connected && (i = stream.Read(bytes, 0, bytes.Length)) > 0)
            {
                data = Encoding.ASCII.GetString(bytes, 0, i);
                try
                {
                    var action = GetActionFromInput(data);
                    if (GetTokenFromInput(data) != token)
                    {
                        AnswerClient(stream, "Token error. Bye bye!");
                    }
                    else
                    {
                        var args = new ServiceControlRequestEventArgs(action);
                        ServiceControlRequest(this, args);
                        var response = args.IsSuccess ?
                            JsonConvert.SerializeObject(args.Responses) : "Action error. Bye bye!";
                        AnswerClient(stream, response);
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
                finally
                {
                    client.Close();
                }
            }
        }
    }

    public class ServiceControlRequestEventArgs : EventArgs
    {
        public string Action { get; set; }
        public bool IsSuccess { get; set; }
        public IList<Dictionary<string, string>> Responses { get; set; }

        public ServiceControlRequestEventArgs(string action)
        {
            Action = action;
            Responses = new List<Dictionary<string, string>>();
        }
    }
}
