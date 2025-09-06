namespace SARVA.Models
{
    public class RelatorioViewModel
    {
        // Propriedades para o formulário de entrada
        public string? RazaoSocialInput { get; set; }

        // Propriedades para exibir os resultados
        public bool GerouRelatorio { get; set; } = false;
        public string? EmpresaNome { get; set; }
        public int TotalVendas { get; set; }
        public int VendasPagas { get; set; }
        public decimal ValorObtido { get; set; }
        public decimal ValorASerPago { get; set; }
        public decimal Lucro { get; set; }
    }
}