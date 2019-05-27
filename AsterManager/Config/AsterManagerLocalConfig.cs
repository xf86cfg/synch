using System;
using System.Collections.Generic;
using System.Text;

namespace synch
{
    public class AsterManagerLocalConfig : AsterManagerConfig
    {
        public override string Hostname { get { return "127.0.0.1"; } }
    }
}
