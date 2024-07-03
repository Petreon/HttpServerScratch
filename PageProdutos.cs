using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServerScratch
{
    class PageProdutos : DynamicPage
    {
        public override byte[] Get(SortedList<string, string> parameterList)
        {

            string codigo = parameterList.ContainsKey("id") ? parameterList["id"] : "";
            int number = 0;
            bool codeIsNumber = int.TryParse(codigo,out number);
            StringBuilder htmlGenerated = new StringBuilder();
            foreach (Produto Product in Produto.ProdutoList)
            {
                htmlGenerated.Append("<tr>");

                if (codeIsNumber && Product.Code == number)
                {
                    //appends to negrito
                    htmlGenerated.Append($"<td><b>{Product.Code:D4}</b></td>");//D4 represents 4 integer digits
                    htmlGenerated.Append($"<td><b>{Product.Name}</b></td>");
                }
                else
                {
                    htmlGenerated.Append($"<td>{Product.Code:D4}</td>");//D4 represents 4 integer digits
                    htmlGenerated.Append($"<td>{Product.Name}</td>");
                }

                
                htmlGenerated.Append("</tr>");
            }

            string textHtmlGenerated = this.HtmlTemplate.Replace(
                "{{HtmlGenerated}}", htmlGenerated.ToString());

            return Encoding.UTF8.GetBytes( textHtmlGenerated );

        }
    }
}
