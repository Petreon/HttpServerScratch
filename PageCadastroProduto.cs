using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServerScratch
{
    internal class PageCadastroProduto : DynamicPage
    {

        public override byte[] Post(SortedList<string,string> parameters)
        {
            Produto produto = new Produto();
            
            produto.Code = parameters.ContainsKey("code") ? Convert.ToInt32(parameters["code"]) : 0;
            produto.Name = parameters.ContainsKey("name") ? parameters["name"] : "";

            if(produto.Code > 0)
            {
                Produto.ProdutoList.Add(produto);
            }
            //redirecting the user to the list page
            string html = "<script>window.location.replace(\"produtos.dhtml\") </script>";
            return Encoding.UTF8.GetBytes(html);
        }

    }
}
