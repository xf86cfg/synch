using System;
using System.Collections.Generic;
using System.Text;

namespace synch
{
    public interface IServiceControl
    {
        event EventHandler<ServiceControlRequestEventArgs> ServiceControlRequest;
        void Start();
        void Stop();
    }
}
