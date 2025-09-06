namespace SARVA.Models
{
    public class PedidoViewModel
    {
        public Pedido? Pedido { get; set; }
        public List<ItemPedidoDetalhe> ItensDetalhados { get; set; } = new List<ItemPedidoDetalhe>();
        public List<ProdutoAgrupado> ProdutosAgrupados { get; set; } = new List<ProdutoAgrupado>();
        public decimal ValorTotal { get; set; }
        public decimal ValorAPagar { get; set; }
    }
}