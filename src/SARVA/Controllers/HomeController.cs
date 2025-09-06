using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SARVA.Data;
using SARVA.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace SARVA.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SARVADbContext _db;

        public HomeController(ILogger<HomeController> logger, SARVADbContext db, UserManager<IdentityUser> userManager) : base(userManager)
        {
            _logger = logger;
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult Relatorio()
        {
            var viewModel = new RelatorioViewModel
            {
                GerouRelatorio = false
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Relatorio(RelatorioViewModel model)
        {
            var empresa = await _db.Empresas
                .FirstOrDefaultAsync(x => x.razao_social == model.RazaoSocialInput);

            if (empresa == null)
            {
                ModelState.AddModelError("RazaoSocialInput", "Empresa não encontrada.");
                model.GerouRelatorio = false;
                return View(model);
            }

            // -> Obtém o ID do usuário diretamente da propriedade da BaseController
            var idUsuarioStr = Convert.ToString(UsuarioIdLogado);

            if (string.IsNullOrEmpty(idUsuarioStr))
            {
                return Unauthorized();
            }

            var totalVendas = await _db.Venda
                .Where(x => x.id_usuario == idUsuarioStr && x.id_empresa == empresa.id)
                .ToListAsync();

            var totalPedidos = await _db.Pedidos
                .Where(x => x.id_usuario == idUsuarioStr && x.id_empresa == empresa.id)
                .ToListAsync();

            var vendasPagas = totalVendas.Where(x => x.data_pagamento != null).ToList();

            decimal valorObtido = vendasPagas.Sum(v => v.valorFinal ?? 0);
            decimal valorASerPago = totalPedidos.Sum(p => p.valor ?? 0);
            decimal lucro = valorObtido - valorASerPago;

            var resultViewModel = new RelatorioViewModel
            {
                GerouRelatorio = true,
                EmpresaNome = empresa.razao_social,
                TotalVendas = totalVendas.Count,
                VendasPagas = vendasPagas.Count,
                ValorObtido = valorObtido,
                ValorASerPago = valorASerPago,
                Lucro = lucro
            };

            return View(resultViewModel);
        }
    }
}
