using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Options;

namespace synch
{
    public class AsterManagerLocal : AsterManager
    {
        public AsterManagerLocal(IOptions<AsterManagerLocalConfig> options) : base(options)
        {
        }
    }
}
