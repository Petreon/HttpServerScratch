using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServerScratch
{
    abstract class DynamicPage
    {

        public string HtmlTemplate { get; set; }

        public virtual byte[] Get(SortedList<string,string> parameters)
        {
            return Encoding.UTF8.GetBytes(HtmlTemplate);
        }

        public virtual byte[] Post(SortedList<string, string> parameters)
        {
            return Encoding.UTF8.GetBytes(HtmlTemplate);
        }

    }
}
