using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SARVA.Data;
using SARVA.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SARVA.Controllers
{
    [Authorize]
    public class VendasController : BaseController
    {
        private readonly SARVADbContext _db;

        public VendasController(SARVADbContext db, UserManager<IdentityUser> userManager) : base(userManager)
        {
            _db = db;
        }

        public async Task<IActionResult> LerDadosVenda(DateTime? dataInicio, DateTime? dataFim, string filtroCliente)
        {
            var idUsuario = Convert.ToString(UsuarioIdLogado);

            var query = _db.Venda
                           .Include(v => v.Cliente)
                           .Include(v => v.Empresa)
                           .Where(x => x.id_usuario == idUsuario);

            if (dataInicio.HasValue && dataFim.HasValue)
            {
                query = query.Where(x => x.data_venda >= dataInicio.Value && x.data_venda <= dataFim.Value);
            }

            if (!string.IsNullOrEmpty(filtroCliente))
            {
                query = query.Where(x => x.Cliente.nome == filtroCliente);
            }

            var listOfData = await query.OrderBy(x => x.id).ToListAsync();

            // Refatorando o cálculo de score para ser mais eficiente
            var clientesIds = listOfData.Select(v => v.id_cliente).Distinct();
            var clientesParaAtualizar = await _db.Clientes
                                                 .Include(c => c.Venda)
                                                 .Where(c => clientesIds.Contains(c.id))
                                                 .ToListAsync();

            foreach (var cliente in clientesParaAtualizar)
            {
                if (!cliente.Venda.Any()) continue;
                int totalVendas = cliente.Venda.Count;
                int pagasNoPrazo = cliente.Venda.Count(v => v.data_pagamento.HasValue && v.data_pagamento <= v.data_vencimento.AddDays(7));
                double proporcao = (double)pagasNoPrazo / totalVendas;

                if (proporcao >= 0.6) cliente.scoreId = 1; // Bom Pagador
                else if (proporcao < 0.4) cliente.scoreId = 2; // Mau Pagador
                else cliente.scoreId = 3; // Neutro
            }
            await _db.SaveChangesAsync();

            return View(listOfData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarVenda(Empresa model, string nomeCliente)
        {
            Console.WriteLine(model.razao_social);
            var idUsuario = Convert.ToString(UsuarioIdLogado);
            var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.nome == nomeCliente && c.id_usuario == idUsuario);
            var empresa = await _db.Empresas.FirstOrDefaultAsync(e => e.razao_social == model.razao_social);

            if (cliente == null || empresa == null)
            {
                // Adicionar mensagem de erro para o usuário
                TempData["ErrorMessage"] = "Cliente ou Empresa não encontrados.";
                return RedirectToAction("Index", "Home"); // Ou outra página apropriada
            }

            DateTime now = DateTime.Now;
            var startDate = new DateTime(now.Year, now.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            Venda venda = new Venda
            {
                id_usuario = idUsuario,
                id_empresa = empresa.id,
                id_cliente = cliente.id,
                data_venda = now,
                data_vencimento = endDate
            };

            _db.Venda.Add(venda);
            await _db.SaveChangesAsync();

            ViewBag.Message = "Data Insert Successfully";
            return RedirectToAction("CatalogoProdutos", "Produtos", new { idVenda = venda.id, idEmpresa = venda.id_empresa });
        }

        [HttpGet]
        public async Task<IActionResult> EditarVenda(int idVenda)
        {
            var venda = await _db.Venda
                                .Include(v => v.Cliente)
                                .FirstOrDefaultAsync(x => x.id == idVenda && x.id_usuario == Convert.ToString(UsuarioIdLogado));
            if (venda == null) return NotFound();
            return View(venda);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarVenda(Venda model)
        {
            var venda = await _db.Venda.Include(v => v.Cliente).FirstOrDefaultAsync(x => x.id == model.id);
            if (venda == null || venda.id_usuario != Convert.ToString(UsuarioIdLogado)) return NotFound();

            if (ModelState.IsValid)
            {
                venda.valor = model.valor;
                venda.desconto = model.desconto;
                venda.valorFinal = model.desconto > 0 ? model.valor - model.desconto : model.valor;
                venda.data_venda = model.data_venda;
                venda.data_vencimento = model.data_vencimento;
                venda.data_pagamento = model.data_pagamento;

                // Nota: Editar o nome do cliente aqui é incomum. O ideal seria alterar o id_cliente.
                if (venda.Cliente != null && model.Cliente != null)
                {
                    venda.Cliente.nome = model.Cliente.nome;
                }

                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(LerDadosVenda));
            }
            return View(venda);
        }

        [HttpGet]
        public async Task<IActionResult> FinalizarVenda(int idVenda)
        {
            var venda = await _db.Venda
                                 .Include(v => v.Cliente)
                                 .Include(v => v.Item_Venda)
                                 .ThenInclude(iv => iv.Produto)
                                 .FirstOrDefaultAsync(x => x.id == idVenda && x.id_usuario == Convert.ToString(UsuarioIdLogado));

            if (venda == null) return NotFound();

            var viewModel = new FinalizarVendaViewModel
            {
                Venda = venda,
                ItensVenda = venda.Item_Venda,
                ScoreCliente = venda.Cliente.scoreId ?? 3, // Usar 3 (Neutro) se for nulo
                ValorTotal = venda.Item_Venda.Sum(item => item.valor * item.quantidade)
            };

            return View(viewModel);

            //ViewBag.Venda = venda;
            //ViewBag.ScoreCliente = venda.Cliente.scoreId;

            //return View(venda.Item_Venda.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarVenda(int idVenda, decimal descontoVenda)
        {
            var venda = await _db.Venda
                                .Include(v => v.Cliente)
                                .Include(v => v.Item_Venda)
                                .ThenInclude(iv => iv.Produto)
                                .FirstOrDefaultAsync(x => x.id == idVenda && x.id_usuario == Convert.ToString(UsuarioIdLogado));

            if (venda == null) return NotFound();

            venda.desconto = descontoVenda;
            await _db.SaveChangesAsync();

            var viewModel = new FinalizarVendaViewModel
            {
                Venda = venda,
                ItensVenda = venda.Item_Venda,
                ScoreCliente = venda.Cliente.scoreId ?? 3, // Usar 3 (Neutro) se for nulo
                ValorTotal = venda.Item_Venda.Sum(item => item.valor * item.quantidade)
            };

            return View(viewModel);

            //ViewBag.Venda = venda;
            //ViewBag.ScoreCliente = venda.Cliente.scoreId;

            //return View(venda.Item_Venda.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarVendaPost(int idVenda, decimal valorTotal)
        {
            var venda = await _db.Venda.FirstOrDefaultAsync(x => x.id == idVenda && x.id_usuario == Convert.ToString(UsuarioIdLogado));
            if (venda == null) return NotFound();

            venda.valor = valorTotal;
            venda.valorFinal = venda.desconto > 0 ? valorTotal - venda.desconto : valorTotal;
            await _db.SaveChangesAsync();

            return RedirectToAction("LerDadosVenda", "Vendas");
        }

        public async Task<IActionResult> DetalhesVenda(int id)
        {
            var listaItemVenda = await _db.Item_Venda.Where(x => x.id_venda == id).ToListAsync();
            return View(listaItemVenda);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverVenda(int id)
        {
            var venda = await _db.Venda
                               .Include(v => v.Item_Venda)
                               .FirstOrDefaultAsync(x => x.id == id && x.id_usuario == Convert.ToString(UsuarioIdLogado));
            if (venda == null)
            {
                Console.WriteLine("ERROR NOT FOUND");
                return NotFound();
            }

            // Lógica complexa de remoção e atualização de pedidos relacionados
            var itensPedidoAfetados = await _db.Item_Pedido
                                              .Include(ip => ip.Pedido)
                                              .Where(x => x.id_venda_IV == id)
                                              .ToListAsync();

            foreach (var item in itensPedidoAfetados)
            {
                var itemVendaOriginal = venda.Item_Venda.FirstOrDefault(iv => iv.codigo_produto == item.codigo_IV);
                if (item.Pedido != null && itemVendaOriginal != null)
                {
                    item.Pedido.valor -= 0.7M * (item.valor * itemVendaOriginal.quantidade);
                }
            }
            _db.Item_Pedido.RemoveRange(itensPedidoAfetados);
            _db.Item_Venda.RemoveRange(venda.Item_Venda);
            _db.Venda.Remove(venda);

            await _db.SaveChangesAsync();

            ViewBag.Message = "Record Delete Successfully";
            return RedirectToAction("LerDadosVenda", "Vendas");
        }
    }
}