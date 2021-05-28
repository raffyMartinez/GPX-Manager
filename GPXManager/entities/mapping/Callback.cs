using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapWinGIS;

namespace GPXManager.entities.mapping
{
    public class Callback:ICallback
    {
        public string ErrorMessage { get; private set; }
        public void Error(string KeyOfSender, string ErrorMsg)
        {
            ErrorMessage = $"{KeyOfSender}: {ErrorMsg}";
        }
        public void Progress(string KeyOfSender, int Percent, string Message)
        {
            //Debug.Print(Message + ": " + Percent + "%");
        }
    }
}
