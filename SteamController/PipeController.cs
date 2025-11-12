using CommonHelpers;

using System.IO.Pipes;

namespace SteamController
{
    public enum Command
    {
        None = 0,
        ControllerEnable,
        ControllerDisable,
        X360,
        DS4,
        Desktop,
        CloseConnection,
        GetStatus,
    }

    internal class ControllerStatus
    {
        public string Controller { get; set; }
        public bool Selected { get; set; }
    }

    internal class PipeController : IDisposable
    {
        private readonly NamedPipeServerStream _pipeStream;

        public event EventHandler Disable;

        public event EventHandler Enable;

        public event EventHandler X360;

        public event EventHandler DS4;

        public event EventHandler Desktop;

        private Func<List<ControllerStatus>> _getControllerStatus;

        public void SetControllerStatusFunc(Func<List<ControllerStatus>> func)
        {
            _getControllerStatus = func;
        }

        public PipeController()
        {
            var numThreads = 1;
            _pipeStream = new NamedPipeServerStream("steamDeckTools",
                        PipeDirection.InOut, numThreads);
        }

        public void Listen()
        {
            _pipeStream.WaitForConnection();
            Command command = Command.None;
            Console.WriteLine($"Client connected.");
            try
            {
                do
                {
                    // Read the request from the client. Once the client has
                    // written to the pipe its security token will be available.
                    StreamString ss = new StreamString(_pipeStream);
                    string commandString = ss.ReadString();

                    if (!Enum.TryParse(commandString, out command))
                    {
                        // Log command sent by client didn't match known commands
                        command = Command.None;
                        continue;
                    }

                    switch (command)
                    {
                        case Command.ControllerEnable:
                            Enable.Invoke(this, new EventArgs());
                            break;

                        case Command.ControllerDisable:
                            Disable.Invoke(this, new EventArgs());
                            break;

                        case Command.X360:
                            X360.Invoke(this, new EventArgs());
                            break;

                        case Command.DS4:
                            DS4.Invoke(this, new EventArgs());
                            break;

                        case Command.Desktop:
                            Desktop.Invoke(this, new EventArgs());
                            break;

                        case Command.GetStatus:
                            if (_getControllerStatus is not null)
                            {
                                var status = _getControllerStatus();
                            }

                            break;

                        default:
                            break;
                    }
                }
                while (command != Command.CloseConnection);
                _pipeStream.Close();
            }
            // Catch the IOException that is raised if the pipe is broken
            // or disconnected.
            catch (IOException e)
            {
                Console.WriteLine($"ERROR: {e.Message}");
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}