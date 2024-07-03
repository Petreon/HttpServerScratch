using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServerScratch
{
    class Produto
    {
        public static List<Produto> ProdutoList {  get; set; } 
        public int Code { get; set; }
        public string Name { get; set; }

        static Produto()
        {
            ProdutoList = new List<Produto>();
            //new way to do this, i would use an looping
            Produto.ProdutoList.AddRange(new List<Produto>
            {
                new Produto{Code=1, Name="Banana"},
                new Produto{Code=2, Name="Apple"},
                new Produto{Code=3, Name="Grapes"},
                new Produto{Code=4, Name="Orange"},
                new Produto{Code=5, Name="Lemon"},
            });
        }

    }
}
