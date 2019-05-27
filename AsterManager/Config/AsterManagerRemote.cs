using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Options;

namespace synch
{
    public class AsterManagerRemote : AsterManager
    {
        public AsterManagerRemote(IOptions<AsterManagerRemoteConfig> options) : base(options)
        {
        }
    }
}
